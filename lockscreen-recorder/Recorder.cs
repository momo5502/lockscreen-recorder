using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace lockscreen_recorder
{
    public partial class Recorder : Form
    {
        private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private KeyboardHook keyboardHook = new KeyboardHook();
        private Thread? recordingThread = null;

        private DateTime? lastShiftPressed = null;

        public Recorder()
        {
            InitializeComponent();
            timer.Interval = 300;
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private bool IsRecording()
        {
            lock (this)
            {
                return recordingThread != null;
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!IsRecording())
            {
                this.TopMost = false;
                this.TopMost = true;
            }
        }

        void HideWindow()
        {
            this.TopMost = false;
            this.WindowState = FormWindowState.Minimized;
        }

        void ShowWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
            this.TopMost = true;
        }

        private void StartRecording()
        {
            HideWindow();
            Thread.Sleep(1000);
            lock (this)
            {
                recordingThread = new Thread(PerformRecording);
                recordingThread.Start();
            }
        }

        private void StopRecording()
        {
            Thread? thread;

            lock (this)
            {
                thread = recordingThread;
                recordingThread = null;
            }

            thread?.Join();

            ShowWindow();
        }

        private void ToggleRecording()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(ToggleRecording));
                return;
            }

            if (IsRecording())
            {
                StopRecording();
            }
            else
            {
                StartRecording();
            }
        }

        private bool IsNotOlderThan(DateTime time, TimeSpan span)
        {
            var diff = DateTime.Now - time;
            return diff < span;
        }

        private void KeyboardHook_KeyPressed(Keys key, bool isPressed)
        {
            var isShift = key == Keys.LShiftKey || key == Keys.RShiftKey;

            if (!isShift)
            {
                return;
            }

            Debug.WriteLine("shift: " + (isPressed ? "pressed" : "released"));

            var needsAction = false;

            lock (this)
            {
                var wasPressed = lastShiftPressed.HasValue && IsNotOlderThan(lastShiftPressed.Value, TimeSpan.FromSeconds(3));
                lastShiftPressed = isPressed ? DateTime.Now : null;

                needsAction = isPressed && !wasPressed;
            }

            if (needsAction)
            {
                ToggleRecording();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            keyboardHook.KeyPressed += KeyboardHook_KeyPressed;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer?.Stop();
            keyboardHook.Cleanup();
        }

        void ReencodeVideo(string infile, string outfile)
        {
            Program.LaunchProcess("ffmpeg.exe", $"-i {infile} -c:v libx264 -crf 23 -profile:v baseline -level 3.0 -pix_fmt yuv420p -c:a aac -ac 2 -b:a 128k -movflags faststart {outfile}");
        }

        void PerformRecording()
        {
            Directory.CreateDirectory("recordings");

            var bounds = Screen.PrimaryScreen.Bounds;
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var fileName = "recordings/output-" + timestamp + "-lossless.mp4";
            var compatibleFileName = "recordings/output-" + timestamp + "-compressed.mp4";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $"-f gdigrab -framerate 60 -offset_x {bounds.Left} -offset_y {bounds.Top} -video_size {bounds.Width}x{bounds.Height} -i desktop -c:v libx264 -preset ultrafast {fileName}",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardInputEncoding = Encoding.ASCII,
            };

            // Create and start the process
            using (Process process = new Process())
            {
                StringBuilder outputBuffer = new StringBuilder();

                process.StartInfo = startInfo;
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        outputBuffer.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        outputBuffer.AppendLine(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                while (IsRecording())
                {
                    Thread.Sleep(100);
                }

                Debug.WriteLine("Stopping recording...");

                byte[] qKey = Encoding.ASCII.GetBytes("q");
                process.StandardInput.BaseStream.Write(qKey, 0, 1);
                process.StandardInput.BaseStream.Flush();

                process.WaitForExit();

                Debug.WriteLine("Output:");
                Debug.WriteLine(outputBuffer.ToString());
            }

            Task.Run(() =>
            {
                ReencodeVideo(fileName, compatibleFileName);
            });
        }
    }
}
