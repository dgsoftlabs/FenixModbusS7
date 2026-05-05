namespace FenixServer
{
    partial class ManagerView
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ManagerView));
            this.mnBottom = new System.Windows.Forms.StatusStrip();
            this.mPath = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.mnMiddle = new System.Windows.Forms.ToolStrip();
            this.openToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.btStartWeb = new System.Windows.Forms.ToolStripButton();
            this.btStopWeb = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.HelpView1 = new System.Windows.Forms.ToolStripButton();
            this.About1 = new System.Windows.Forms.ToolStripButton();
            this.mnTop = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.projectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mnAutostart = new System.Windows.Forms.ToolStripMenuItem();
            this.dockPan = new WeifenLuo.WinFormsUI.Docking.DockPanel();
            this.ofd_Menager = new System.Windows.Forms.OpenFileDialog();
            this.ntfIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.cmnNotify = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.showServerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mnBottom.SuspendLayout();
            this.mnMiddle.SuspendLayout();
            this.mnTop.SuspendLayout();
            this.cmnNotify.SuspendLayout();
            this.SuspendLayout();
            // 
            // mnBottom
            // 
            this.mnBottom.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.mnBottom.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mPath,
            this.toolStripStatusLabel1});
            this.mnBottom.Location = new System.Drawing.Point(0, 592);
            this.mnBottom.Name = "mnBottom";
            this.mnBottom.Size = new System.Drawing.Size(930, 22);
            this.mnBottom.TabIndex = 0;
            this.mnBottom.Text = "statusStrip1";
            // 
            // mPath
            // 
            this.mPath.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.mPath.Name = "mPath";
            this.mPath.Size = new System.Drawing.Size(12, 17);
            this.mPath.Text = "-";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(111, 17);
            this.toolStripStatusLabel1.Text = "Project: Read Only";
            // 
            // mnMiddle
            // 
            this.mnMiddle.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.mnMiddle.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripButton,
            this.toolStripSeparator,
            this.btStartWeb,
            this.btStopWeb,
            this.toolStripSeparator1,
            this.HelpView1,
            this.About1});
            this.mnMiddle.Location = new System.Drawing.Point(0, 24);
            this.mnMiddle.Name = "mnMiddle";
            this.mnMiddle.Size = new System.Drawing.Size(930, 39);
            this.mnMiddle.TabIndex = 1;
            this.mnMiddle.Text = "toolStrip1";
            // 
            // openToolStripButton
            // 
            this.openToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.openToolStripButton.Image = global::FenixServer.Properties.Resources.Open;
            this.openToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.openToolStripButton.Name = "openToolStripButton";
            this.openToolStripButton.Size = new System.Drawing.Size(36, 36);
            this.openToolStripButton.Text = "&Open";
            this.openToolStripButton.Click += new System.EventHandler(this.openToolStripButton_Click);
            // 
            // toolStripSeparator
            // 
            this.toolStripSeparator.Name = "toolStripSeparator";
            this.toolStripSeparator.Size = new System.Drawing.Size(6, 39);
            // 
            // btStartWeb
            // 
            this.btStartWeb.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btStartWeb.Enabled = false;
            this.btStartWeb.Image = global::FenixServer.Properties.Resources.run;
            this.btStartWeb.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btStartWeb.Name = "btStartWeb";
            this.btStartWeb.Size = new System.Drawing.Size(36, 36);
            this.btStartWeb.Text = "btStartWeb";
            this.btStartWeb.Click += new System.EventHandler(this.btStartButton1_Click);
            // 
            // btStopWeb
            // 
            this.btStopWeb.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btStopWeb.Enabled = false;
            this.btStopWeb.Image = global::FenixServer.Properties.Resources.stop;
            this.btStopWeb.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btStopWeb.Name = "btStopWeb";
            this.btStopWeb.Size = new System.Drawing.Size(36, 36);
            this.btStopWeb.Text = "btStopWeb";
            this.btStopWeb.Click += new System.EventHandler(this.btStopButton_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 39);
            // 
            // HelpView1
            // 
            this.HelpView1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.HelpView1.Image = global::FenixServer.Properties.Resources.HelpView;
            this.HelpView1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.HelpView1.Name = "HelpView1";
            this.HelpView1.Size = new System.Drawing.Size(36, 36);
            this.HelpView1.Text = "HelpView";
            this.HelpView1.Click += new System.EventHandler(this.HelpView1_Click);
            // 
            // About1
            // 
            this.About1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.About1.Image = global::FenixServer.Properties.Resources.Help;
            this.About1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.About1.Name = "About1";
            this.About1.Size = new System.Drawing.Size(36, 36);
            this.About1.Text = "About";
            this.About1.Click += new System.EventHandler(this.About1_Click);
            // 
            // mnTop
            // 
            this.mnTop.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.projectToolStripMenuItem});
            this.mnTop.Location = new System.Drawing.Point(0, 0);
            this.mnTop.Name = "mnTop";
            this.mnTop.Size = new System.Drawing.Size(930, 24);
            this.mnTop.TabIndex = 2;
            this.mnTop.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.closeProjectToolStripMenuItem,
            this.closeToolStripMenuItem1});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Image = global::FenixServer.Properties.Resources.Open;
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(143, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // closeProjectToolStripMenuItem
            // 
            this.closeProjectToolStripMenuItem.Name = "closeProjectToolStripMenuItem";
            this.closeProjectToolStripMenuItem.Size = new System.Drawing.Size(143, 22);
            this.closeProjectToolStripMenuItem.Text = "Close Project";
            this.closeProjectToolStripMenuItem.Click += new System.EventHandler(this.closeProjectToolStripMenuItem_Click);
            // 
            // closeToolStripMenuItem1
            // 
            this.closeToolStripMenuItem1.Name = "closeToolStripMenuItem1";
            this.closeToolStripMenuItem1.Size = new System.Drawing.Size(143, 22);
            this.closeToolStripMenuItem1.Text = "Close";
            this.closeToolStripMenuItem1.Click += new System.EventHandler(this.closeToolStripMenuItem1_Click);
            // 
            // projectToolStripMenuItem
            // 
            this.projectToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnAutostart});
            this.projectToolStripMenuItem.Name = "projectToolStripMenuItem";
            this.projectToolStripMenuItem.Size = new System.Drawing.Size(56, 20);
            this.projectToolStripMenuItem.Text = "Project";
            this.projectToolStripMenuItem.DropDownOpened += new System.EventHandler(this.projectToolStripMenuItem_DropDownOpened);
            // 
            // mnAutostart
            // 
            this.mnAutostart.Name = "mnAutostart";
            this.mnAutostart.Size = new System.Drawing.Size(123, 22);
            this.mnAutostart.Text = "Autostart";
            this.mnAutostart.Click += new System.EventHandler(this.mnAutostart_Click);
            // 
            // dockPan
            // 
            this.dockPan.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dockPan.Location = new System.Drawing.Point(0, 63);
            this.dockPan.Name = "dockPan";
            this.dockPan.Size = new System.Drawing.Size(930, 529);
            this.dockPan.TabIndex = 4;
            // 
            // ntfIcon
            // 
            this.ntfIcon.ContextMenuStrip = this.cmnNotify;
            this.ntfIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("ntfIcon.Icon")));
            this.ntfIcon.Text = "Notyfi";
            this.ntfIcon.DoubleClick += new System.EventHandler(this.ntfIcon_DoubleClick);
            // 
            // cmnNotify
            // 
            this.cmnNotify.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showServerToolStripMenuItem,
            this.closeToolStripMenuItem});
            this.cmnNotify.Name = "cmnNotify";
            this.cmnNotify.Size = new System.Drawing.Size(139, 48);
            this.cmnNotify.Text = "Fenix Server";
            // 
            // showServerToolStripMenuItem
            // 
            this.showServerToolStripMenuItem.Name = "showServerToolStripMenuItem";
            this.showServerToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            this.showServerToolStripMenuItem.Text = "Show Server";
            this.showServerToolStripMenuItem.Click += new System.EventHandler(this.showServerToolStripMenuItem_Click);
            // 
            // closeToolStripMenuItem
            // 
            this.closeToolStripMenuItem.Image = global::FenixServer.Properties.Resources.Close;
            this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            this.closeToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            this.closeToolStripMenuItem.Text = "Close";
            this.closeToolStripMenuItem.Click += new System.EventHandler(this.closeToolStripMenuItem_Click);
            // 
            // ManagerView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(930, 614);
            this.Controls.Add(this.dockPan);
            this.Controls.Add(this.mnMiddle);
            this.Controls.Add(this.mnBottom);
            this.Controls.Add(this.mnTop);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.IsMdiContainer = true;
            this.MainMenuStrip = this.mnTop;
            this.Name = "ManagerView";
            this.Text = "Fenix Server";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ManagerView_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ManagerView_FormClosed);
            this.Shown += new System.EventHandler(this.ManagerView_Shown);
            this.mnBottom.ResumeLayout(false);
            this.mnBottom.PerformLayout();
            this.mnMiddle.ResumeLayout(false);
            this.mnMiddle.PerformLayout();
            this.mnTop.ResumeLayout(false);
            this.mnTop.PerformLayout();
            this.cmnNotify.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip mnBottom;
        private System.Windows.Forms.ToolStrip mnMiddle;
        private System.Windows.Forms.MenuStrip mnTop;
        private System.Windows.Forms.ToolStripButton openToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private WeifenLuo.WinFormsUI.Docking.DockPanel dockPan;
        private System.Windows.Forms.ToolStripButton btStartWeb;
        private System.Windows.Forms.ToolStripButton btStopWeb;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.OpenFileDialog ofd_Menager;
        private System.Windows.Forms.NotifyIcon ntfIcon;
        private System.Windows.Forms.ContextMenuStrip cmnNotify;
        private System.Windows.Forms.ToolStripMenuItem showServerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem projectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mnAutostart;
        private System.Windows.Forms.ToolStripStatusLabel mPath;
        private System.Windows.Forms.ToolStripButton HelpView1;
        private System.Windows.Forms.ToolStripButton About1;
        private System.Windows.Forms.ToolStripMenuItem closeProjectToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
    }
}

