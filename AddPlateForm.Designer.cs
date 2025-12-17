namespace WinForms_RTSP_Player
{
    partial class AddPlateForm
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
            lblPlate = new Label();
            txtPlate = new TextBox();
            lblOwner = new Label();
            txtOwner = new TextBox();
            lblType = new Label();
            cmbType = new ComboBox();
            btnSave = new Button();
            btnCancel = new Button();
            panelContent = new Panel();
            panelContent.SuspendLayout();
            SuspendLayout();
            // 
            // lblPlate
            // 
            lblPlate.AutoSize = true;
            lblPlate.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblPlate.Location = new Point(23, 29);
            lblPlate.Margin = new Padding(4, 0, 4, 0);
            lblPlate.Name = "lblPlate";
            lblPlate.Size = new Size(46, 19);
            lblPlate.TabIndex = 0;
            lblPlate.Text = "Plaka";
            // 
            // txtPlate
            // 
            txtPlate.Font = new Font("Segoe UI", 10F);
            txtPlate.Location = new Point(28, 54);
            txtPlate.Margin = new Padding(4, 3, 4, 3);
            txtPlate.Name = "txtPlate";
            txtPlate.Size = new Size(410, 25);
            txtPlate.TabIndex = 1;
            // 
            // lblOwner
            // 
            lblOwner.AutoSize = true;
            lblOwner.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblOwner.Location = new Point(23, 104);
            lblOwner.Margin = new Padding(4, 0, 4, 0);
            lblOwner.Name = "lblOwner";
            lblOwner.Size = new Size(85, 19);
            lblOwner.TabIndex = 2;
            lblOwner.Text = "Araç Sahibi";
            // 
            // txtOwner
            // 
            txtOwner.Font = new Font("Segoe UI", 10F);
            txtOwner.Location = new Point(28, 129);
            txtOwner.Margin = new Padding(4, 3, 4, 3);
            txtOwner.Name = "txtOwner";
            txtOwner.Size = new Size(410, 25);
            txtOwner.TabIndex = 3;
            // 
            // lblType
            // 
            lblType.AutoSize = true;
            lblType.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblType.Location = new Point(23, 179);
            lblType.Margin = new Padding(4, 0, 4, 0);
            lblType.Name = "lblType";
            lblType.Size = new Size(69, 19);
            lblType.TabIndex = 4;
            lblType.Text = "Araç Tipi";
            // 
            // cmbType
            // 
            cmbType.Font = new Font("Segoe UI", 10F);
            cmbType.FormattingEnabled = true;
            cmbType.Items.AddRange(new object[] { "Binek Araç", "Motosiklet", "Dolmuş/Panelvan", "Minibüs/Otobüs", "Kamyon/Tır", "Kamu Aracı", "Ticari Araç" });
            cmbType.Location = new Point(28, 204);
            cmbType.Margin = new Padding(4, 3, 4, 3);
            cmbType.Name = "cmbType";
            cmbType.Size = new Size(410, 25);
            cmbType.TabIndex = 5;
            cmbType.Text = "Binek Araç";
            // 
            // btnSave
            // 
            btnSave.BackColor = Color.FromArgb(46, 204, 113);
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnSave.ForeColor = Color.White;
            btnSave.Location = new Point(233, 271);
            btnSave.Margin = new Padding(4, 3, 4, 3);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(205, 52);
            btnSave.TabIndex = 6;
            btnSave.Text = "Kaydı Ekle";
            btnSave.UseVisualStyleBackColor = false;
            btnSave.Click += btnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.BackColor = Color.FromArgb(149, 165, 166);
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnCancel.ForeColor = Color.White;
            btnCancel.Location = new Point(28, 271);
            btnCancel.Margin = new Padding(4, 3, 4, 3);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(175, 52);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "İptal";
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Click += btnCancel_Click;
            // 
            // panelContent
            // 
            panelContent.BackColor = Color.White;
            panelContent.Controls.Add(btnCancel);
            panelContent.Controls.Add(btnSave);
            panelContent.Controls.Add(cmbType);
            panelContent.Controls.Add(lblType);
            panelContent.Controls.Add(txtOwner);
            panelContent.Controls.Add(lblOwner);
            panelContent.Controls.Add(txtPlate);
            panelContent.Controls.Add(lblPlate);
            panelContent.Dock = DockStyle.Fill;
            panelContent.Location = new Point(0, 0);
            panelContent.Margin = new Padding(4, 3, 4, 3);
            panelContent.Name = "panelContent";
            panelContent.Padding = new Padding(23, 23, 23, 23);
            panelContent.Size = new Size(467, 346);
            panelContent.TabIndex = 0;
            // 
            // AddPlateForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(467, 346);
            Controls.Add(panelContent);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AddPlateForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Yeni Araç Ekle";
            panelContent.ResumeLayout(false);
            panelContent.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelContent;
        private System.Windows.Forms.Label lblPlate;
        private System.Windows.Forms.TextBox txtPlate;
        private System.Windows.Forms.Label lblOwner;
        private System.Windows.Forms.TextBox txtOwner;
        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.ComboBox cmbType;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
    }
}
