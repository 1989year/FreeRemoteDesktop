namespace desktop
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            axrdpViewer1 = new AxRDPCOMAPILib.AxRDPViewer();
            pbLoading = new PictureBox();
            lbMgr = new Label();
            ((System.ComponentModel.ISupportInitialize)axrdpViewer1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pbLoading).BeginInit();
            SuspendLayout();
            // 
            // axrdpViewer1
            // 
            axrdpViewer1.Dock = DockStyle.Fill;
            axrdpViewer1.Enabled = true;
            axrdpViewer1.Location = new Point(0, 0);
            axrdpViewer1.Margin = new Padding(0);
            axrdpViewer1.Name = "axrdpViewer1";
            axrdpViewer1.OcxState = (AxHost.State)resources.GetObject("axrdpViewer1.OcxState");
            axrdpViewer1.Size = new Size(784, 561);
            axrdpViewer1.TabIndex = 0;
            axrdpViewer1.Visible = false;
            // 
            // pbLoading
            // 
            pbLoading.Image = Properties.Resources.loading;
            pbLoading.Location = new Point(368, 193);
            pbLoading.Margin = new Padding(0);
            pbLoading.Name = "pbLoading";
            pbLoading.Size = new Size(64, 64);
            pbLoading.SizeMode = PictureBoxSizeMode.StretchImage;
            pbLoading.TabIndex = 1;
            pbLoading.TabStop = false;
            // 
            // lbMgr
            // 
            lbMgr.BackColor = Color.FromArgb(74, 134, 232);
            lbMgr.Dock = DockStyle.Top;
            lbMgr.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            lbMgr.ForeColor = Color.White;
            lbMgr.Location = new Point(0, 0);
            lbMgr.Margin = new Padding(0);
            lbMgr.Name = "lbMgr";
            lbMgr.Padding = new Padding(10, 0, 0, 0);
            lbMgr.Size = new Size(784, 40);
            lbMgr.TabIndex = 4;
            lbMgr.Text = "Loading...";
            lbMgr.TextAlign = ContentAlignment.MiddleLeft;
            lbMgr.Visible = false;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(784, 561);
            Controls.Add(lbMgr);
            Controls.Add(pbLoading);
            Controls.Add(axrdpViewer1);
            Name = "MainForm";
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Form";
            Load += MainForm_Load;
            Resize += MainForm_Resize;
            ((System.ComponentModel.ISupportInitialize)axrdpViewer1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pbLoading).EndInit();
            ResumeLayout(false);
        }

        #endregion

        public AxRDPCOMAPILib.AxRDPViewer axrdpViewer1;
        private PictureBox pbLoading;
        private Label lbMgr;
    }
}
