using LibVLCSharp.Shared;
using System;
//using System.Diagnostics;
using WinForms_RTSP_Player.Data;

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
            DatabaseManager.Instance.LogSystem("WARNING", "RTSP bağlantı sorunu algılandı", "ReconnectManager.ConnectionIssue");
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
                    DatabaseManager.Instance.LogSystem("INFO", "RTSP yeniden bağlanma denemesi...", "ReconnectManager.AttemptReconnect");
                    _mediaPlayer.Play(_mediaFactory.Invoke());
                }
                catch (Exception ex)
                {
                    DatabaseManager.Instance.LogSystem("ERROR", "Yeniden bağlanma hatası", "ReconnectManager.AttemptReconnect", ex.ToString());
                }
            }
            else
            {
                _reconnectTimer.Stop();
                DatabaseManager.Instance.LogSystem("INFO", "Yeniden bağlantı başarılı, timer durdu.", "ReconnectManager.AttemptReconnect");
            }
        }

        public void Dispose()
        {
            _reconnectTimer?.Stop();
            _reconnectTimer?.Dispose();
        }
    }
}
