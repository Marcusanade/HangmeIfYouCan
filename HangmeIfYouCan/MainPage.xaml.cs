using HangmeIfYouCan.Resources.Strings;
using Microsoft.Maui.Controls.Shapes;

namespace HangmeIfYouCan;

public partial class MainPage : ContentPage
{
    // ── Juego ────────────────────────────────────────────────────────
    private GameEngine? _game;
    private readonly HangmanDrawable _drawable = new();
    private Difficulty _selectedDifficulty = Difficulty.Medium;

    // Teclado: Border + Label (letra) + BoxView (tachado para errores)
    private readonly Dictionary<char, Border>  _keyViews       = [];
    private readonly Dictionary<char, Label>   _keyLabels      = [];
    private readonly Dictionary<char, BoxView> _keyStrikeLines = [];

    // Palabra: Labels pre-creadas para evitar layout shifts al actualizar
    private readonly List<Label> _wordLabels = [];

    private static readonly string[] KeyboardRows =
    [
        "ABCDEFGHI",
        "JKLMNÑOPQ",
        "RSTUVWXYZ"
    ];

    // ── Colores cacheados (evita parsear hex en cada UpdateUI) ────────
    private static readonly Color COrg      = Color.FromArgb("#EA580C");
    private static readonly Color CLGray    = Color.FromArgb("#F9FAFB");
    private static readonly Color CKeyUsed  = Color.FromArgb("#E5E7EB");
    private static readonly Color CInactive = Color.FromArgb("#D1D5DB");
    private static readonly Color CDark     = Color.FromArgb("#1C1917");
    private static readonly Color CRed      = Color.FromArgb("#DC2626");
    private static readonly Color CGreenWin = Color.FromArgb("#22C55E");
    private static readonly Color CGreenOk  = Color.FromArgb("#16A34A");
    private static readonly Color CAmber    = Color.FromArgb("#D97706");
    private static readonly Color CGray     = Color.FromArgb("#9CA3AF");
    private static readonly Color COrgTint  = Color.FromArgb("#FFF7ED");
    private static readonly Color CLostRed  = Color.FromArgb("#EF4444");

    // ── Modo y jugadores ─────────────────────────────────────────────
    private bool _modoMulti = false;
    private readonly string[] _nombres  = ["Jugador 1", "Jugador 2"];
    private readonly int[]    _marcador = [0, 0];
    private int _turnoActual  = 0;
    private int _turnoInicial = 0;

    // ── Timer ────────────────────────────────────────────────────────
    private int _timerSegundos  = 20;
    private int _tiempoRestante = 0;
    private IDispatcherTimer? _timer;
    private bool _procesandoTurno  = false;
    private bool _warningPlayed    = false;

    public MainPage()
    {
        InitializeComponent();
        HangmanView.Drawable = _drawable;
        HintButton.Text      = AppResources.HintButton;
        PlayAgainButton.Text = AppResources.PlayAgain;
        BtnLanguage.Source   = ImageSource.FromFile(AppResources.CurrentFlagImage);
        RefreshModeButtons();
        RefreshDifficultyButtons();
        RefreshMuteButtons();
        BuildKeyboard();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (StartView.IsVisible)
            AudioService.Instance.StartHomeBackground();
        RefreshMuteButtons();
    }

    // ════════════════════════════════════════════════════════════════
    // SELECCIÓN DE MODO
    // ════════════════════════════════════════════════════════════════

    private void On1PlayerClicked(object? sender, EventArgs e)
    {
        _modoMulti = false;
        SetupMulti.IsVisible = false;
        RefreshModeButtons();
    }

    private void On2PlayersClicked(object? sender, EventArgs e)
    {
        _modoMulti = true;
        SetupMulti.IsVisible = true;
        _timerSegundos = DefaultTimerFor(_selectedDifficulty);
        LabelTimerValue.Text = _timerSegundos.ToString();
        RefreshModeButtons();
    }

    private static int DefaultTimerFor(Difficulty d) => d switch
    {
        Difficulty.Easy => 15,
        Difficulty.Hard => 25,
        _               => 20
    };

    private void OnTimerMinus(object? sender, EventArgs e)
    {
        _timerSegundos = Math.Max(5, _timerSegundos - 5);
        LabelTimerValue.Text = _timerSegundos.ToString();
    }

    private void OnTimerPlus(object? sender, EventArgs e)
    {
        _timerSegundos = Math.Min(120, _timerSegundos + 5);
        LabelTimerValue.Text = _timerSegundos.ToString();
    }

