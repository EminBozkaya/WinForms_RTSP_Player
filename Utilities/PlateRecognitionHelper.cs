using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;

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
                    // Tüm candidate'ları topla
                    var allCandidates = new List<(string plate, double confidence)>();
                    
                    foreach (var result in results)
                    {
                        var candidates = result["candidates"];
                        if (candidates != null && candidates.HasValues)
                        {
                            foreach (var candidate in candidates)
                            {
                                string plate = candidate["plate"]?.ToString();
                                double confidence = candidate["confidence"]?.Value<double>() ?? 0;
                                
                                if (!string.IsNullOrEmpty(plate))
                                {
                                    allCandidates.Add((plate, confidence));
                                }
                            }
                        }
                    }
                    
                    // En iyi plakayı seç
                    string bestPlate = SelectBestPlate(allCandidates);
                    
                    if (string.IsNullOrEmpty(bestPlate))
                        return "Plaka geçersiz veya okunamadı.";
                    
                    return bestPlate;
                }
            }
            catch { }

            return null;
        }
        
        private static string SelectBestPlate(List<(string plate, double confidence)> candidates)
        {
            if (candidates == null || candidates.Count == 0)
                return null;
                
            // 1. Confidence skoruna göre sırala (yüksekten düşüğe)
            candidates = candidates.OrderByDescending(c => c.confidence).ToList();
            
            // 2. En yüksek confidence'ı al
            var topCandidates = candidates.Where(c => c.confidence >= candidates[0].confidence * 0.8).ToList();
            
            // 3. Karakter bazında analiz yap
            if (topCandidates.Count > 1)
            {
                return AnalyzeByCharacterSimilarity(topCandidates);
            }
            
            return topCandidates[0].plate;
        }
        
        private static string AnalyzeByCharacterSimilarity(List<(string plate, double confidence)> candidates)
        {
            // En uzun plaka uzunluğunu bul
            int maxLength = candidates.Max(c => c.plate.Length);
            
            // Her pozisyon için en çok tekrar eden karakteri bul
            var finalPlate = new char[maxLength];
            
            for (int i = 0; i < maxLength; i++)
            {
                var charVotes = new Dictionary<char, double>();
                
                foreach (var candidate in candidates)
                {
                    if (i < candidate.plate.Length)
                    {
                        char c = candidate.plate[i];
                        if (!charVotes.ContainsKey(c))
                            charVotes[c] = 0;
                        charVotes[c] += candidate.confidence;
                    }
                }
                
                // En yüksek oy alan karakteri seç
                if (charVotes.Count > 0)
                {
                    finalPlate[i] = charVotes.OrderByDescending(kvp => kvp.Value).First().Key;
                }
            }
            
            return new string(finalPlate);
        }
    }

}
