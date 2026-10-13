namespace HangmeIfYouCan;

public static class StatsService
{
    // ── 1 jugador ────────────────────────────────────────────────────

    public static void RecordSinglePlayer(Difficulty difficulty, bool won)
    {
        string dk = difficulty.ToString();
        Preferences.Set($"sp_played_{dk}", GetPlayed(difficulty) + 1);
        if (won)
        {
            Preferences.Set($"sp_won_{dk}", GetWon(difficulty) + 1);
            int streak = Preferences.Get("sp_streak", 0) + 1;
            Preferences.Set("sp_streak", streak);
            if (streak > Preferences.Get("sp_best_streak", 0))
                Preferences.Set("sp_best_streak", streak);
        }
        else
        {
            Preferences.Set("sp_streak", 0);
        }
    }

    public static int GetPlayed(Difficulty d) => Preferences.Get($"sp_played_{d}", 0);
    public static int GetWon(Difficulty d)    => Preferences.Get($"sp_won_{d}",    0);
    public static int GetCurrentStreak()      => Preferences.Get("sp_streak",      0);
    public static int GetBestStreak()         => Preferences.Get("sp_best_streak", 0);

    public static int TotalPlayed() =>
        GetPlayed(Difficulty.Easy) + GetPlayed(Difficulty.Medium) + GetPlayed(Difficulty.Hard);

    public static int TotalWon() =>
        GetWon(Difficulty.Easy) + GetWon(Difficulty.Medium) + GetWon(Difficulty.Hard);

    // ── 2 jugadores ──────────────────────────────────────────────────

    public static void RecordMultiPlayerResult(string winner, string loser)
    {
        AddOrIncrementPlayer(winner, won: true);
        AddOrIncrementPlayer(loser,  won: false);
    }

    public static void RecordMultiPlayerDraw(string p1, string p2)
    {
        AddOrIncrementPlayer(p1, won: false);
        AddOrIncrementPlayer(p2, won: false);
    }

    private static void AddOrIncrementPlayer(string displayName, bool won)
    {
        string key     = MakeKey(displayName);
        var    players = GetPlayerKeys();
        if (!players.Contains(key))
        {
            players.Add(key);
            Preferences.Set("mp_players", string.Join('|', players));
            Preferences.Set($"mp_display_{key}", displayName.Trim());
        }
        Preferences.Set($"mp_games_{key}", Preferences.Get($"mp_games_{key}", 0) + 1);
        if (won)
            Preferences.Set($"mp_wins_{key}", Preferences.Get($"mp_wins_{key}", 0) + 1);
    }

    private static string MakeKey(string name)
    {
        var key = new string(name.Trim().ToLower()
            .Where(c => char.IsLetterOrDigit(c) || c == '_')
            .Take(20).ToArray());
        // Si el nombre es todo símbolos, usar hash numérico como fallback
        return string.IsNullOrEmpty(key)
            ? $"p_{Math.Abs(name.GetHashCode()) % 100000}"
            : key;
    }

    public static List<string> GetPlayerKeys()
    {
        var raw = Preferences.Get("mp_players", "");
        return string.IsNullOrEmpty(raw)
            ? []
            : [.. raw.Split('|').Where(s => !string.IsNullOrEmpty(s))];
    }

    public static string GetPlayerDisplayName(string key) =>
        Preferences.Get($"mp_display_{key}", key);

    public static int GetPlayerGames(string key) => Preferences.Get($"mp_games_{key}", 0);
    public static int GetPlayerWins(string key)  => Preferences.Get($"mp_wins_{key}",  0);

    // ── Reset ─────────────────────────────────────────────────────────

    public static void ResetAll()
    {
        Preferences.Remove("sp_streak");
        Preferences.Remove("sp_best_streak");
        foreach (var d in Enum.GetValues<Difficulty>())
        {
            Preferences.Remove($"sp_played_{d}");
            Preferences.Remove($"sp_won_{d}");
        }
        var players = GetPlayerKeys();
        foreach (var p in players)
        {
            Preferences.Remove($"mp_games_{p}");
            Preferences.Remove($"mp_wins_{p}");
            Preferences.Remove($"mp_display_{p}");
        }
        Preferences.Remove("mp_players");
    }
}