    private void RefreshModeButtons()
    {
        // Seleccionado: naranja sólido. No seleccionado: gris muy claro + borde naranja + texto naranja
        Btn1Player.BackgroundColor = !_modoMulti ? COrg : CLGray;
        Btn1Player.TextColor       = !_modoMulti ? Colors.White : COrg;
        Btn1Player.BorderColor     = COrg;
        Btn1Player.FontAttributes  = !_modoMulti ? FontAttributes.Bold : FontAttributes.None;

        Btn2Players.BackgroundColor = _modoMulti ? COrg : CLGray;
        Btn2Players.TextColor       = _modoMulti ? Colors.White : COrg;
        Btn2Players.BorderColor     = COrg;
        Btn2Players.FontAttributes  = _modoMulti ? FontAttributes.Bold : FontAttributes.None;
    }

    // ════════════════════════════════════════════════════════════════
    // SELECCIÓN DE DIFICULTAD
    // ════════════════════════════════════════════════════════════════

    private void OnDifficultyClicked(object? sender, EventArgs e)
    {
        _selectedDifficulty = ((Button)sender!).CommandParameter?.ToString() switch
        {
            "Easy" => Difficulty.Easy,
            "Hard" => Difficulty.Hard,
            _      => Difficulty.Medium
        };
        RefreshDifficultyButtons();
        if (_modoMulti)
        {
            _timerSegundos = DefaultTimerFor(_selectedDifficulty);
            LabelTimerValue.Text = _timerSegundos.ToString();
        }
    }

