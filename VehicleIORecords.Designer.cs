namespace WinForms_RTSP_Player
{
    partial class VehicleIORecords
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
            this.lblPlateFilter = new System.Windows.Forms.Label();
            this.txtSearchPlate = new System.Windows.Forms.TextBox();
            this.lblTypeFilter = new System.Windows.Forms.Label();
            this.cmbTypeFilter = new System.Windows.Forms.ComboBox();
            this.lblAuthFilter = new System.Windows.Forms.Label();
            this.cmbAuthFilter = new System.Windows.Forms.ComboBox();
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
            this.panelFilters.Controls.Add(this.cmbAuthFilter);
            this.panelFilters.Controls.Add(this.lblAuthFilter);
            this.panelFilters.Controls.Add(this.cmbTypeFilter);
            this.panelFilters.Controls.Add(this.lblTypeFilter);
            this.panelFilters.Controls.Add(this.txtSearchPlate);
            this.panelFilters.Controls.Add(this.lblPlateFilter);
            this.panelFilters.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelFilters.Location = new System.Drawing.Point(0, 0);
            this.panelFilters.Name = "panelFilters";
            this.panelFilters.Size = new System.Drawing.Size(634, 75);
            this.panelFilters.TabIndex = 2;
            // 
            // lblPlateFilter
            // 
            this.lblPlateFilter.AutoSize = true;
            this.lblPlateFilter.Location = new System.Drawing.Point(12, 12);
            this.lblPlateFilter.Name = "lblPlateFilter";
            this.lblPlateFilter.Size = new System.Drawing.Size(37, 13);
            this.lblPlateFilter.TabIndex = 0;
            this.lblPlateFilter.Text = "Plaka:";
            // 
            // txtSearchPlate
            // 
            this.txtSearchPlate.Location = new System.Drawing.Point(12, 28);
            this.txtSearchPlate.Name = "txtSearchPlate";
            this.txtSearchPlate.Size = new System.Drawing.Size(120, 20);
            this.txtSearchPlate.TabIndex = 1;
            this.txtSearchPlate.TextChanged += new System.EventHandler(this.FilterControl_Changed);
            // 
            // lblTypeFilter
            // 
            this.lblTypeFilter.AutoSize = true;
            this.lblTypeFilter.Location = new System.Drawing.Point(150, 12);
            this.lblTypeFilter.Name = "lblTypeFilter";
            this.lblTypeFilter.Size = new System.Drawing.Size(61, 13);
            this.lblTypeFilter.TabIndex = 2;
            this.lblTypeFilter.Text = "Giriş/Çıkış:";
            // 
            // cmbTypeFilter
            // 
            this.cmbTypeFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTypeFilter.FormattingEnabled = true;
            this.cmbTypeFilter.Location = new System.Drawing.Point(150, 28);
            this.cmbTypeFilter.Name = "cmbTypeFilter";
            this.cmbTypeFilter.Size = new System.Drawing.Size(100, 21);
            this.cmbTypeFilter.TabIndex = 3;
            this.cmbTypeFilter.SelectedIndexChanged += new System.EventHandler(this.FilterControl_Changed);
            // 
            // lblAuthFilter
            // 
            this.lblAuthFilter.AutoSize = true;
            this.lblAuthFilter.Location = new System.Drawing.Point(270, 12);
            this.lblAuthFilter.Name = "lblAuthFilter";
            this.lblAuthFilter.Size = new System.Drawing.Size(74, 13);
            this.lblAuthFilter.TabIndex = 4;
            this.lblAuthFilter.Text = "Yetki Durumu:";
            // 
            // cmbAuthFilter
            // 
            this.cmbAuthFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAuthFilter.FormattingEnabled = true;
            this.cmbAuthFilter.Location = new System.Drawing.Point(270, 28);
            this.cmbAuthFilter.Name = "cmbAuthFilter";
            this.cmbAuthFilter.Size = new System.Drawing.Size(100, 21);
            this.cmbAuthFilter.TabIndex = 5;
            this.cmbAuthFilter.SelectedIndexChanged += new System.EventHandler(this.FilterControl_Changed);
            // 
            // lblStartDate
            // 
            this.lblStartDate.AutoSize = true;
            this.lblStartDate.Location = new System.Drawing.Point(385, 12);
            this.lblStartDate.Name = "lblStartDate";
            this.lblStartDate.Size = new System.Drawing.Size(56, 13);
            this.lblStartDate.TabIndex = 6;
            this.lblStartDate.Text = "Başlangıç:";
            // 
            // dtpStart
            // 
            this.dtpStart.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpStart.Location = new System.Drawing.Point(385, 28);
            this.dtpStart.Name = "dtpStart";
            this.dtpStart.Size = new System.Drawing.Size(85, 20);
            this.dtpStart.TabIndex = 7;
            // 
            // lblEndDate
            // 
            this.lblEndDate.AutoSize = true;
            this.lblEndDate.Location = new System.Drawing.Point(480, 12);
            this.lblEndDate.Name = "lblEndDate";
            this.lblEndDate.Size = new System.Drawing.Size(29, 13);
            this.lblEndDate.TabIndex = 8;
            this.lblEndDate.Text = "Bitiş:";
            // 
            // dtpEnd
            // 
            this.dtpEnd.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpEnd.Location = new System.Drawing.Point(480, 28);
            this.dtpEnd.Name = "dtpEnd";
            this.dtpEnd.Size = new System.Drawing.Size(85, 20);
            this.dtpEnd.TabIndex = 9;
            // 
            // btnFetchLogs
            // 
            this.btnFetchLogs.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(204)))), ((int)(((byte)(113)))));
            this.btnFetchLogs.FlatAppearance.BorderSize = 0;
            this.btnFetchLogs.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFetchLogs.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnFetchLogs.ForeColor = System.Drawing.Color.White;
            this.btnFetchLogs.Location = new System.Drawing.Point(575, 12);
            this.btnFetchLogs.Name = "btnFetchLogs";
            this.btnFetchLogs.Size = new System.Drawing.Size(55, 41);
            this.btnFetchLogs.TabIndex = 10;
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
            this.btnDelete.Text = "Kayıt Sil";
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
            // VehicleIORecords
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.dataGridViewLogs);
            this.Controls.Add(this.panelFilters);
            this.Controls.Add(this.panelButtons);
            this.Name = "VehicleIORecords";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Araç Giriş Çıkış Kayıtları";
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
        private System.Windows.Forms.Label lblPlateFilter;
        private System.Windows.Forms.TextBox txtSearchPlate;
        private System.Windows.Forms.Label lblTypeFilter;
        private System.Windows.Forms.ComboBox cmbTypeFilter;
        private System.Windows.Forms.Label lblAuthFilter;
        private System.Windows.Forms.ComboBox cmbAuthFilter;
        private System.Windows.Forms.Label lblStartDate;
        private System.Windows.Forms.DateTimePicker dtpStart;
        private System.Windows.Forms.Label lblEndDate;
        private System.Windows.Forms.DateTimePicker dtpEnd;
        private System.Windows.Forms.Button btnFetchLogs;
    }
}
