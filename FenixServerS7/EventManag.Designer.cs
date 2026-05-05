namespace FenixServer
{
    partial class EventManag
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EventManag));
            this.dgvMain = new System.Windows.Forms.DataGridView();
            this.alDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.alInfo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.mnTop = new System.Windows.Forms.ToolStrip();
            this.btClear = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMain)).BeginInit();
            this.mnTop.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvMain
            // 
            this.dgvMain.AllowUserToAddRows = false;
            this.dgvMain.AllowUserToDeleteRows = false;
            this.dgvMain.BackgroundColor = System.Drawing.SystemColors.ControlLight;
            this.dgvMain.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvMain.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.alDate,
            this.alInfo});
            this.dgvMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvMain.GridColor = System.Drawing.SystemColors.ControlLight;
            this.dgvMain.Location = new System.Drawing.Point(0, 25);
            this.dgvMain.Name = "dgvMain";
            this.dgvMain.ReadOnly = true;
            this.dgvMain.Size = new System.Drawing.Size(520, 409);
            this.dgvMain.TabIndex = 0;
            // 
            // alDate
            // 
            this.alDate.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.alDate.HeaderText = "DateTime";
            this.alDate.Name = "alDate";
            this.alDate.ReadOnly = true;
            this.alDate.Width = 78;
            // 
            // alInfo
            // 
            this.alInfo.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.alInfo.HeaderText = "Description";
            this.alInfo.Name = "alInfo";
            this.alInfo.ReadOnly = true;
            // 
            // mnTop
            // 
            this.mnTop.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btClear});
            this.mnTop.Location = new System.Drawing.Point(0, 0);
            this.mnTop.Name = "mnTop";
            this.mnTop.Size = new System.Drawing.Size(520, 25);
            this.mnTop.TabIndex = 1;
            this.mnTop.Text = "toolStrip1";
            // 
            // btClear
            // 
            this.btClear.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btClear.Image = global::FenixServer.Properties.Resources.Clear;
            this.btClear.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btClear.Name = "btClear";
            this.btClear.Size = new System.Drawing.Size(23, 22);
            this.btClear.Text = "btClear";
            // 
            // EventManag
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(520, 434);
            this.CloseButton = false;
            this.CloseButtonVisible = false;
            this.Controls.Add(this.dgvMain);
            this.Controls.Add(this.mnTop);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "EventManag";
            this.Text = "Events";
            ((System.ComponentModel.ISupportInitialize)(this.dgvMain)).EndInit();
            this.mnTop.ResumeLayout(false);
            this.mnTop.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.DataGridView dgvMain;
        private System.Windows.Forms.DataGridViewTextBoxColumn alDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn alInfo;
        private System.Windows.Forms.ToolStrip mnTop;
        public System.Windows.Forms.ToolStripButton btClear;
    }
}