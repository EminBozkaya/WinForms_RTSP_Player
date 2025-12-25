namespace WinForms_RTSP_Player
{
    partial class AddParameterForm
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
            label1 = new Label();
            txtName = new TextBox();
            label2 = new Label();
            txtValue = new TextBox();
            label3 = new Label();
            txtDetail = new TextBox();
            btnSave = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 9F);
            label1.ForeColor = Color.Gray;
            label1.Location = new Point(12, 15);
            label1.Name = "label1";
            label1.Size = new Size(81, 15);
            label1.TabIndex = 0;
            label1.Text = "Parametre Adı";
            // 
            // txtName
            // 
            txtName.Font = new Font("Segoe UI", 10F);
            txtName.Location = new Point(12, 35);
            txtName.Name = "txtName";
            txtName.Size = new Size(360, 25);
            txtName.TabIndex = 1;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 9F);
            label2.ForeColor = Color.Gray;
            label2.Location = new Point(12, 75);
            label2.Name = "label2";
            label2.Size = new Size(37, 15);
            label2.TabIndex = 2;
            label2.Text = "Değer";
            // 
            // txtValue
            // 
            txtValue.Font = new Font("Segoe UI", 10F);
            txtValue.Location = new Point(12, 95);
            txtValue.Name = "txtValue";
            txtValue.Size = new Size(360, 25);
            txtValue.TabIndex = 3;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 9F);
            label3.ForeColor = Color.Gray;
            label3.Location = new Point(12, 135);
            label3.Name = "label3";
            label3.Size = new Size(122, 15);
            label3.TabIndex = 4;
            label3.Text = "Parametre Açıklaması";
            // 
            // txtDetail
            // 
            txtDetail.Font = new Font("Segoe UI", 10F);
            txtDetail.Location = new Point(12, 155);
            txtDetail.Name = "txtDetail";
            txtDetail.Size = new Size(360, 25);
            txtDetail.TabIndex = 5;
            // 
            // btnSave
            // 
            btnSave.BackColor = Color.FromArgb(46, 204, 113);
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSave.ForeColor = Color.White;
            btnSave.Location = new Point(176, 200);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(95, 35);
            btnSave.TabIndex = 6;
            btnSave.Text = "Kaydet";
            btnSave.UseVisualStyleBackColor = false;
            btnSave.Click += btnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.BackColor = Color.Maroon;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnCancel.ForeColor = Color.White;
            btnCancel.Location = new Point(277, 200);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(95, 35);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "İptal";
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Click += btnCancel_Click;
            // 
            // AddParameterForm
            // 
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(40, 44, 52);
            CancelButton = btnCancel;
            ClientSize = new Size(384, 251);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(txtDetail);
            Controls.Add(label3);
            Controls.Add(txtValue);
            Controls.Add(label2);
            Controls.Add(txtName);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AddParameterForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Yeni Parametre Ekle";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox txtName;
        private Label label2;
        private TextBox txtValue;
        private Label label3;
        private TextBox txtDetail;
        private Button btnSave;
        private Button btnCancel;
    }
}