    private void RefreshDifficultyButtons()
    {
        Apply(BtnEasy,   Difficulty.Easy);
        Apply(BtnMedium, Difficulty.Medium);
        Apply(BtnHard,   Difficulty.Hard);

        void Apply(Button btn, Difficulty level)
        {
            bool sel = _selectedDifficulty == level;
            btn.Text            = AppResources.DifficultyName(level);
            btn.BackgroundColor = sel ? COrg : CLGray;
            btn.TextColor       = sel ? Colors.White : COrg;
            btn.BorderColor     = COrg;
            btn.FontAttributes  = sel ? FontAttributes.Bold : FontAttributes.None;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // NAVEGACIÓN
    // ════════════════════════════════════════════════════════════════

    private void OnStartClicked(object? sender, EventArgs e)
    {
        if (_modoMulti)
        {
            _nombres[0] = string.IsNullOrWhiteSpace(EntryPlayer1.Text) ? "Jugador 1" : EntryPlayer1.Text.Trim();
            _nombres[1] = string.IsNullOrWhiteSpace(EntryPlayer2.Text) ? "Jugador 2" : EntryPlayer2.Text.Trim();
            // _timerSegundos ya está actualizado por el spinner
            _marcador[0]  = 0;
            _marcador[1]  = 0;
            _turnoInicial = 0;
        }
        StartGame();
    }

    private void OnHomeClicked(object? sender, EventArgs e)
    {
        StopTimer();
        AudioService.Instance.StopBackground();
        _game = null;
        GameView.IsVisible  = false;
        StartView.IsVisible = true;
        AudioService.Instance.StartHomeBackground();
    }

    private void OnMuteClicked(object? sender, EventArgs e)
    {
        AudioService.Instance.ToggleMute();
        RefreshMuteButtons();
    }

    private void RefreshMuteButtons()
    {
        string icon = AudioService.Instance.IsMuted ? "🔇" : "🔊";
        HomeMuteButton.Text = icon;
        GameMuteButton.Text = icon;
    }

    private void OnPlayAgainClicked(object? sender, EventArgs e) => StartGame();

    private void OnRematchClicked(object? sender, EventArgs e)
    {
        _turnoInicial = 1 - _turnoInicial;
        StartGame();
    }

    private void OnChangePlayersClicked(object? sender, EventArgs e)
    {
        StopTimer();
        _marcador[0] = 0;
        _marcador[1] = 0;
        _game = null;
        GameView.IsVisible  = false;
        StartView.IsVisible = true;
    }

    private void OnLanguageClicked(object? sender, EventArgs e) =>
        _ = DisplayAlertAsync("Idioma",
            $"Idioma actual: {AppResources.CurrentLang.ToUpper()}\n(Más idiomas próximamente)",
            "OK");

    private async void OnStatsClicked(object? sender, EventArgs e)
    {
        StopTimer();
        await Shell.Current.GoToAsync("statspage");
        // Al volver, si había partida en curso la reanudamos
        if (_modoMulti && _game?.GetState() is { Won: false, Lost: false })
            StartTimer();
    }

    // ════════════════════════════════════════════════════════════════
    // FLUJO DE JUEGO
    // ════════════════════════════════════════════════════════════════

    private void StartGame()
    {
        StopTimer();
        _procesandoTurno = false;
        _game = GameEngine.StartNew(_selectedDifficulty);
        _drawable.WrongGuesses = 0;

        DifficultyLabel.Text = AppResources.DifficultyName(_selectedDifficulty);
        ResultLabel.Text     = "";

        PlayAgainButton.IsVisible     = false;
        RematchButton.IsVisible       = false;
        ChangePlayersButton.IsVisible = false;

        if (_modoMulti)
        {
            _turnoActual = _turnoInicial;
            TurnBanner.IsVisible = true;
            Scoreboard.IsVisible = true;
            UpdateTurnBanner();
            UpdateScoreboard();
            StartTimer();
        }
        else
        {
            TurnBanner.IsVisible = false;
            Scoreboard.IsVisible = false;
        }

        ResetKeyboard();
        var state = _game.GetState();
        InitWordDisplay(state.WordVisible.Count);
        UpdateUI(state);

        StartView.IsVisible = false;
        GameView.IsVisible  = true;
        AudioService.Instance.StartGameBackground();
    }

    private async Task OnKeyTappedAsync(char letter)
    {
        try
        {
            if (_game is null || _procesandoTurno) return;
            if (!_keyViews.TryGetValue(letter, out var borderView) || borderView.InputTransparent) return;

            int livesBefore = _game.GetState().Lives;
            var state = _game.GuessLetter(letter);
            if (state.Lives == livesBefore) AudioService.Instance.PlayLetterOk();
            else                            AudioService.Instance.PlayLetterKo();
            UpdateUI(state);

            if (!state.Won && !state.Lost && _modoMulti)
            {
                bool correct = state.Lives == livesBefore;
                if (correct)
                    ResetTimer();
                else
                {
                    StopTimer();
                    await CambiarTurnoAsync("¡Fallo!");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnKeyTappedAsync error: {ex}");
        }
    }

    private void OnHintClicked(object? sender, EventArgs e)
    {
        if (_game is null) return;
        AudioService.Instance.PlayHint();
        UpdateUI(_game.RevealHint());
    }

    // ════════════════════════════════════════════════════════════════
    // LÓGICA MULTIJUGADOR
    // ════════════════════════════════════════════════════════════════

    private async Task CambiarTurnoAsync(string motivo)
    {
        if (_procesandoTurno) return;
        _procesandoTurno = true;
        _warningPlayed   = false;
        AudioService.Instance.PlayEndTurnBuzzer();

        int siguiente = 1 - _turnoActual;
        TurnLabel.Text = $"{motivo}  Turno de {_nombres[siguiente]}";
        TurnBanner.BackgroundColor = CRed; // rojo durante transición
        TimerLabel.IsVisible = false;

        await Task.Delay(2500);

        var state = _game?.GetState();
        if (state is { Won: false, Lost: false })
        {
            _turnoActual = siguiente;
            _procesandoTurno = false;
            UpdateTurnBanner();
            StartTimer();
        }
        else
        {
            _procesandoTurno = false;
        }
    }

    private void UpdateTurnBanner()
    {
        TurnLabel.Text = $"Turno de {_nombres[_turnoActual]}";
        TurnBanner.BackgroundColor = CGreenOk; // verde al inicio del turno
        TimerLabel.IsVisible = true;
    }

    private void UpdateScoreboard()
    {
        Name1Label.Text  = _nombres[0];
        Name2Label.Text  = _nombres[1];
        Score1Label.Text = _marcador[0].ToString();
        Score2Label.Text = _marcador[1].ToString();

        bool p1Active = _turnoActual == 0;
        ScoreBorder1.Stroke          = p1Active ? COrg     : CInactive;
        ScoreBorder1.BackgroundColor = p1Active ? COrgTint : CLGray;
        Name1Label.TextColor         = p1Active ? COrg     : CGray;
        Score1Label.TextColor        = p1Active ? COrg     : CGray;
        ScoreBorder1.StrokeThickness = p1Active ? 2.5 : 1.5;

        bool p2Active = _turnoActual == 1;
        ScoreBorder2.Stroke          = p2Active ? COrg     : CInactive;
        ScoreBorder2.BackgroundColor = p2Active ? COrgTint : CLGray;
        Name2Label.TextColor         = p2Active ? COrg     : CGray;
        Score2Label.TextColor        = p2Active ? COrg     : CGray;
        ScoreBorder2.StrokeThickness = p2Active ? 2.5 : 1.5;
    }

    // ════════════════════════════════════════════════════════════════
    // TIMER
    // ════════════════════════════════════════════════════════════════

    private void StartTimer()
    {
        StopTimer();
        _tiempoRestante = _timerSegundos;
        _warningPlayed  = false;
        UpdateTimerDisplay();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void ResetTimer()
    {
        _tiempoRestante = _timerSegundos;
        UpdateTimerDisplay();
    }

    private void StopTimer()
    {
        if (_timer is null) return;
        _timer.Tick -= OnTimerTick;
        _timer.Stop();
        _timer = null;
    }

    private async void OnTimerTick(object? sender, EventArgs e)
    {
        _tiempoRestante--;
        UpdateTimerDisplay();

        // Aviso sonoro cuando quedan 5 segundos (una sola vez por turno)
        if (_tiempoRestante == 5 && !_warningPlayed)
        {
            _warningPlayed = true;
            AudioService.Instance.PlayTurnWarning();
        }

        if (_tiempoRestante <= 0)
        {
            StopTimer();
            var state = _game?.GetState();
            if (state is { Won: false, Lost: false })
                await CambiarTurnoAsync("⏰ ¡Tiempo!");
        }
    }

    private void UpdateTimerDisplay()
    {
        TimerLabel.Text = _tiempoRestante.ToString();
        // Texto siempre blanco; el FONDO del banner cambia de color según el tiempo
        if (_procesandoTurno) return;
        double ratio = _timerSegundos > 0 ? (double)_tiempoRestante / _timerSegundos : 0;
        TurnBanner.BackgroundColor = ratio > 0.5
            ? CGreenOk   // verde: tiempo de sobra
            : ratio > 0.2
                ? CAmber // ámbar: aviso
                : CRed; // rojo: urgente
    }

    // ════════════════════════════════════════════════════════════════
    // CONSTRUIR UI
    // ════════════════════════════════════════════════════════════════

    private void ResetKeyboard()
    {
        foreach (var (letter, border) in _keyViews)
        {
            border.BackgroundColor            = COrg;
            border.InputTransparent           = false;
            _keyLabels[letter].TextColor      = Colors.White;
            _keyLabels[letter].FontAttributes = FontAttributes.Bold;
            _keyStrikeLines[letter].IsVisible = false;
        }
    }

    private void BuildKeyboard()
    {
        foreach (var row in KeyboardRows)
        {
            var rowStack = new HorizontalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 4
            };

            foreach (var letter in row)
            {
                var lbl = new Label
                {
                    Text                    = letter.ToString(),
                    FontSize                = 13,
                    FontAttributes          = FontAttributes.Bold,
                    TextColor               = Colors.White,
                    HorizontalOptions       = LayoutOptions.Center,
                    VerticalOptions         = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.Center
                };

                // Línea de tachado con margen para no llegar a los bordes
                var strike = new BoxView
                {
                    HeightRequest     = 2,
                    Color             = COrg,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions   = LayoutOptions.Center,
                    Margin            = new Thickness(6, 0),
                    IsVisible         = false
                };

                var inner = new Grid();
                inner.Add(lbl);
                inner.Add(strike);

                var border = new Border
                {
                    WidthRequest    = 34,
                    HeightRequest   = 40,
                    BackgroundColor = COrg,
                    StrokeShape     = new RoundRectangle { CornerRadius = 6 },
                    Stroke          = Colors.Transparent,
                    Content         = inner
                };

                char cap = letter;
                var tap = new TapGestureRecognizer();
                tap.Tapped += async (s, e) => await OnKeyTappedAsync(cap);
                border.GestureRecognizers.Add(tap);

                _keyViews[letter]       = border;
                _keyLabels[letter]      = lbl;
                _keyStrikeLines[letter] = strike;
                rowStack.Add(border);
            }

            KeyboardLayout.Children.Add(rowStack);
        }
    }

    private void InitWordDisplay(int wordLength)
    {
        WordDisplay.Children.Clear();
        _wordLabels.Clear();

        for (int i = 0; i < wordLength; i++)
        {
            var lbl = new Label
            {
                Text                    = "",
                TextColor               = CDark,
                FontSize                = 16,
                FontAttributes          = FontAttributes.Bold,
                HorizontalOptions       = LayoutOptions.Center,
                VerticalOptions         = LayoutOptions.End,
                HorizontalTextAlignment = TextAlignment.Center,
                WidthRequest            = 26
            };

            var cell = new Grid
            {
                WidthRequest  = 26,
                HeightRequest = 38,
                Margin        = new Thickness(3, 2)
            };
            cell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            cell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3) });

            cell.Add(lbl, column: 0, row: 0);
            cell.Add(new BoxView
            {
                Color           = COrg,
                HeightRequest   = 3,
                VerticalOptions = LayoutOptions.End
            }, column: 0, row: 1);

            _wordLabels.Add(lbl);
            WordDisplay.Children.Add(cell);
        }
    }

    private void UpdateWordDisplay(List<char> visible)
    {
        for (int i = 0; i < _wordLabels.Count && i < visible.Count; i++)
            _wordLabels[i].Text = visible[i] == '_' ? "" : visible[i].ToString();
    }

    private void UpdateUI(GameState state)
    {
        _drawable.WrongGuesses = GameEngine.MaxLives - state.Lives;
        HangmanView.Invalidate();

        // Vidas: 50% más grandes (FontSize 27)
        var fs = new FormattedString();
        for (int i = 0; i < state.Lives; i++)
            fs.Spans.Add(new Span { Text = "♥", TextColor = CRed, FontSize = 27 });
        for (int i = state.Lives; i < GameEngine.MaxLives; i++)
            fs.Spans.Add(new Span { Text = "♡", TextColor = CInactive, FontSize = 27 });
        LivesLabel.FormattedText = fs;

        // Palabra: solo actualiza texto, sin reconstruir celdas → sin layout shift
        UpdateWordDisplay(state.WordVisible);

        if (_modoMulti) UpdateScoreboard();

        // Teclado: usadas → gris + naranja; errores → tachado
        foreach (var (letter, border) in _keyViews)
        {
            if (!state.UsedLetters.Contains(letter)) continue;
            bool correct = state.WordVisible.Contains(letter);
            border.BackgroundColor            = CKeyUsed;
            border.InputTransparent           = true;
            _keyLabels[letter].TextColor      = COrg;
            _keyLabels[letter].FontAttributes = FontAttributes.Bold;
            if (!correct)
                _keyStrikeLines[letter].IsVisible = true; // tachado solo para errores
        }

        // Pista: Opacity en lugar de IsVisible → ocupa espacio siempre
        bool canHint = !state.Won && !state.Lost
                       && state.Hint is null
                       && state.Lives <= GameEngine.HintThreshold(_selectedDifficulty);
        HintButton.Opacity          = canHint ? 1 : 0;
        HintButton.InputTransparent = !canHint;
        HintLabel.Opacity           = state.Hint is not null ? 1 : 0;
        HintLabel.Text              = state.Hint ?? "";

        // Música de tensión al quedar pocas vidas
        if (state.Lives <= 3 && !state.Won && !state.Lost)
            AudioService.Instance.SwitchToTension();

        // Fin de partida
        if (state.Won)
        {
            StopTimer();
            AudioService.Instance.StopBackground();
            AudioService.Instance.PlayVictory();
            if (_modoMulti)
            {
                _marcador[_turnoActual]++;
                UpdateScoreboard();
                StatsService.RecordMultiPlayerResult(
                    _nombres[_turnoActual], _nombres[1 - _turnoActual]);
                ResultLabel.Text              = $"🎉 ¡{_nombres[_turnoActual]} gana!";
                RematchButton.IsVisible       = true;
                ChangePlayersButton.IsVisible = true;
            }
            else
            {
                StatsService.RecordSinglePlayer(_selectedDifficulty, true);
                ResultLabel.Text          = AppResources.Won;
                PlayAgainButton.IsVisible = true;
            }
            ResultLabel.TextColor = CGreenWin;
            DisableKeyboard();
        }
        else if (state.Lost)
        {
            StopTimer();
            AudioService.Instance.StopBackground();
            AudioService.Instance.PlayGameOver();
            if (_modoMulti)
            {
                StatsService.RecordMultiPlayerDraw(_nombres[0], _nombres[1]);
                ResultLabel.Text              = $"😢 ¡Habéis perdido!  Palabra: {state.Word}";
                RematchButton.IsVisible       = true;
                ChangePlayersButton.IsVisible = true;
            }
            else
            {
                StatsService.RecordSinglePlayer(_selectedDifficulty, false);
                ResultLabel.Text          = AppResources.LostPrefix + state.Word;
                PlayAgainButton.IsVisible = true;
            }
            ResultLabel.TextColor = CLostRed;
            DisableKeyboard();
        }
    }

    private void DisableKeyboard()
    {
        foreach (var border in _keyViews.Values)
            border.InputTransparent = true;
        HintButton.Opacity          = 0;
        HintButton.InputTransparent = true;
    }
}
