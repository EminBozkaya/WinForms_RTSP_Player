using LibVLCSharp.Shared;
using System;
//using System.Diagnostics;
using System.Windows.Forms;

namespace WinForms_RTSP_Player.Business
{
    // Yeni sınıf: RTSP Bağlantısını gözleyen ve gerektiğinde yeniden başlatan bir yöneticidir
    public class ReconnectManager
    {
        private readonly MediaPlayer _mediaPlayer;
        private readonly Func<Media> _mediaFactory; // RTSP URL'yi veren fonksiyon
        private readonly System.Windows.Forms.Timer _reconnectTimer;

        public ReconnectManager(MediaPlayer mediaPlayer, Func<Media> mediaFactory)
        {
            _mediaPlayer = mediaPlayer;
            _mediaFactory = mediaFactory;

            _mediaPlayer.EncounteredError += MediaPlayer_ConnectionIssue;
            _mediaPlayer.Stopped += MediaPlayer_ConnectionIssue;

            _reconnectTimer = new System.Windows.Forms.Timer();
            _reconnectTimer.Interval = 3000; // 3 saniyede bir yeniden dene
            _reconnectTimer.Tick += (s, e) => AttemptReconnect();
        }

        private void MediaPlayer_ConnectionIssue(object sender, EventArgs e)
        {
            Console.WriteLine("RTSP bağlantı sorunu algılandı, yeniden bağlanılacak...");
            StartReconnectLoop();
        }

        private void StartReconnectLoop()
        {
            if (!_reconnectTimer.Enabled)
            {
                _reconnectTimer.Start();
            }
        }

        private void AttemptReconnect()
        {
            if (!_mediaPlayer.IsPlaying)
            {
                try
                {
                    Console.WriteLine("RTSP yeniden bağlanma denemesi...");
                    _mediaPlayer.Play(_mediaFactory.Invoke());
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Yeniden bağlanma hatası: " + ex.Message);
                }
            }
            else
            {
                _reconnectTimer.Stop();
                Console.WriteLine("Yeniden bağlantı başarılı, timer durdu.");
            }
        }

        public void Dispose()
        {
            _reconnectTimer?.Stop();
            _reconnectTimer?.Dispose();
        }
    }
}
