# Download placeholder / setup for ONNX speech emotion model
$modelsDir = Join-Path (Split-Path $PSScriptRoot -Parent) "models"
New-Item -ItemType Directory -Force -Path $modelsDir | Out-Null

$labels = Join-Path $modelsDir "emotion_labels.json"
if (-not (Test-Path $labels)) {
    Write-Host "emotion_labels.json missing — copy from repo models folder." -ForegroundColor Yellow
}

$modelPath = Join-Path $modelsDir "emotion_model.onnx"
if (Test-Path $modelPath) {
    Write-Host "emotion_model.onnx already exists." -ForegroundColor Green
    exit 0
}

Write-Host @"
ONNX emotion model not bundled (size).

For your viva / demo you can:
1. Export a speech-emotion ONNX model (input: 48000 float samples @ 16kHz)
   e.g. from Hugging Face: speech emotion recognition -> ONNX
2. Save as: $modelPath
3. Restart Perception Mapper Desktop

Until then, LIVE coaching uses rule-based prosody (still real-time).

Tamil/Sinhala:
  Settings -> Language -> Tamil (Sri Lanka) / Sinhala -> Speech recognition + Speech
"@ -ForegroundColor Cyan
