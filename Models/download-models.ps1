# ONNX Model İndirme Scripti
# Bu script, gerekli ONNX modellerini Hugging Face'den indirir

$ModelsDir = "$PSScriptRoot"
Write-Host "Models dizini: $ModelsDir" -ForegroundColor Cyan

# YOLOv8 Plate Detection Model
$yoloUrl = "https://huggingface.co/keremberke/yolov8m-license-plate-detection/resolve/main/best.onnx"
$yoloOutput = Join-Path $ModelsDir "yolov8n-plate-detection.onnx"

Write-Host "`n[1/2] YOLOv8 Plate Detection modeli indiriliyor..." -ForegroundColor Yellow
Write-Host "URL: $yoloUrl" -ForegroundColor Gray

if (Test-Path $yoloOutput) {
    Write-Host "Model zaten mevcut: $yoloOutput" -ForegroundColor Green
} 
else {
    try {
        Invoke-WebRequest -Uri $yoloUrl -OutFile $yoloOutput -UseBasicParsing
        Write-Host "OK Indirme tamamlandi: $yoloOutput" -ForegroundColor Green
        $fileSize = (Get-Item $yoloOutput).Length / 1MB
        Write-Host "  Boyut: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Gray
    }
    catch {
        Write-Host "X Indirme hatasi: $_" -ForegroundColor Red
        Write-Host "Manuel indirme gerekli: $yoloUrl" -ForegroundColor Yellow
    }
}

# PaddleOCR Model
Write-Host "`n[2/2] PaddleOCR modeli icin manuel indirme gerekli" -ForegroundColor Yellow
Write-Host "Sebep: Hugging Face API token gerektirebilir veya direct link yok" -ForegroundColor Gray
Write-Host "`nManuel adimlar:" -ForegroundColor Cyan
Write-Host "1. Tarayicida ac: https://huggingface.co/monkt/paddleocr-onnx" -ForegroundColor White
Write-Host "2. 'Files and versions' sekmesine git" -ForegroundColor White
Write-Host "3. 'languages/latin/rec.onnx' dosyasini indir" -ForegroundColor White
Write-Host "4. 'paddle-ocr-rec.onnx' olarak yeniden adlandir" -ForegroundColor White
Write-Host "5. Bu klasore kopyala: $ModelsDir" -ForegroundColor White

Write-Host "`nOK Script tamamlandi!" -ForegroundColor Green
Write-Host "Not: PaddleOCR modelini manuel olarak indirmeyi unutma!" -ForegroundColor Yellow
