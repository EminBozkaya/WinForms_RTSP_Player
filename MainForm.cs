using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using Newtonsoft.Json.Linq;
using WinForms_RTSP_Player.Utilities;
using System.Configuration;

namespace WinForms_RTSP_Player
{
    public partial class MainForm : Form
    {
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private Timer _frameCaptureTimer;

        //private Timer _streamHealthTimer;          // Stream sağlığı için timer
        //private DateTime _lastVideoUpdateTime;     // Son video frame zaman damgası

        //private string _rtspUrl = "rtsp://192.168.0.101/user=admin_password=tlJwpbo6_channel=1_stream=0.sdp?real_stream";//Eski kamera
        private string _rtspUrl = string.Empty; // RTSP URL'si App.config'den alınacak

        public MainForm()
        {
            InitializeComponent();
            Core.Initialize(@"libvlc\win-x64");

            _rtspUrl = ConfigurationManager.AppSettings["RtspUrl"];
            if (string.IsNullOrEmpty(_rtspUrl))
            {
                MessageBox.Show("RTSP bağlantı adresi App.config dosyasında bulunamadı!");
                return;
            }

            var libvlcOptions = new[]
            {
                "--network-caching=50",
                //"--rtsp-tcp",
                //"--no-skip-frames",
                "--no-video-title-show",
                //"--video-title=0",
                "--no-osd",
                //"--no-overlay",
                "--no-snapshot-preview",


                //"--rtsp-timeout=60",
                //"--rtsp-frame-buffer-size=100000",
                //"--avcodec-hw=auto",
                "--avcodec-hw=dxva2",
                "--clock-synchro=1",
                "--clock-jitter=0",
            };

            _libVLC = new LibVLC(libvlcOptions);
            _mediaPlayer = new MediaPlayer(_libVLC);
            _mediaPlayer.Mute = true;
            videoView1.MediaPlayer = _mediaPlayer;

            //// Video frame geldiğinde zaman damgasını güncelle
            //_mediaPlayer.TimeChanged += (s, e) =>
            //{
            //    _lastVideoUpdateTime = DateTime.Now;
            //};

            _frameCaptureTimer = new Timer { Interval = 2000 };
            _frameCaptureTimer.Tick += FrameCaptureTimer_Tick;

            //// Stream sağlık kontrol timer
            //_streamHealthTimer = new Timer { Interval = 5000 }; // 5 saniyede bir kontrol
            //_streamHealthTimer.Tick += (s, e) => CheckStreamHealth();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            _mediaPlayer.Play(new Media(_libVLC, _rtspUrl, FromType.FromLocation));
            Debug.WriteLine($"Media oynatıcısı başlatıldı: {DateTime.Now}");
            _frameCaptureTimer.Start();

            //_lastVideoUpdateTime = DateTime.Now;
            //_streamHealthTimer.Start();
        }

        private void FrameCaptureTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                string tempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp.jpg");
                bool success = _mediaPlayer.TakeSnapshot(0, tempPath, 0, 0);

                if (success && File.Exists(tempPath))
                {
                    string result = PlateRecognitionHelper.RunOpenALPR(tempPath);
                    string plate = PlateRecognitionHelper.ExtractPlateFromJson(result);
                    lblPlate.Text = !string.IsNullOrEmpty(plate) ? $"Tespit Edilen Plaka: {plate}" : "Tespit Edilen Plaka: ---";
                    Debug.WriteLine($"*******************{DateTime.Now}****************************");
                    Debug.WriteLine($"OCR okunan: {result}");
                    Debug.WriteLine($"-----");
                    Debug.WriteLine($"Tespit Edilen Plaka: {plate}");
                    File.Delete(tempPath);
                }
                else Debug.WriteLine("🎯 Ekran görüntüsü alınamadı veya dosya bulunamadı.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Hata: " + ex.Message);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _frameCaptureTimer.Stop();
            _frameCaptureTimer.Dispose();

            //_streamHealthTimer.Stop();
            //_streamHealthTimer.Dispose();

            _mediaPlayer.Stop();
            _mediaPlayer.Dispose();
            _libVLC.Dispose();
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            var testForm = new TestForm();
            testForm.Show();
        }

        //private void CheckStreamHealth()
        //{
        //    var secondsSinceLastFrame = (DateTime.Now - _lastVideoUpdateTime).TotalSeconds;

        //    if (secondsSinceLastFrame > 3) // 3 saniye boyunca yeni frame gelmediyse
        //    {
        //        Debug.WriteLine($"⚠ Frame akışı {secondsSinceLastFrame:F1} sn durdu. RTSP yeniden başlatılıyor...{DateTime.Now}");
        //        try
        //        {
        //            _mediaPlayer.Stop();
        //            _mediaPlayer.Play(new Media(_libVLC, _rtspUrl, FromType.FromLocation));
        //            Debug.WriteLine($"Media oynatıcısı başlatıldı: {DateTime.Now}");
        //            _lastVideoUpdateTime = DateTime.Now;
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.WriteLine("🎯 Yeniden bağlantı hatası: " + ex.Message);
        //        }
        //    }
        //}

    }
}
