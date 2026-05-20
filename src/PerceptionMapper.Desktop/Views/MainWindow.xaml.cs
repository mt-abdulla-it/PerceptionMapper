using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PerceptionMapper.Application.DTOs;
using PerceptionMapper.Application.Enums;
using PerceptionMapper.Application.Services;
using PerceptionMapper.Desktop.Models;
using PerceptionMapper.Desktop.Services;

namespace PerceptionMapper.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly ApiClient _api = new();
    private readonly SpeechService _speech = new();
    private readonly RealtimeVoiceCoachService _liveCoach = new();
    private string _lastSpokenSummary = "";
    private string _lastInputText = "";
    private AnalysisResponse? _lastResult;
    private DateTime? _listenStartedAt;
    private AudioProsodyMetricsDto? _lastVoiceMetrics;
    private string _lastInputMoodLabel = "";
    private string _lastOutputMoodLabel = "";
    private bool _resourcesReleased;

    private static readonly string[] ToneNames = ["Neutral", "Assertive", "Empathetic", "Defensive", "Persuasive"];
    private static readonly string[] LangNames = ["Unknown", "English", "Tamil", "Sinhala"];

    public MainWindow()
    {
        InitializeComponent();
        WelcomeText.Text = Session.Username;

        foreach (PracticeScenario scenario in Enum.GetValues<PracticeScenario>())
        {
            ScenarioCombo.Items.Add(new ComboBoxItem
            {
                Content = ScenarioCoachService.GetDisplayName(scenario),
                Tag = scenario.ToString()
            });
        }
        ScenarioCombo.SelectedIndex = 0;
        ScenarioCombo.SelectionChanged += (_, _) => UpdateScenarioTip();

        InputMoodCombo.Items.Add(new ComboBoxItem { Content = "Not specified", Tag = null });
        for (var i = 0; i < ToneNames.Length; i++)
        {
            InputMoodCombo.Items.Add(new ComboBoxItem
            {
                Content = ToneNames[i],
                Tag = i
            });
        }
        InputMoodCombo.SelectedIndex = 0;

        for (var i = 0; i < ToneNames.Length; i++)
        {
            OutputMoodCombo.Items.Add(new ComboBoxItem
            {
                Content = ToneNames[i],
                Tag = i
            });
        }
        OutputMoodCombo.SelectedIndex = 0;

        foreach (var lang in SpeechService.LanguageOptions)
            VoiceLanguageCombo.Items.Add(lang.DisplayName);
        VoiceLanguageCombo.SelectedIndex = 0;

        _speech.SpeechCaptured += OnSpeechCaptured;
        _speech.ErrorOccurred += (_, msg) =>
        {
            VoiceStatusText.Text = msg;
            SetListeningUi(false);
        };
        _speech.ListeningStopped += (_, _) => SetListeningUi(false);

        _liveCoach.MomentDetected += (_, moment) =>
        {
            LiveMomentsList.Items.Add(moment.Message);
            if (LiveMomentsList.Items.Count > 0)
                LiveMomentsList.ScrollIntoView(LiveMomentsList.Items[^1]);
        };
        _liveCoach.StatusChanged += (_, msg) => VoiceStatusText.Text = msg;

        InputText.TextChanged += (_, _) => UpdateInputChrome();
        InputText.GotFocus += (_, _) => UpdateInputChrome();
        InputText.LostFocus += (_, _) => UpdateInputChrome();

        Loaded += async (_, _) =>
        {
            InputText.Text = "";
            VoiceOnlyModeCheck.IsChecked = true;
            ApplyVoiceOnlyLayout();
            UpdateScenarioTip();
            ShowSpeechSetupStatus();
            VoiceLanguageCombo.SelectionChanged += (_, _) => UpdateLanguagePackHint();
            UpdateLanguagePackHint();
            await CheckApiStatusAsync();
            await LoadHistoryAsync();
            await LoadPracticeSessionsAsync();
            UpdateInputChrome();
        };
    }

    private static readonly Brush InputChromeIdleBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)); // #CBD5E1
    private static readonly Brush InputChromeFocusBrush = new SolidColorBrush(Color.FromRgb(14, 154, 167)); // Accent

    static MainWindow()
    {
        InputChromeIdleBrush.Freeze();
        InputChromeFocusBrush.Freeze();
    }

    private void UpdateInputChrome()
    {
        if (InputWatermark is null || InputTextChrome is null)
            return;

        InputWatermark.Visibility = string.IsNullOrWhiteSpace(InputText.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;

        InputTextChrome.BorderBrush = InputText.IsKeyboardFocused ? InputChromeFocusBrush : InputChromeIdleBrush;
        InputTextChrome.BorderThickness = new Thickness(InputText.IsKeyboardFocused ? 2 : 1);
    }

    private int? SelectedDeclaredInputTone =>
        (InputMoodCombo.SelectedItem as ComboBoxItem)?.Tag is int t ? t : null;

    private int? SelectedRephraseTone =>
        (OutputMoodCombo.SelectedItem as ComboBoxItem)?.Tag is int t ? t : null;

    private string SelectedScenario =>
        (ScenarioCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "General";

    private void UpdateScenarioTip() =>
        ScenarioTipText.Text = ScenarioCoachService.GetScenarioTip(
            ScenarioCoachService.Parse(SelectedScenario));

    private void UpdateLanguagePackHint()
    {
        var culture = SelectedCulture;
        if (MultilingualVoiceHelper.IsRecognizerReady(culture))
        {
            ShowSpeechSetupStatus();
            return;
        }

        VoiceStatusText.Text =
            $"Speech for {FormatCultureDisplay(culture)} is not installed.\n" +
            "Open Mic settings → Windows Language options → download Speech recognition for that language.";
    }

    private void ShowSpeechSetupStatus()
    {
        VoiceStatusText.Text = BuildFriendlySpeechStatus();
        if (!SpeechService.HasRecognizer)
        {
            MessageBox.Show(
                "Windows speech recognition is NOT installed.\n\n" +
                "To fix voice-only mode:\n" +
                "1. Settings → Time & language → Language\n" +
                "2. English (United States) → Language options\n" +
                "3. Download 'Speech recognition'\n" +
                "4. Settings → Privacy → Microphone → Allow desktop apps\n" +
                "5. Restart this app\n\n" +
                "Click 'Mic settings' in the app for shortcuts.",
                "Voice setup required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private string SelectedCulture =>
        SpeechService.LanguageOptions[VoiceLanguageCombo.SelectedIndex].CultureName;

    private void VoiceOnlyStart_Click(object sender, RoutedEventArgs e)
    {
        VoiceOnlyModeCheck.IsChecked = true;
        ApplyVoiceOnlyLayout();
        StartListeningInternal("Speak clearly after the beep — listening up to 12 seconds...");
    }

    private void Listen_Click(object sender, RoutedEventArgs e) =>
        StartListeningInternal("Listening... speak clearly now (up to 12 seconds).");

    private void StartListeningInternal(string statusMessage)
    {
        if (_speech.IsListening) return;

        if (!MultilingualVoiceHelper.IsRecognizerReady(SelectedCulture) && !SpeechService.HasRecognizer)
        {
            ShowSpeechSetupStatus();
            MultilingualVoiceHelper.OpenLanguageSettings();
            return;
        }

        HeardTextLabel.Visibility = Visibility.Collapsed;
        LiveMomentsList.Items.Clear();
        _listenStartedAt = DateTime.UtcNow;
        SetListeningUi(true);
        VoiceStatusText.Text = statusMessage;
        System.Media.SystemSounds.Beep.Play();

        if (LiveCoachCheck.IsChecked == true)
            _liveCoach.Start();

        _speech.StartListening(SelectedCulture);
    }

    private void OnSpeechCaptured(object? sender, SpeechCapturedEventArgs e)
    {
        _liveCoach.Stop();
        SetListeningUi(false);
        InputText.Text = e.Text;
        HeardTextLabel.Visibility = Visibility.Visible;
        HeardTextLabel.Text = "Heard: " + e.Text;

        var voiceMetrics = VoiceProsodyAnalyzer.AnalyzeFile(e.AudioPath, e.Text);
        TryDeleteAudio(e.AudioPath);

        if (voiceMetrics is not null)
        {
            var live = _liveCoach.GetCapturedMoments();
            voiceMetrics.SpeakingMoments = voiceMetrics.SpeakingMoments
                .Concat(live)
                .GroupBy(m => m.Message)
                .Select(g => g.First())
                .OrderBy(m => m.StartSeconds)
                .ToList();
        }

        var duration = _listenStartedAt.HasValue
            ? (DateTime.UtcNow - _listenStartedAt.Value).TotalSeconds
            : 0;
        _listenStartedAt = null;

        if (VoiceOnlyModeCheck.IsChecked == true)
        {
            VoiceStatusText.Text = voiceMetrics is not null
                ? "Analyzing your speaking tone + words..."
                : "Analyzing (words only)...";
            _ = RunAnalysisAsync(e.Text, AutoSpeakCheck.IsChecked == true, voiceMetrics, duration);
        }
        else
        {
            VoiceStatusText.Text = voiceMetrics is not null
                ? "Speech captured (voice tone detected). Click Analyze."
                : "Speech captured. Click Analyze.";
            _lastVoiceMetrics = voiceMetrics;
            _pendingDurationSeconds = duration;
        }
    }

    private double _pendingDurationSeconds;

    private static void TryDeleteAudio(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        try { File.Delete(path); } catch { /* ignore */ }
    }

    private void StopListen_Click(object sender, RoutedEventArgs e)
    {
        _liveCoach.Stop();
        _speech.StopListening();
        SetListeningUi(false);
        _listenStartedAt = null;
        VoiceStatusText.Text = "Stopped. Click Speak now to try again.";
    }

    private async void AnalyzeVoice_Click(object sender, RoutedEventArgs e)
    {
        var text = InputText.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(text))
        {
            MessageBox.Show("Use 'Speak now' or Listen first.", "Voice",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await RunAnalysisAsync(text, AutoSpeakCheck.IsChecked == true, _lastVoiceMetrics, _pendingDurationSeconds);
        _lastVoiceMetrics = null;
        _pendingDurationSeconds = 0;
    }

    private async void Analyze_Click(object sender, RoutedEventArgs e) =>
        await RunAnalysisAsync(InputText.Text ?? "", AutoSpeakCheck.IsChecked == true, null, 0);

    private async Task RunAnalysisAsync(
        string text,
        bool speakResults,
        AudioProsodyMetricsDto? voiceMetrics,
        double durationSeconds)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                VoiceStatusText.Text = "Nothing to analyze. Use the microphone first.";
                return;
            }

            VoiceStatusText.Text = "Analyzing...";
            var scenario = SelectedScenario;
            _lastInputMoodLabel = GetMoodLabel(InputMoodCombo);
            _lastOutputMoodLabel = GetMoodLabel(OutputMoodCombo);
            var rephraseTone = SelectedRephraseTone;
            var declaredInput = SelectedDeclaredInputTone;
            var result = await _api.AnalyzeAsync(
                text, Session.UserId, voiceMetrics, scenario, durationSeconds, rephraseTone, declaredInput);
            if (result is null)
            {
                result = LocalAnalysisHelper.Analyze(text, voiceMetrics, scenario, rephraseTone, declaredInput);
                await CheckApiStatusAsync();

                VoiceStatusText.Text = OfflineBanner.Visibility == Visibility.Visible
                    ? "API offline — results shown locally (practice sessions NOT saved). Start PerceptionMapper.Api first."
                    : $"Server error — practice NOT saved. {_api.LastError ?? "Restart the API project."}";
            }
            else
            {
                OfflineBanner.Visibility = Visibility.Collapsed;
                VoiceStatusText.Text =
                    "Analysis complete — practice session saved. Check score on the right and the Practice sessions tab below.";
            }

            _lastInputText = text;
            _lastResult = result;
            ApplyResultToUi(result);
            ResultsScrollViewer?.ScrollToVerticalOffset(0);
            _lastSpokenSummary = BuildSpeakableSummary(result);

            if (speakResults)
                _speech.Speak(_lastSpokenSummary, SelectedCulture);

            await LoadHistoryAsync();
            await LoadPracticeSessionsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Analysis error:\n{ex.Message}", "Perception Mapper",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string FormatCultureDisplay(string cultureName) => cultureName switch
    {
        "en-US" => "English (United States)",
        "ta-IN" => "Tamil (India)",
        "si-LK" => "Sinhala (Sri Lanka)",
        _ => cultureName
    };

    private string BuildFriendlySpeechStatus()
    {
        var selected = SpeechService.LanguageOptions[VoiceLanguageCombo.SelectedIndex].DisplayName;
        if (!SpeechService.HasRecognizer)
            return "Speech recognition is not installed. Use English (United States) language pack in Windows Settings.";

        var installed = System.Speech.Recognition.SpeechRecognitionEngine.InstalledRecognizers()
            .Select(r => FormatCultureDisplay(r.Culture.Name));
        return $"Speech ready: {string.Join(", ", installed)}\nSelected for practice: {selected}";
    }

    private static string GetMoodLabel(ComboBox cb) =>
        (cb.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

    private void ApplyResultToUi(AnalysisResponse result)
    {
        ResultsPlaceholder.Visibility = Visibility.Collapsed;
        ResultsScrollViewer.Visibility = Visibility.Visible;

        MoodSetupSummaryText.Text =
            $"Input mood (your intent): {_lastInputMoodLabel}. Output mood (rephrase target): {_lastOutputMoodLabel}.";

        ScoreText.Text = $"{result.CommunicationScore}";
        ScoreLabelText.Text = string.IsNullOrWhiteSpace(result.ScoreLabel)
            ? "—"
            : result.ScoreLabel;

        LanguageText.Text = LangNames.ElementAtOrDefault(result.Language) ?? result.Language.ToString();
        var toneName = ToneNames.ElementAtOrDefault(result.DominantTone) ?? result.DominantTone.ToString();
        var confidence = result.ToneConfidencePercent > 0 ? $" ({result.ToneConfidencePercent}%)" : "";
        var source = result.UsedVoiceTone ? " — from how you spoke + words" : "";
        ToneText.Text = toneName + confidence + source;
        if (result.IsToneAmbiguous)
            ToneText.Text += " (mixed)";

        if (result.BiasFlags?.Count > 0)
        {
            BiasText.Text = string.Join("\n", result.BiasFlags.Select(f =>
                $"[{f.Severity}] {f.Category}: \"{f.Phrase}\"\n  {f.Explanation}"));
        }
        else
        {
            BiasText.Text = result.FlaggedBiasPhrases?.Count > 0
                ? string.Join("\n", result.FlaggedBiasPhrases)
                : "None detected";
        }

        FillerText.Text = result.FillerWords?.Count > 0
            ? "Filler words: " + string.Join(", ", result.FillerWords)
            : "";

        FeedbackText.Text = result.FeedbackMessage ?? "";
        SuggestionsText.Text = result.RephrasingSuggestions?.Count > 0
            ? "• " + string.Join("\n• ", result.RephrasingSuggestions)
            : "—";

        TargetRephraseText.Text = result.TargetToneRephrases?.Count > 0
            ? "• " + string.Join("\n• ", result.TargetToneRephrases)
            : "—";

        TimelineItems.Items.Clear();
        if (result.SpeakingMoments?.Count > 0)
        {
            foreach (var m in result.SpeakingMoments)
            {
                TimelineItems.Items.Add(new TextBlock
                {
                    Text = $"[{m.StartSeconds:F1}s] {m.Message}",
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 15,
                    LineHeight = 22,
                    Margin = new Thickness(0, 0, 0, 8)
                });
            }
        }
        else
        {
            TimelineItems.Items.Add(new TextBlock
            {
                Text = "Use voice input for moment feedback (nervous, harsh, hesitation, low confidence).",
                Foreground = System.Windows.Media.Brushes.Gray
            });
        }

        if (!string.IsNullOrWhiteSpace(result.ScenarioTip))
            ScenarioTipText.Text = result.ScenarioTip;
    }

    private static string BuildSpeakableSummary(AnalysisResponse result)
    {
        var tone = ToneNames.ElementAtOrDefault(result.DominantTone) ?? "unknown";
        var lang = LangNames.ElementAtOrDefault(result.Language) ?? "unknown";
        var bias = result.FlaggedBiasPhrases?.Count > 0
            ? string.Join(", ", result.FlaggedBiasPhrases)
            : "none";
        var suggestions = result.RephrasingSuggestions?.Count > 0
            ? string.Join(". ", result.RephrasingSuggestions)
            : "";

        var parts = new List<string>
        {
            $"Communication score: {result.CommunicationScore} out of 100. {result.ScoreLabel}.",
            $"Language detected: {lang}.",
            $"Dominant tone: {tone}.",
            result.FeedbackMessage ?? "",
            $"Bias flags: {bias}."
        };

        if (!string.IsNullOrWhiteSpace(suggestions))
            parts.Add($"Suggestions: {suggestions}");

        return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private void VoiceOnlyMode_Changed(object sender, RoutedEventArgs e) => ApplyVoiceOnlyLayout();

    private void ApplyVoiceOnlyLayout()
    {
        var voiceOnly = VoiceOnlyModeCheck.IsChecked == true;
        TextInputPanel.Visibility = voiceOnly ? Visibility.Collapsed : Visibility.Visible;

        if (WorkspaceSplitGrid.RowDefinitions.Count >= 2)
        {
            if (voiceOnly)
            {
                WorkspaceSplitGrid.RowDefinitions[0].Height = new GridLength(0);
                WorkspaceSplitGrid.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
            }
            else
            {
                WorkspaceSplitGrid.RowDefinitions[0].Height = new GridLength(6, GridUnitType.Star);
                WorkspaceSplitGrid.RowDefinitions[1].Height = new GridLength(5, GridUnitType.Star);
            }
        }

        VoiceOnlyStartButton.Visibility = Visibility.Visible;
        VoiceStatusText.Text = voiceOnly
            ? "Voice-only: click green 'Speak now', wait for beep, then talk."
            : SpeechService.GetDiagnostics();
    }

    private void OpenMicSettings_Click(object sender, RoutedEventArgs e)
    {
        TryOpenSettings("ms-settings:privacy-microphone");
        TryOpenSettings("ms-settings:speech");
    }

    private static void TryOpenSettings(string uri)
    {
        try
        {
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }
        catch
        {
            // ignore
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        InputText.Clear();
        UpdateInputChrome();
        HeardTextLabel.Visibility = Visibility.Collapsed;
        MoodSetupSummaryText.Text = "";
        _lastSpokenSummary = "";
        _lastResult = null;
        ResultsPlaceholder.Visibility = Visibility.Visible;
        ResultsScrollViewer.Visibility = Visibility.Collapsed;
    }

    private void SetListeningUi(bool listening)
    {
        ListenButton.IsEnabled = !listening;
        VoiceOnlyStartButton.IsEnabled = !listening;
        StopListenButton.IsEnabled = listening;
        ListenButton.Content = listening ? "Listening…" : "Listen";
        VoiceOnlyStartButton.Content = listening ? "Listening…" : "Speak now";
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            var history = await _api.GetHistoryAsync(Session.UserId);
            HistoryList.ItemsSource = history?.Select(h => new HistoryRow
            {
                Id = h.Id,
                AnalyzedAt = h.AnalyzedAt,
                DominantTone = h.DominantTone ?? "",
                Preview = Truncate(h.InputText ?? "", 96)
            }).ToList();
        }
        catch
        {
            // optional when API is off
        }
    }

    private async Task LoadPracticeSessionsAsync()
    {
        List<PracticeSessionItem>? sessions = null;
        try
        {
            sessions = await _api.GetPracticeSessionsAsync(Session.UserId);
            PracticeSessionsList.ItemsSource = sessions;
        }
        catch
        {
            // optional when API is off
        }

        UpdateDashboard(sessions);
    }

    private void UpdateDashboard(IReadOnlyList<PracticeSessionItem>? sessions)
    {
        if (sessions is null || sessions.Count == 0)
        {
            DashboardText.Text =
                "No practice sessions saved yet. Steps: (1) Start PerceptionMapper.Api  (2) Login  (3) Click Analyze text or Analyze after voice. " +
                "If you see a yellow API banner, sessions will not save.";
            return;
        }

        var avg = (int)sessions.Average(s => s.CommunicationScore);
        var best = sessions.Max(s => s.CommunicationScore);
        var latest = sessions.OrderByDescending(s => s.CreatedAt).First();
        DashboardText.Text =
            $"Sessions: {sessions.Count}   ·   Average: {avg}/100   ·   Best: {best}/100\n" +
            $"Latest: {latest.CommunicationScore}/100 — {latest.Scenario} — {latest.CreatedAt:g}";
    }

    private async Task CheckApiStatusAsync()
    {
        var online = await _api.PingAsync();
        if (online)
        {
            OfflineBanner.Visibility = Visibility.Collapsed;
            ApiStatusDot.Fill = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(34, 197, 94));
            ApiStatusLabel.Text = "API connected";
            return;
        }

        OfflineBanner.Visibility = Visibility.Visible;
        OfflineBannerText.Text =
            $"API offline at {AppSettings.ApiBaseUrl} — analysis works locally; history and practice sessions are not saved. Start PerceptionMapper.Api first.";
        ApiStatusDot.Fill = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(248, 113, 113));
        ApiStatusLabel.Text = "API offline";
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var about = new AboutWindow { Owner = this };
        about.ShowDialog();
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "Sign out and return to the login screen?",
            "Logout",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
            return;

        ReleaseResources();
        Session.UserId = 0;
        Session.Username = "";

        var login = new LoginWindow();
        login.Show();
        Close();
    }

    private void ReleaseResources()
    {
        if (_resourcesReleased) return;
        _resourcesReleased = true;
        _liveCoach.Dispose();
        _speech.Dispose();
    }

    private async void ExportReport_Click(object sender, RoutedEventArgs e)
    {
        if (_lastResult is null || string.IsNullOrWhiteSpace(_lastInputText))
        {
            MessageBox.Show("Run an analysis first.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var path = await SessionReportExporter.ExportAsync(_lastResult, _lastInputText, Session.Username);
            MessageBox.Show($"Report saved to:\n{path}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed:\n{ex.Message}", "Export", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Window_Closed(object sender, EventArgs e) => ReleaseResources();

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "...";

    private sealed class HistoryRow
    {
        public int Id { get; init; }
        public DateTime AnalyzedAt { get; init; }
        public string DominantTone { get; init; } = "";
        public string Preview { get; init; } = "";
    }
}
