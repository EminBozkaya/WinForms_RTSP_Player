using System;
using System.IO.Ports;
using System.Threading.Tasks;
using WinForms_RTSP_Player.Data;

namespace WinForms_RTSP_Player.Utilities
{
    public class HardwareController : IDisposable
    {
        private static HardwareController _instance;
        private static readonly object _lock = new object();
        private SerialPort _serialPort;
        private bool _isInitialized = false;

        public static HardwareController Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new HardwareController();
                    return _instance;
                }
            }
        }

        private HardwareController()
        {
        }

        public bool Initialize(string portName, int baudRate = 9600)
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                }

                _serialPort = new SerialPort(portName, baudRate);
                _serialPort.ReadTimeout = 500;
                _serialPort.WriteTimeout = 500;
                _serialPort.Open();

                _isInitialized = true;
                DatabaseManager.Instance.LogSystem("INFO", $"Donanım Portu Açıldı: {portName}", "HardwareController.Initialize");
                return true;
            }
            catch (Exception ex)
            {
                _isInitialized = false;
                DatabaseManager.Instance.LogSystem("ERROR", $"Donanım Portu Açılamadı: {portName}", "HardwareController.Initialize", ex.ToString());
                return false;
            }
        }

        public async Task<bool> OpenGateAsync()
        {
            if (!_isInitialized || _serialPort == null || !_serialPort.IsOpen)
            {
                DatabaseManager.Instance.LogSystem("WARNING", "Kapı açma isteği gönderilemedi: Port hazır değil.", "HardwareController.OpenGateAsync");
#if DEBUG
                Console.WriteLine("WARNING!!!!!! Kapı açma isteği gönderilemedi: Port hazır değil.");
#endif
                return false;
            }

            try
            {
                // Arduino sketch'imizde beklediğimiz kod: "OPEN_GATE"
                _serialPort.WriteLine("OPEN_GATE");
                
                DatabaseManager.Instance.LogSystem("INFO", "Kapı açma komutu gönderildi (OPEN_GATE)", "HardwareController.OpenGateAsync");
#if DEBUG
                Console.WriteLine("OPEN_GATE ===>>>>>> Kapı açma komutu gönderildi");
#endif

                // Geri bildirimi okumayı deneyebiliriz (opsiyonel)
                // string response = _serialPort.ReadLine();

                return true;
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Kapı açma komutu gönderilirken hata oluştu", "HardwareController.OpenGateAsync", ex.ToString());
                return false;
            }
        }

        public void Dispose()
        {
            if (_serialPort != null)
            {
                if (_serialPort.IsOpen)
                    _serialPort.Close();
                _serialPort.Dispose();
            }
        }
    }
}
