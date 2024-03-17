using System.Diagnostics;
using System.Runtime.InteropServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using System.Windows.Forms;
using System.Text;

namespace lockscreen_recorder
{
    internal static class Program
    {
        public const int UOI_NAME = 2;

        [DllImport("kernel32.dll")]
        public static extern int GetCurrentThreadId();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetThreadDesktop(int dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool GetUserObjectInformation(IntPtr hObj, int nIndex, [Out] byte[] pvInfo, uint nLength, out uint lpnLengthNeeded);

        public static bool IsWinlogonDesktop()
        {
            IntPtr desktop = GetThreadDesktop(GetCurrentThreadId());

            byte[] name = new byte[256];
            uint length;
            GetUserObjectInformation(desktop, UOI_NAME, name, (uint)name.Length, out length);

            string desktopName = System.Text.Encoding.Unicode.GetString(name).TrimEnd('\0');
            return desktopName == "Winlogon";
        }

        public static bool IsProcessRunning(string processName)
        {
            foreach (Process process in Process.GetProcesses())
            {
                if (process.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public static void LaunchProcess(string app, string args)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = app,
                Arguments = args,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardInputEncoding = Encoding.ASCII,
            };

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

                process.WaitForExit();

                Debug.WriteLine("Output:");
                Debug.WriteLine(outputBuffer.ToString());
            }
        }

        [STAThread]
        static void Main()
        {
            if (!IsProcessRunning("aura"))
            {
                MessageBox.Show("Aura Wallpaper must be running", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Directory.SetCurrentDirectory(exeDirectory);

            ApplicationConfiguration.Initialize();

            Form form;
            if (!IsWinlogonDesktop())
            {
                form = new Spawner();
            }
            else
            {
                form = new Recorder();
            }

            Application.Run(form);
        }
    }
}