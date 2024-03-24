namespace lockscreen_recorder
{
    partial class Spawner
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Spawner));
            label1 = new Label();
            button1 = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(33, 29);
            label1.Name = "label1";
            label1.Size = new Size(295, 15);
            label1.TabIndex = 0;
            label1.Text = "Recorder is visible on the LockScreen if Aura is running";
            // 
            // button1
            // 
            button1.Location = new Point(33, 61);
            button1.Name = "button1";
            button1.Size = new Size(295, 23);
            button1.TabIndex = 1;
            button1.Text = "View recordings";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // Spawner
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(361, 103);
            Controls.Add(button1);
            Controls.Add(label1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "Spawner";
            Text = "LockScreen Recorder";
            FormClosing += Spawner_FormClosing;
            FormClosed += Spawner_FormClosed;
            Load += Spawner_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Button button1;
    }
}