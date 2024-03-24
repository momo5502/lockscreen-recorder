using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace lockscreen_recorder
{
    public partial class Spawner : Form
    {
        private Thread? runner = null;

        public Spawner()
        {
            InitializeComponent();
        }

        private void Spawner_Load(object sender, EventArgs e)
        {
            if (runner != null)
            {
                return;
            }

            runner = new Thread(LaunchRecorder);
            runner.Start();
        }

        private void CloseThisForm()
        {
            if (InvokeRequired)
            {
                BeginInvoke(CloseThisForm);
                return;
            }

            Close();
        }

        private void LaunchRecorder()
        {
            var currentApp = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            Program.LaunchProcess("PsExec64.exe", $"-accepteula -s -x -i \"{currentApp}\"");
            CloseThisForm();
        }

        private void KillRecorder()
        {
            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "LockscreenRecorderKillPipe", PipeDirection.InOut))
            {
                try
                {
                    pipeClient.Connect(1000);
                }
                catch (Exception ex) { }
            }
        }

        private void Spawner_FormClosed(object sender, FormClosedEventArgs e)
        {
            runner?.Join();
            runner = null;
        }

        private void Spawner_FormClosing(object sender, FormClosingEventArgs e)
        {
            KillRecorder();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Directory.CreateDirectory("recordings");
            Process.Start("explorer.exe", Path.Join(Directory.GetCurrentDirectory(), "recordings"));
        }
    }
}
