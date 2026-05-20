# ML emotion model (offline ONNX)

Place a compatible speech-emotion ONNX model here:

- `emotion_model.onnx` — input: float waveform `[1, 48000]` @ 16 kHz (3 seconds)
- `emotion_labels.json` — label names (included)

Run:

```powershell
.\scripts\Download-EmotionModel.ps1
```

If the model is missing, the app uses rule-based voice coaching (still works offline).
