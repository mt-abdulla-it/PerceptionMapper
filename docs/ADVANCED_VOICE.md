# Advanced voice features

## 1. Real-time streaming analysis

- Enable **Live coach (real-time)** in the app.
- Click **Speak now** / **Listen**.
- While you talk, the **Live streaming feedback** list updates every ~2 seconds.
- Detects: nervous, harsh, hesitation, low confidence (rules + optional ML).

## 2. ML emotion model (ONNX, offline)

1. Run: `.\scripts\Download-EmotionModel.ps1`
2. Place compatible model: `models\emotion_model.onnx`
   - Input: float waveform, 48000 samples @ 16 kHz (3 sec)
   - Output: emotion logits (see `emotion_labels.json`)
3. Rebuild and run Desktop — status shows **ML emotion + rules**.

Without the file, coaching uses **rule-based prosody** only.

## 3. Tamil / Sinhala voice packs

### Speech-to-text
1. Settings → Time & language → Language
2. Add **Tamil (Sri Lanka)** and/or **Sinhala**
3. Language options → download **Speech recognition**
4. In app, choose **Speak in: Tamil** or **Sinhala**

### Text-to-speech
Download **Speech** / voice pack for the same language.

### Verify (PowerShell)
```powershell
Add-Type -AssemblyName System.Speech
[System.Speech.Recognition.SpeechRecognitionEngine]::InstalledRecognizers() | % { $_.Culture.Name }
```

You should see `ta-IN` and/or `si-LK` when packs are installed.
