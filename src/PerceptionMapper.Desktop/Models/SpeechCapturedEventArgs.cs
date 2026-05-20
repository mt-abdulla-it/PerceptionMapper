namespace PerceptionMapper.Desktop.Models;

public sealed class SpeechCapturedEventArgs : EventArgs
{
    public string Text { get; init; } = "";
    public string? AudioPath { get; init; }
}
