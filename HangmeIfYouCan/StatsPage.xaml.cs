using Microsoft.Maui.Controls.Shapes;

namespace HangmeIfYouCan;

public partial class StatsPage : ContentPage
{
    public StatsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshStats();
        RefreshMuteButton();
    }

    private void RefreshMuteButton()
    {
        StatsMuteButton.Text = AudioService.Instance.IsMuted ? "🔇" : "🔊";
    }

    private void OnMuteClicked(object? sender, EventArgs e)
    {
        AudioService.Instance.ToggleMute();
        RefreshMuteButton();
    }

    private void RefreshStats()
    {
        int totalPlayed = StatsService.TotalPlayed();
        int totalWon    = StatsService.TotalWon();

        // ── 1 Jugador ────────────────────────────────────────────────
        CurrentStreakLabel.Text = StatsService.GetCurrentStreak().ToString();
        BestStreakLabel.Text    = StatsService.GetBestStreak().ToString();

        TotalPlayedLabel.Text = totalPlayed.ToString();
        TotalWonLabel.Text    = totalWon.ToString();
        TotalPctLabel.Text    = totalPlayed > 0
            ? $"{(int)Math.Round(100.0 * totalWon / totalPlayed)}%"
            : "—";

        PopulateDifficulty(Difficulty.Easy,   EasyPlayedLabel,   EasyPctLabel,   EasyBar);
        PopulateDifficulty(Difficulty.Medium, MediumPlayedLabel, MediumPctLabel, MediumBar);
        PopulateDifficulty(Difficulty.Hard,   HardPlayedLabel,   HardPctLabel,   HardBar);

        // ── 2 Jugadores ──────────────────────────────────────────────
        MultiplayerContainer.Children.Clear();
        var playerKeys = StatsService.GetPlayerKeys()
            .OrderByDescending(k => StatsService.GetPlayerWins(k))
            .ThenByDescending(k => StatsService.GetPlayerGames(k))
            .ToList();

        NoMpStatsLabel.IsVisible = playerKeys.Count == 0;

        foreach (var key in playerKeys)
        {
            int    games = StatsService.GetPlayerGames(key);
            int    wins  = StatsService.GetPlayerWins(key);
            string name  = StatsService.GetPlayerDisplayName(key);
            string pct   = games > 0
                ? $"{(int)Math.Round(100.0 * wins / games)}%"
                : "—";

            var nameLabel = new Label
            {
                Text          = name,
                FontSize      = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor     = Color.FromArgb("#1C1917"),
                VerticalOptions = LayoutOptions.Center,
                WidthRequest  = 90
            };
            var winsLabel = new Label
            {
                Text          = $"{wins} victorias",
                FontSize      = 13,
                TextColor     = Color.FromArgb("#EA580C"),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Fill
            };
            var pctLabel = new Label
            {
                Text          = pct,
                FontSize      = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor     = Color.FromArgb("#EA580C"),
                VerticalOptions = LayoutOptions.Center
            };
            var gamesLabel = new Label
            {
                Text          = $"{games} partidas",
                FontSize      = 11,
                TextColor     = Color.FromArgb("#9CA3AF"),
                HorizontalOptions = LayoutOptions.Fill
            };

            var innerGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(90) },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };
            innerGrid.Add(nameLabel,  column: 0, row: 0);
            innerGrid.Add(winsLabel,  column: 1, row: 0);
            innerGrid.Add(pctLabel,   column: 2, row: 0);
            innerGrid.Add(gamesLabel, column: 1, row: 1);
            Grid.SetRowSpan(nameLabel, 2);

            var card = new Border
            {
                BackgroundColor = Color.FromArgb("#FFF7ED"),
                Stroke          = Color.FromArgb("#EA580C"),
                StrokeThickness = 1.5,
                StrokeShape     = new RoundRectangle { CornerRadius = 12 },
                Padding         = new Thickness(14, 10),
                Content         = innerGrid
            };

            MultiplayerContainer.Children.Add(card);
        }

        NoStatsLabel.IsVisible = totalPlayed == 0 && playerKeys.Count == 0;
    }

    private static void PopulateDifficulty(
        Difficulty d, Label playedLbl, Label pctLbl, BoxView bar)
    {
        int played = StatsService.GetPlayed(d);
        int won    = StatsService.GetWon(d);
        playedLbl.Text = $"{played} jugadas";
        pctLbl.Text    = played > 0 ? $"{(int)Math.Round(100.0 * won / played)}%" : "—";
        bar.ScaleX     = played > 0 ? Math.Max(0.05, (double)won / played) : 0.05;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnResetClicked(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlertAsync(
            "Resetear estadísticas",
            "¿Seguro que quieres borrar todas las estadísticas? Esta acción no se puede deshacer.",
            "Sí, borrar", "Cancelar");
        if (confirm)
        {
            StatsService.ResetAll();
            RefreshStats();
        }
    }
}
