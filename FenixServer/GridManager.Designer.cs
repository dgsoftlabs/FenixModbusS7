namespace FenixServer
{
    partial class GridManager
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dgvMain = new System.Windows.Forms.DataGridView();
            this.conName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.conType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.conSend = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.conRecive = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.conStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMain)).BeginInit();
            this.SuspendLayout();
            // 
            // dgvMain
            // 
            this.dgvMain.AllowUserToAddRows = false;
            this.dgvMain.AllowUserToDeleteRows = false;
            this.dgvMain.AllowUserToResizeRows = false;
            this.dgvMain.BackgroundColor = System.Drawing.SystemColors.ControlLight;
            this.dgvMain.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvMain.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.conName,
            this.conType,
            this.conSend,
            this.conRecive,
            this.conStatus});
            this.dgvMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvMain.GridColor = System.Drawing.SystemColors.ControlLight;
            this.dgvMain.Location = new System.Drawing.Point(0, 0);
            this.dgvMain.Name = "dgvMain";
            this.dgvMain.ReadOnly = true;
            this.dgvMain.RowHeadersWidth = 5;
            this.dgvMain.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.dgvMain.Size = new System.Drawing.Size(495, 454);
            this.dgvMain.TabIndex = 0;
            // 
            // conName
            // 
            this.conName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.conName.DefaultCellStyle = dataGridViewCellStyle1;
            this.conName.HeaderText = "Name";
            this.conName.Name = "conName";
            this.conName.ReadOnly = true;
            this.conName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.conName.Width = 41;
            // 
            // conType
            // 
            this.conType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.conType.HeaderText = "Type";
            this.conType.Name = "conType";
            this.conType.ReadOnly = true;
            this.conType.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.conType.Width = 37;
            // 
            // conSend
            // 
            this.conSend.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.conSend.HeaderText = "Send Bytes";
            this.conSend.Name = "conSend";
            this.conSend.ReadOnly = true;
            this.conSend.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.conSend.Width = 67;
            // 
            // conRecive
            // 
            this.conRecive.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.conRecive.HeaderText = "Recive Bytes";
            this.conRecive.Name = "conRecive";
            this.conRecive.ReadOnly = true;
            this.conRecive.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.conRecive.Width = 76;
            // 
            // conStatus
            // 
            this.conStatus.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.conStatus.HeaderText = "IsLive";
            this.conStatus.Name = "conStatus";
            this.conStatus.ReadOnly = true;
            this.conStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // GridManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(495, 454);
            this.CloseButton = false;
            this.CloseButtonVisible = false;
            this.Controls.Add(this.dgvMain);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.Name = "GridManager";
            this.Text = "Connections";
            ((System.ComponentModel.ISupportInitialize)(this.dgvMain)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.DataGridView dgvMain;
        private System.Windows.Forms.DataGridViewTextBoxColumn conName;
        private System.Windows.Forms.DataGridViewTextBoxColumn conType;
        private System.Windows.Forms.DataGridViewTextBoxColumn conSend;
        private System.Windows.Forms.DataGridViewTextBoxColumn conRecive;
        private System.Windows.Forms.DataGridViewTextBoxColumn conStatus;
    }
}