namespace WinForms_RTSP_Player
{
    partial class AdminForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AdminForm));
            centerPanel = new FlowLayoutPanel();
            btnPlates = new Button();
            btnEntryExit = new Button();
            btnSystemLogs = new Button();
            btnSystemParameters = new Button();
            btnExit = new Button();
            centerPanel.SuspendLayout();
            SuspendLayout();
            // 
            // centerPanel
            // 
            centerPanel.AutoSize = true;
            centerPanel.BackColor = Color.Transparent;
            centerPanel.Controls.Add(btnPlates);
            centerPanel.Controls.Add(btnEntryExit);
            centerPanel.Controls.Add(btnSystemLogs);
            centerPanel.Controls.Add(btnSystemParameters);
            centerPanel.FlowDirection = FlowDirection.TopDown;
            centerPanel.Location = new Point(233, 58);
            centerPanel.Margin = new Padding(4, 3, 4, 3);
            centerPanel.Name = "centerPanel";
            centerPanel.Size = new Size(475, 552);
            centerPanel.TabIndex = 0;
            centerPanel.WrapContents = false;
            // 
            // btnPlates
            // 
            btnPlates.BackColor = Color.FromArgb(60, 65, 75);
            btnPlates.Cursor = Cursors.Hand;
            btnPlates.FlatAppearance.BorderSize = 0;
            btnPlates.FlatStyle = FlatStyle.Flat;
            btnPlates.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            btnPlates.ForeColor = Color.White;
            btnPlates.Image = (Image)resources.GetObject("btnPlates.Image");
            btnPlates.ImageAlign = ContentAlignment.MiddleLeft;
            btnPlates.Location = new Point(4, 17);
            btnPlates.Margin = new Padding(4, 17, 4, 17);
            btnPlates.Name = "btnPlates";
            btnPlates.Padding = new Padding(29, 0, 0, 0);
            btnPlates.Size = new Size(467, 104);
            btnPlates.TabIndex = 0;
            btnPlates.Text = "  Kayıtlı Araç Plaka Listesi";
            btnPlates.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnPlates.UseVisualStyleBackColor = false;
            btnPlates.Click += btnPlates_Click;
            // 
            // btnEntryExit
            // 
            btnEntryExit.BackColor = Color.FromArgb(60, 65, 75);
            btnEntryExit.Cursor = Cursors.Hand;
            btnEntryExit.FlatAppearance.BorderSize = 0;
            btnEntryExit.FlatStyle = FlatStyle.Flat;
            btnEntryExit.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            btnEntryExit.ForeColor = Color.White;
            btnEntryExit.Image = (Image)resources.GetObject("btnEntryExit.Image");
            btnEntryExit.ImageAlign = ContentAlignment.MiddleLeft;
            btnEntryExit.Location = new Point(4, 155);
            btnEntryExit.Margin = new Padding(4, 17, 4, 17);
            btnEntryExit.Name = "btnEntryExit";
            btnEntryExit.Padding = new Padding(29, 0, 0, 0);
            btnEntryExit.Size = new Size(467, 104);
            btnEntryExit.TabIndex = 1;
            btnEntryExit.Text = "  Araç Giriş/Çıkış Kayıtları";
            btnEntryExit.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnEntryExit.UseVisualStyleBackColor = false;
            btnEntryExit.Click += btnEntryExit_Click;
            // 
            // btnSystemLogs
            // 
            btnSystemLogs.BackColor = Color.FromArgb(60, 65, 75);
            btnSystemLogs.Cursor = Cursors.Hand;
            btnSystemLogs.FlatAppearance.BorderSize = 0;
            btnSystemLogs.FlatStyle = FlatStyle.Flat;
            btnSystemLogs.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            btnSystemLogs.ForeColor = Color.White;
            btnSystemLogs.Image = (Image)resources.GetObject("btnSystemLogs.Image");
            btnSystemLogs.ImageAlign = ContentAlignment.MiddleLeft;
            btnSystemLogs.Location = new Point(4, 293);
            btnSystemLogs.Margin = new Padding(4, 17, 4, 17);
            btnSystemLogs.Name = "btnSystemLogs";
            btnSystemLogs.Padding = new Padding(29, 0, 0, 0);
            btnSystemLogs.Size = new Size(467, 104);
            btnSystemLogs.TabIndex = 2;
            btnSystemLogs.Text = "  Sistem Genel Kayıtları";
            btnSystemLogs.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnSystemLogs.UseVisualStyleBackColor = false;
            btnSystemLogs.Click += btnSystemLogs_Click;
            // 
            // btnSystemParameters
            // 
            btnSystemParameters.BackColor = Color.FromArgb(60, 65, 75);
            btnSystemParameters.Cursor = Cursors.Hand;
            btnSystemParameters.FlatAppearance.BorderSize = 0;
            btnSystemParameters.FlatStyle = FlatStyle.Flat;
            btnSystemParameters.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            btnSystemParameters.ForeColor = Color.White;
            btnSystemParameters.Image = (Image)resources.GetObject("btnSystemParameters.Image");
            btnSystemParameters.ImageAlign = ContentAlignment.MiddleLeft;
            btnSystemParameters.Location = new Point(4, 431);
            btnSystemParameters.Margin = new Padding(4, 17, 4, 17);
            btnSystemParameters.Name = "btnSystemParameters";
            btnSystemParameters.Padding = new Padding(29, 0, 0, 0);
            btnSystemParameters.Size = new Size(467, 104);
            btnSystemParameters.TabIndex = 4;
            btnSystemParameters.Text = "  Sistem Parametreleri";
            btnSystemParameters.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnSystemParameters.UseVisualStyleBackColor = false;
            btnSystemParameters.Click += btnSystemParameters_Click;
            // 
            // btnExit
            // 
            btnExit.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnExit.BackColor = Color.Maroon;
            btnExit.Cursor = Cursors.Hand;
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.FlatStyle = FlatStyle.Flat;
            btnExit.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnExit.ForeColor = Color.White;
            btnExit.Location = new Point(23, 582);
            btnExit.Margin = new Padding(4, 3, 4, 3);
            btnExit.Name = "btnExit";
            btnExit.Size = new Size(163, 58);
            btnExit.TabIndex = 3;
            btnExit.Text = "Ana Sayfaya Dön";
            btnExit.UseVisualStyleBackColor = false;
            btnExit.Click += btnExit_Click;
            // 
            // AdminForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(40, 44, 52);
            ClientSize = new Size(945, 663);
            Controls.Add(btnExit);
            Controls.Add(centerPanel);
            Margin = new Padding(4, 3, 4, 3);
            Name = "AdminForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Admin Panel";
            centerPanel.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel centerPanel;
        private System.Windows.Forms.Button btnPlates;
        private System.Windows.Forms.Button btnEntryExit;
        private System.Windows.Forms.Button btnSystemLogs;
        private System.Windows.Forms.Button btnSystemParameters;
        private System.Windows.Forms.Button btnExit;
    }
}