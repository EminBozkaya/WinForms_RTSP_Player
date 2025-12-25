namespace WinForms_RTSP_Player
{
    partial class SystemLogs
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.dataGridViewLogs = new System.Windows.Forms.DataGridView();
            this.panelButtons = new System.Windows.Forms.Panel();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnClearOldData = new System.Windows.Forms.Button();
            this.panelFilters = new System.Windows.Forms.Panel();
            this.lblMessageFilter = new System.Windows.Forms.Label();
            this.txtSearchMessage = new System.Windows.Forms.TextBox();
            this.lblLevelFilter = new System.Windows.Forms.Label();
            this.cmbLevelFilter = new System.Windows.Forms.ComboBox();
            this.lblStartDate = new System.Windows.Forms.Label();
            this.dtpStart = new System.Windows.Forms.DateTimePicker();
            this.lblEndDate = new System.Windows.Forms.Label();
            this.dtpEnd = new System.Windows.Forms.DateTimePicker();
            this.btnFetchLogs = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewLogs)).BeginInit();
            this.panelButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataGridViewLogs
            // 
            this.dataGridViewLogs.AllowUserToAddRows = false;
            this.dataGridViewLogs.AllowUserToDeleteRows = false;
            this.dataGridViewLogs.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridViewLogs.BackgroundColor = System.Drawing.Color.White;
            this.dataGridViewLogs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewLogs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewLogs.Location = new System.Drawing.Point(0, 0);
            this.dataGridViewLogs.Name = "dataGridViewLogs";
            this.dataGridViewLogs.ReadOnly = true;
            this.dataGridViewLogs.RowTemplate.Height = 25;
            this.dataGridViewLogs.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridViewLogs.Size = new System.Drawing.Size(634, 450);
            this.dataGridViewLogs.TabIndex = 0;
            // 
            // panelButtons
            // 
            this.panelButtons.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panelButtons.Controls.Add(this.btnClearOldData);
            this.panelButtons.Controls.Add(this.btnRefresh);
            this.panelButtons.Controls.Add(this.btnDelete);
            this.panelButtons.Dock = System.Windows.Forms.DockStyle.Right;
            this.panelButtons.Location = new System.Drawing.Point(634, 0);
            this.panelButtons.Name = "panelButtons";
            this.panelButtons.Padding = new System.Windows.Forms.Padding(10);
            this.panelButtons.Size = new System.Drawing.Size(166, 450);
            this.panelButtons.TabIndex = 1;
            // 
            // panelFilters
            // 
            this.panelFilters.BackColor = System.Drawing.Color.White;
            this.panelFilters.Controls.Add(this.btnFetchLogs);
            this.panelFilters.Controls.Add(this.dtpEnd);
            this.panelFilters.Controls.Add(this.lblEndDate);
            this.panelFilters.Controls.Add(this.dtpStart);
            this.panelFilters.Controls.Add(this.lblStartDate);
            this.panelFilters.Controls.Add(this.cmbLevelFilter);
            this.panelFilters.Controls.Add(this.lblLevelFilter);
            this.panelFilters.Controls.Add(this.txtSearchMessage);
            this.panelFilters.Controls.Add(this.lblMessageFilter);
            this.panelFilters.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelFilters.Location = new System.Drawing.Point(0, 0);
            this.panelFilters.Name = "panelFilters";
            this.panelFilters.Size = new System.Drawing.Size(634, 75);
            this.panelFilters.TabIndex = 2;
            // 
            // lblMessageFilter
            // 
            this.lblMessageFilter.AutoSize = true;
            this.lblMessageFilter.Location = new System.Drawing.Point(12, 12);
            this.lblMessageFilter.Name = "lblMessageFilter";
            this.lblMessageFilter.Size = new System.Drawing.Size(38, 13);
            this.lblMessageFilter.TabIndex = 0;
            this.lblMessageFilter.Text = "Mesaj:";
            // 
            // txtSearchMessage
            // 
            this.txtSearchMessage.Location = new System.Drawing.Point(12, 28);
            this.txtSearchMessage.Name = "txtSearchMessage";
            this.txtSearchMessage.Size = new System.Drawing.Size(200, 20);
            this.txtSearchMessage.TabIndex = 1;
            this.txtSearchMessage.TextChanged += new System.EventHandler(this.FilterControl_Changed);
            // 
            // lblLevelFilter
            // 
            this.lblLevelFilter.AutoSize = true;
            this.lblLevelFilter.Location = new System.Drawing.Point(230, 12);
            this.lblLevelFilter.Name = "lblLevelFilter";
            this.lblLevelFilter.Size = new System.Drawing.Size(42, 13);
            this.lblLevelFilter.TabIndex = 2;
            this.lblLevelFilter.Text = "Seviye:";
            // 
            // cmbLevelFilter
            // 
            this.cmbLevelFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLevelFilter.FormattingEnabled = true;
            this.cmbLevelFilter.Location = new System.Drawing.Point(230, 28);
            this.cmbLevelFilter.Name = "cmbLevelFilter";
            this.cmbLevelFilter.Size = new System.Drawing.Size(100, 21);
            this.cmbLevelFilter.TabIndex = 3;
            this.cmbLevelFilter.SelectedIndexChanged += new System.EventHandler(this.FilterControl_Changed);
            // 
            // lblStartDate
            // 
            this.lblStartDate.AutoSize = true;
            this.lblStartDate.Location = new System.Drawing.Point(345, 12);
            this.lblStartDate.Name = "lblStartDate";
            this.lblStartDate.Size = new System.Drawing.Size(56, 13);
            this.lblStartDate.TabIndex = 4;
            this.lblStartDate.Text = "Başlangıç:";
            // 
            // dtpStart
            // 
            this.dtpStart.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpStart.Location = new System.Drawing.Point(345, 28);
            this.dtpStart.Name = "dtpStart";
            this.dtpStart.Size = new System.Drawing.Size(95, 20);
            this.dtpStart.TabIndex = 5;
            // 
            // lblEndDate
            // 
            this.lblEndDate.AutoSize = true;
            this.lblEndDate.Location = new System.Drawing.Point(450, 12);
            this.lblEndDate.Name = "lblEndDate";
            this.lblEndDate.Size = new System.Drawing.Size(29, 13);
            this.lblEndDate.TabIndex = 6;
            this.lblEndDate.Text = "Bitiş:";
            // 
            // dtpEnd
            // 
            this.dtpEnd.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpEnd.Location = new System.Drawing.Point(450, 28);
            this.dtpEnd.Name = "dtpEnd";
            this.dtpEnd.Size = new System.Drawing.Size(95, 20);
            this.dtpEnd.TabIndex = 7;
            // 
            // btnFetchLogs
            // 
            this.btnFetchLogs.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(204)))), ((int)(((byte)(113)))));
            this.btnFetchLogs.FlatAppearance.BorderSize = 0;
            this.btnFetchLogs.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFetchLogs.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnFetchLogs.ForeColor = System.Drawing.Color.White;
            this.btnFetchLogs.Location = new System.Drawing.Point(555, 12);
            this.btnFetchLogs.Name = "btnFetchLogs";
            this.btnFetchLogs.Size = new System.Drawing.Size(70, 41);
            this.btnFetchLogs.TabIndex = 8;
            this.btnFetchLogs.Text = "Listeyi Getir";
            this.btnFetchLogs.UseVisualStyleBackColor = false;
            this.btnFetchLogs.Click += new System.EventHandler(this.btnFetchLogs_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(231)))), ((int)(((byte)(76)))), ((int)(((byte)(60)))));
            this.btnDelete.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnDelete.FlatAppearance.BorderSize = 0;
            this.btnDelete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDelete.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnDelete.ForeColor = System.Drawing.Color.White;
            this.btnDelete.Location = new System.Drawing.Point(10, 10);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(146, 45);
            this.btnDelete.TabIndex = 0;
            this.btnDelete.Text = "Seçileni Sil";
            this.btnDelete.UseVisualStyleBackColor = false;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnRefresh
            // 
            this.btnRefresh.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(152)))), ((int)(((byte)(219)))));
            this.btnRefresh.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnRefresh.FlatAppearance.BorderSize = 0;
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnRefresh.ForeColor = System.Drawing.Color.White;
            this.btnRefresh.Location = new System.Drawing.Point(10, 55);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(146, 45);
            this.btnRefresh.TabIndex = 1;
            this.btnRefresh.Text = "🔄 Yenile";
            this.btnRefresh.UseVisualStyleBackColor = false;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // btnClearOldData
            // 
            this.btnClearOldData.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(127)))), ((int)(((byte)(140)))), ((int)(((byte)(141)))));
            this.btnClearOldData.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnClearOldData.FlatAppearance.BorderSize = 0;
            this.btnClearOldData.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClearOldData.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnClearOldData.ForeColor = System.Drawing.Color.White;
            this.btnClearOldData.Location = new Point(10, 100);
            this.btnClearOldData.Name = "btnClearOldData";
            this.btnClearOldData.Size = new System.Drawing.Size(146, 60);
            this.btnClearOldData.TabIndex = 2;
            this.btnClearOldData.Text = "Son bir hafta verilerinin dışındakileri temizle";
            this.btnClearOldData.UseVisualStyleBackColor = false;
            this.btnClearOldData.Click += new System.EventHandler(this.btnClearOldData_Click);
            // 
            // SystemLogs
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.dataGridViewLogs);
            this.Controls.Add(this.panelFilters);
            this.Controls.Add(this.panelButtons);
            this.Name = "SystemLogs";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Sistem Genel Kayıtları";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewLogs)).EndInit();
            this.panelButtons.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridViewLogs;
        private System.Windows.Forms.Panel panelButtons;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnClearOldData;
        private System.Windows.Forms.Panel panelFilters;
        private System.Windows.Forms.Label lblMessageFilter;
        private System.Windows.Forms.TextBox txtSearchMessage;
        private System.Windows.Forms.Label lblLevelFilter;
        private System.Windows.Forms.ComboBox cmbLevelFilter;
        private System.Windows.Forms.Label lblStartDate;
        private System.Windows.Forms.DateTimePicker dtpStart;
        private System.Windows.Forms.Label lblEndDate;
        private System.Windows.Forms.DateTimePicker dtpEnd;
        private System.Windows.Forms.Button btnFetchLogs;
    }
}
