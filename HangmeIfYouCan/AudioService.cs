using Plugin.Maui.Audio;

namespace HangmeIfYouCan;

public sealed class AudioService
{
    public static readonly AudioService Instance = new();

    private IAudioPlayer? _bgPlayer;
    private string?       _currentBgFile;
    private bool          _tensionActive = false;
    private bool          _isMuted;

    public bool IsMuted => _isMuted;

    private AudioService()
    {
        _isMuted = Preferences.Default.Get("sound_muted", false);
    }

    // ── Toggle global ─────────────────────────────────────────────────

    public void ToggleMute()
    {
        _isMuted = !_isMuted;
        Preferences.Default.Set("sound_muted", _isMuted);
        if (_isMuted)
        {
            _bgPlayer?.Stop();
        }
        else if (_currentBgFile is not null)
        {
            _ = StartBgAsync(_currentBgFile, _tensionActive ? 0.30 : 0.25);
        }
    }

    // ── Efectos ───────────────────────────────────────────────────────

    public void PlayLetterOk()      => _ = PlayOnceAsync("letter_ok.wav",           0.8);
    public void PlayLetterKo()      => _ = PlayOnceAsync("letter_ko.wav",            0.8);
    public void PlayHint()          => _ = PlayOnceAsync("hint.wav",                 0.9);
    public void PlayTurnWarning()   => _ = PlayOnceAsync("turn_warning_soft.wav",    0.8);
    public void PlayEndTurnBuzzer() => _ = PlayOnceAsync("end_turn_buzzer.wav",      1.0);
    public void PlayVictory()       => _ = PlayOnceAsync("victory.wav",              1.0);
    public void PlayGameOver()      => _ = PlayOnceAsync("word_not_guessed.wav",     1.0);

    // ── Música de fondo ───────────────────────────────────────────────

    public void StartHomeBackground()
    {
        _tensionActive = false;
        _ = StartBgAsync("main_background_arcade.wav", 0.25);
    }

    public void StartGameBackground()
    {
        _tensionActive = false;
        _ = StartBgAsync("main_background_arcade.wav", 0.25);
    }

    public void SwitchToTension()
    {
        if (_tensionActive) return;
        _tensionActive = true;
        _ = StartBgAsync("background_tension_loop.wav", 0.30);
    }

    public void StopBackground()
    {
        _bgPlayer?.Stop();
        _bgPlayer?.Dispose();
        _bgPlayer      = null;
        _currentBgFile = null;
        _tensionActive = false;
    }

    // ── Privados ──────────────────────────────────────────────────────

    private async Task PlayOnceAsync(string file, double volume)
    {
        if (IsMuted) return;
        try
        {
            var stream = await FileSystem.OpenAppPackageFileAsync(file);
            var player = AudioManager.Current.CreatePlayer(stream);
            player.Volume = volume;
            player.PlaybackEnded += (_, _) =>
            {
                // No liberar dentro del callback: el plugin sigue usando el MediaPlayer
                // (IsPlaying) tras lanzar este evento y crashea con ObjectDisposedException.
                // Se difiere para que el callback nativo termine antes del Dispose.
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500);
                    try { player.Dispose(); } catch { }
                    try { stream.Dispose(); } catch { }
                });
            };
            player.Play();
        }
        catch { }
    }

    private async Task StartBgAsync(string file, double volume)
    {
        StopBackground();
        _currentBgFile = file;

        if (IsMuted) return;
        try
        {
            var stream = await FileSystem.OpenAppPackageFileAsync(file);
            _bgPlayer        = AudioManager.Current.CreatePlayer(stream);
            _bgPlayer.Loop   = true;
            _bgPlayer.Volume = volume;
            _bgPlayer.Play();
        }
        catch { }
    }
}
