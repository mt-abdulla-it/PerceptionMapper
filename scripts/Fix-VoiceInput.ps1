# Perception Mapper - PowerShell voice/microphone setup & test
# Run:  powershell -ExecutionPolicy Bypass -File ".\scripts\Fix-VoiceInput.ps1"
# Or:   Set-ExecutionPolicy -Scope CurrentUser RemoteSigned   (once)
#       .\scripts\Fix-VoiceInput.ps1

$ErrorActionPreference = "Stop"

Write-Host "=== Perception Mapper - Voice fix (PowerShell) ===" -ForegroundColor Cyan
Write-Host ""

# --- 1. Enable microphone privacy (current user) ---
Write-Host "[1/5] Enabling microphone access for apps..." -ForegroundColor Yellow
$micRoot = "HKCU:\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone"
if (-not (Test-Path $micRoot)) {
    New-Item -Path $micRoot -Force | Out-Null
}
Set-ItemProperty -Path $micRoot -Name "Value" -Value "Allow" -Type String -Force

$nonPackaged = Join-Path $micRoot "NonPackaged"
if (-not (Test-Path $nonPackaged)) {
    New-Item -Path $nonPackaged -Force | Out-Null
}
Set-ItemProperty -Path $nonPackaged -Name "Value" -Value "Allow" -Type String -Force
Write-Host "       Microphone privacy set to Allow (current user)." -ForegroundColor Green

# --- 2. System-wide: let apps access microphone (needs Admin) ---
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)

if ($isAdmin) {
    Write-Host "[2/5] Enabling system microphone access (admin)..." -ForegroundColor Yellow
    $lmMic = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone"
    if (-not (Test-Path $lmMic)) {
        New-Item -Path $lmMic -Force | Out-Null
    }
    Set-ItemProperty -Path $lmMic -Name "Value" -Value "Allow" -Type String -Force -ErrorAction SilentlyContinue
    Set-ItemProperty -Path $lmMic -Name "LetAppsAccessMicrophone" -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue
    Write-Host "       System microphone access updated." -ForegroundColor Green
}
else {
    Write-Host "[2/5] Skip system keys (re-run as Administrator for full fix):" -ForegroundColor DarkYellow
    Write-Host "       Start-Process powershell -Verb RunAs -ArgumentList '-ExecutionPolicy Bypass -File `"$PSCommandPath`"'" -ForegroundColor Gray
}

# --- 3. List recording devices ---
Write-Host "[3/5] Recording devices (input):" -ForegroundColor Yellow
try {
    $recorders = Get-CimInstance -ClassName Win32_SoundDevice -ErrorAction SilentlyContinue |
        Where-Object { $_.Status -eq "OK" }
    if ($recorders) {
        $recorders | ForEach-Object { Write-Host "       - $($_.Name)" }
    }
    else {
        Write-Host "       (No devices via WMI - check Sound settings.)" -ForegroundColor DarkYellow
    }
}
catch {
    Write-Host "       Could not list devices: $_" -ForegroundColor DarkYellow
}

# Optional: AudioDeviceCmdlets for default mic
Write-Host ""
Write-Host "       To set default mic via PowerShell, install:" -ForegroundColor Gray
Write-Host "       Install-Module AudioDeviceCmdlets -Scope CurrentUser -Force" -ForegroundColor Gray
Write-Host "       Get-AudioDevice -List | Where-Object Type -eq 'Recording'" -ForegroundColor Gray
Write-Host "       Set-AudioDevice -ID '<index from list>'" -ForegroundColor Gray
Write-Host ""

# --- 4. Check speech recognizers ---
Write-Host "[4/5] Speech recognizers:" -ForegroundColor Yellow
Add-Type -AssemblyName System.Speech
$recognizers = [System.Speech.Recognition.SpeechRecognitionEngine]::InstalledRecognizers()
if ($recognizers.Count -eq 0) {
    Write-Host "       NONE installed. Install in Settings:" -ForegroundColor Red
    Write-Host "       ms-settings:language" -ForegroundColor Gray
    Write-Host "       English (US) -> Language options -> Speech recognition" -ForegroundColor Gray
}
else {
    $recognizers | ForEach-Object { Write-Host "       - $($_.Culture.Name)  ($($_.Description))" -ForegroundColor Green }
}

# --- 5. Live microphone + recognition test ---
Write-Host ""
Write-Host "[5/5] LIVE TEST - speak when you see 'Listening...'" -ForegroundColor Yellow
Write-Host "       (Speak clearly for 5-10 seconds after the countdown)" -ForegroundColor Gray
Write-Host ""

$answer = Read-Host "Run live speech test now? (Y/N)"
if ($answer -match '^[Yy]') {
    try {
        $culture = [System.Globalization.CultureInfo]::new("en-US")
        $engine = New-Object System.Speech.Recognition.SpeechRecognitionEngine($culture)
        $engine.SetInputToDefaultAudioDevice()
        $engine.LoadGrammar((New-Object System.Speech.Recognition.DictationGrammar))
        $engine.InitialSilenceTimeout = [TimeSpan]::FromSeconds(8)
        $engine.EndSilenceTimeout = [TimeSpan]::FromSeconds(1.5)
        $engine.BabbleTimeout = [TimeSpan]::FromSeconds(0)

        Write-Host "Listening in 3..." -ForegroundColor Cyan
        Start-Sleep -Seconds 1
        Write-Host "2..." -ForegroundColor Cyan
        Start-Sleep -Seconds 1
        Write-Host "1... SPEAK NOW" -ForegroundColor Green

        $result = $engine.Recognize([TimeSpan]::FromSeconds(15))
        $engine.Dispose()

        if ($result -and $result.Text) {
            Write-Host ""
            Write-Host "SUCCESS! Heard:" -ForegroundColor Green
            Write-Host "  $($result.Text)" -ForegroundColor White
            Write-Host ""
            Write-Host "Your mic + speech work. Run Perception Mapper Desktop next." -ForegroundColor Green
        }
        else {
            Write-Host ""
            Write-Host "No speech detected." -ForegroundColor Red
            Write-Host "Fix: Right-click speaker icon -> Sounds -> Recording -> set default mic, unmute, level 80+." -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "Test failed: $_" -ForegroundColor Red
        Write-Host "Often: no default microphone or mic muted." -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=== Open Windows settings (optional) ===" -ForegroundColor Cyan
Write-Host "Microphone privacy: ms-settings:privacy-microphone"
Write-Host "Speech setup:       ms-settings:speech"
Write-Host "Sound input:        ms-settings:sound"
Write-Host ""
$open = Read-Host "Open microphone privacy settings now? (Y/N)"
if ($open -match '^[Yy]') {
    Start-Process "ms-settings:privacy-microphone"
}

Write-Host "Done." -ForegroundColor Cyan
