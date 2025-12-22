namespace WinForms_RTSP_Player
{
    partial class EditParameterForm
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
            lblDetail = new Label();
            lblCurrentValue = new Label();
            txtNewValue = new TextBox();
            btnUpdate = new Button();
            btnCancel = new Button();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            SuspendLayout();
            // 
            // lblDetail
            // 
            lblDetail.AutoSize = true;
            lblDetail.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblDetail.ForeColor = Color.White;
            lblDetail.Location = new Point(12, 35);
            lblDetail.MaximumSize = new Size(360, 0);
            lblDetail.Name = "lblDetail";
            lblDetail.Size = new Size(155, 19);
            lblDetail.TabIndex = 0;
            lblDetail.Text = "Parametre Açıklaması";
            // 
            // lblCurrentValue
            // 
            lblCurrentValue.AutoSize = true;
            lblCurrentValue.Font = new Font("Segoe UI", 10F);
            lblCurrentValue.ForeColor = Color.LightGray;
            lblCurrentValue.Location = new Point(12, 90);
            lblCurrentValue.Name = "lblCurrentValue";
            lblCurrentValue.Size = new Size(92, 19);
            lblCurrentValue.TabIndex = 1;
            lblCurrentValue.Text = "Mevcut Değer";
            // 
            // txtNewValue
            // 
            txtNewValue.Font = new Font("Segoe UI", 10F);
            txtNewValue.Location = new Point(12, 145);
            txtNewValue.Name = "txtNewValue";
            txtNewValue.Size = new Size(360, 25);
            txtNewValue.TabIndex = 2;
            // 
            // btnUpdate
            // 
            btnUpdate.BackColor = Color.FromArgb(60, 65, 75);
            btnUpdate.FlatStyle = FlatStyle.Flat;
            btnUpdate.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnUpdate.ForeColor = Color.White;
            btnUpdate.Location = new Point(176, 190);
            btnUpdate.Name = "btnUpdate";
            btnUpdate.Size = new Size(95, 35);
            btnUpdate.TabIndex = 3;
            btnUpdate.Text = "Güncelle";
            btnUpdate.UseVisualStyleBackColor = false;
            btnUpdate.Click += btnUpdate_Click;
            // 
            // btnCancel
            // 
            btnCancel.BackColor = Color.Maroon;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnCancel.ForeColor = Color.White;
            btnCancel.Location = new Point(277, 190);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(95, 35);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "İptal";
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Click += btnCancel_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            label1.ForeColor = Color.Gray;
            label1.Location = new Point(12, 15);
            label1.Name = "label1";
            label1.Size = new Size(60, 15);
            label1.TabIndex = 5;
            label1.Text = "Parametre";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            label2.ForeColor = Color.Gray;
            label2.Location = new Point(12, 70);
            label2.Name = "label2";
            label2.Size = new Size(79, 15);
            label2.TabIndex = 6;
            label2.Text = "Mevcut Değer";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            label3.ForeColor = Color.Gray;
            label3.Location = new Point(12, 125);
            label3.Name = "label3";
            label3.Size = new Size(64, 15);
            label3.TabIndex = 7;
            label3.Text = "Yeni Değer";
            // 
            // EditParameterForm
            // 
            AcceptButton = btnUpdate;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(40, 44, 52);
            CancelButton = btnCancel;
            ClientSize = new Size(384, 241);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(btnCancel);
            Controls.Add(btnUpdate);
            Controls.Add(txtNewValue);
            Controls.Add(lblCurrentValue);
            Controls.Add(lblDetail);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "EditParameterForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Parametre Güncelle";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblDetail;
        private System.Windows.Forms.Label lblCurrentValue;
        private System.Windows.Forms.TextBox txtNewValue;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
    }
}
