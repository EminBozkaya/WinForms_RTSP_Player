using System;

namespace WinForms_RTSP_Player.Business
{
    /// <summary>
    /// Plaka tespit edildiğinde fırlatılan event arguments
    /// </summary>
    public class PlateDetectedEventArgs : EventArgs
    {
        public string CameraId { get; set; }
        public string Direction { get; set; }
        public string Plate { get; set; }
        public float Confidence { get; set; }
        public DateTime DetectedAt { get; set; }
        public DateTime CapturedAt { get; set; }
        public string OcrPlate { get; set; } // OCR'dan gelen orijinal plaka (sanitize öncesi)
        public int GateOpId { get; set; } // Gate operation ID (tracking için)
    }
}
