using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using WinForms_RTSP_Player.Utilities;

namespace WinForms_RTSP_Player
{
    public partial class TestForm : Form
    {
        public TestForm()
        {
            InitializeComponent();
            this.Load += new System.EventHandler(this.TestForm_Load);
        }

        private async void TestForm_Load(object sender, EventArgs e)
        {
            await webView21.EnsureCoreWebView2Async();
            webView21.Source = new Uri("https://www.google.com");
        }

        //private void btnCapture_Click(object sender, EventArgs e)
        //{
        //    // Ekran görüntüsü al
        //    Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
        //    using (Graphics g = Graphics.FromImage(bmp))
        //    {
        //        g.CopyFromScreen(Point.Empty, Point.Empty, bmp.Size);
        //    }

        //    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screenshot.jpg");
        //    bmp.Save(path, ImageFormat.Jpeg);

        //    pictureBox1.Image = bmp;

        //    // OpenALPR ile analiz et
        //    string result = PlateRecognitionHelper.RunOpenALPR(path);
        //    string plate = PlateRecognitionHelper.ExtractPlateFromJson(result);
        //    lblResult.Text = string.IsNullOrEmpty(plate)
        //        ? "Plaka tespit edilemedi."
        //        : $"Tespit Edilen Plaka: {plate}";

        //    if (File.Exists(path))
        //        File.Delete(path);
        //}
        private void btnCapture_Click(object sender, EventArgs e)
        {
            // Sadece WebView2 kontrolünün ekran görüntüsünü al
            Rectangle bounds = webView21.Bounds;
            Bitmap bmp = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                // Formun üzerindeki kontrolün ekran koordinatına göre konumunu bul
                Point controlLocation = webView21.PointToScreen(Point.Empty);
                g.CopyFromScreen(controlLocation, Point.Empty, bounds.Size);
            }

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screenshot.jpg");
            bmp.Save(path, ImageFormat.Jpeg);

            pictureBox1.Image = bmp;

            // OpenALPR ile analiz
            string result = PlateRecognitionHelper.RunOpenALPR(path);
            string plate = PlateRecognitionHelper.ExtractPlateFromJson(result);
            lblResult.Text = string.IsNullOrEmpty(plate)
                ? "Plaka tespit edilemedi."
                : $"Tespit Edilen Plaka: {plate}";

            if (File.Exists(path))
                File.Delete(path);
        }

    }

}
