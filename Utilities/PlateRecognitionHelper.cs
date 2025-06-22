using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace WinForms_RTSP_Player.Utilities
{
    public static class PlateRecognitionHelper
    {
        public static string RunOpenALPR(string imagePath)
        {
            var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "alpr", "alpr.exe");

            if (!File.Exists(exePath))
            {
                MessageBox.Show($"alpr.exe bulunamadı: {exePath}");
                return null;
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"-j -c eu \"{imagePath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(exePath)
            };

            using (var process = Process.Start(processInfo))
            using (var reader = process.StandardOutput)
            {
                return reader.ReadToEnd();
            }
        }

        public static string ExtractPlateFromJson(string json)
        {
            try
            {
                var j = JObject.Parse(json);
                var results = j["results"];
                if (results != null && results.HasValues)
                {
                    string rawPlate = results[0]["plate"]?.ToString();

                    string fixedPlate = PlateSanitizer.ValidateTurkishPlateFormat(rawPlate);

                    if (fixedPlate == null)
                        return "Plaka geçersiz veya okunamadı.";

                    return fixedPlate;
                }
            }
            catch { }

            return null;
        }
    }

}
