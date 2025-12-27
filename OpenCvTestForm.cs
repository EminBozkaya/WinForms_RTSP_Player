using System;
using System.Drawing;
using System.Windows.Forms;
using WinForms_RTSP_Player.Business;
using WinForms_RTSP_Player.Utilities;

namespace WinForms_RTSP_Player
{
    public partial class OpenCvTestForm : Form
    {
        private OpenCvStreamCapture? _streamCapture;
        private PictureBox pictureBoxStream;
        private Label labelFps;
        private Label labelStatus;
        private Button btnStart;
        private Button btnStop;
        private TextBox txtRtspUrl;

        public OpenCvTestForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "OpenCV Stream Test";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // RTSP URL TextBox
            txtRtspUrl = new TextBox
            {
                Location = new System.Drawing.Point(10, 10),
                Size = new Size(600, 25),
                Text = "rtsp://admin:admin@192.168.1.100:554/stream" // Placeholder
            };
            this.Controls.Add(txtRtspUrl);

            // Start Button
            btnStart = new Button
            {
                Location = new System.Drawing.Point(620, 10),
                Size = new Size(75, 25),
                Text = "Start"
            };
            btnStart.Click += BtnStart_Click;
            this.Controls.Add(btnStart);

            // Stop Button
            btnStop = new Button
            {
                Location = new System.Drawing.Point(700, 10),
                Size = new Size(75, 25),
                Text = "Stop",
                Enabled = false
            };
            btnStop.Click += BtnStop_Click;
            this.Controls.Add(btnStop);

            // PictureBox for stream
            pictureBoxStream = new PictureBox
            {
                Location = new System.Drawing.Point(10, 45),
                Size = new Size(760, 450),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };
            this.Controls.Add(pictureBoxStream);

            // FPS Label
            labelFps = new Label
            {
                Location = new System.Drawing.Point(10, 505),
                Size = new Size(200, 20),
                Text = "FPS: 0.0"
            };
            this.Controls.Add(labelFps);

            // Status Label
            labelStatus = new Label
            {
                Location = new System.Drawing.Point(10, 530),
                Size = new Size(760, 20),
                Text = "Status: Disconnected"
            };
            this.Controls.Add(labelStatus);

            this.FormClosing += OpenCvTestForm_FormClosing;
        }

        private void BtnStart_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRtspUrl.Text))
            {
                MessageBox.Show("RTSP URL giriniz!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _streamCapture = new OpenCvStreamCapture("TEST_CAM", txtRtspUrl.Text);
                _streamCapture.FrameReceived += StreamCapture_FrameReceived;
                _streamCapture.StateChanged += StreamCapture_StateChanged;
                _streamCapture.Start();

                btnStart.Enabled = false;
                btnStop.Enabled = true;
                txtRtspUrl.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Başlatma hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnStop_Click(object? sender, EventArgs e)
        {
            StopStream();
        }

        private void StopStream()
        {
            if (_streamCapture != null)
            {
                _streamCapture.FrameReceived -= StreamCapture_FrameReceived;
                _streamCapture.StateChanged -= StreamCapture_StateChanged;
                _streamCapture.Stop();
                _streamCapture.Dispose();
                _streamCapture = null;
            }

            btnStart.Enabled = true;
            btnStop.Enabled = false;
            txtRtspUrl.Enabled = true;

            if (pictureBoxStream.Image != null)
            {
                pictureBoxStream.Image.Dispose();
                pictureBoxStream.Image = null;
            }

            labelStatus.Text = "Status: Disconnected";
            labelFps.Text = "FPS: 0.0";
        }

        private void StreamCapture_FrameReceived(object? sender, FrameReceivedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => StreamCapture_FrameReceived(sender, e)));
                return;
            }

            try
            {
                // Convert Mat to Bitmap
                var bitmap = e.Frame.ToBitmap();
                if (bitmap != null)
                {
                    // Dispose old image
                    var oldImage = pictureBoxStream.Image;
                    pictureBoxStream.Image = bitmap;
                    oldImage?.Dispose();
                }

                // Update FPS
                if (_streamCapture != null)
                {
                    labelFps.Text = $"FPS: {_streamCapture.CurrentFps:F1}";
                }

                // Dispose frame
                e.Frame?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Frame rendering error: {ex.Message}");
            }
        }

        private void StreamCapture_StateChanged(object? sender, ConnectionStateChangedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => StreamCapture_StateChanged(sender, e)));
                return;
            }

            labelStatus.Text = $"Status: {e.State} - {e.Message}";
            labelStatus.ForeColor = e.State == ConnectionState.Connected ? Color.Green :
                                     e.State == ConnectionState.Error ? Color.Red :
                                     Color.Orange;
        }

        private void OpenCvTestForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            StopStream();
        }
    }
}
