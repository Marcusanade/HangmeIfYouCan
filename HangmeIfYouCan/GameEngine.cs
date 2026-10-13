using System.Text.Json;

namespace HangmeIfYouCan;

public enum Difficulty { Easy, Medium, Hard }

public class WordEntry
{
    public string palabra { get; set; } = "";
    public string? pista { get; set; }
    public bool usado { get; set; }
}

public class GameState
{
    public List<char> WordVisible { get; set; } = [];
    public List<char> UsedLetters { get; set; } = [];
    public int Lives { get; set; }
    public bool Won { get; set; }
    public bool Lost { get; set; }
    public string? Hint { get; set; }
    public string? Word { get; set; }
}

public class GameEngine
{
    public const int MaxLives = 6;

    // Umbral de pista igual que el proyecto web: Fácil=4, Medio=3, Difícil=2
    public static int HintThreshold(Difficulty d) => d switch
    {
        Difficulty.Easy => 4,
        Difficulty.Hard => 2,
        _               => 3
    };

    private static readonly Dictionary<Difficulty, List<WordEntry>> _data = [];
    private static readonly Dictionary<Difficulty, string> _resourceNames = new()
    {
        { Difficulty.Easy,   "es.palabras_faciles_es_v1.0.json" },
        { Difficulty.Medium, "es.palabras_medias_es_v1.0.json"  },
        { Difficulty.Hard,   "es.palabras_dificiles_es_v1.0.json" }
    };

    private string _word = "";
    private string? _hint;
    private readonly SortedSet<char> _guessed = [];
    private int _lives = MaxLives;
    private bool _hintRevealed;
    public Difficulty Difficulty { get; private set; }

    static GameEngine()
    {
        foreach (var (level, resource) in _resourceNames)
        {
            using var stream = GetResourceStream(resource);
            if (stream is null) continue;
            var entries = JsonSerializer.Deserialize<List<WordEntry>>(stream) ?? [];
            _data[level] = entries;
        }
    }

    private static Stream? GetResourceStream(string resourceName)
    {
        var assembly = typeof(GameEngine).Assembly;
        var fullName = $"HangmeIfYouCan.Resources.Words.{resourceName}";
        return assembly.GetManifestResourceStream(fullName);
    }

    public static GameEngine StartNew(Difficulty difficulty)
    {
        var words = _data[difficulty];
        var available = words.Where(w => !w.usado).ToList();

        if (available.Count == 0)
        {
            foreach (var w in words) w.usado = false;
            available = words;
        }

        var entry = available[Random.Shared.Next(available.Count)];
        entry.usado = true;

        return new GameEngine
        {
            _word      = Normalize(entry.palabra.ToUpper()),
            _hint      = entry.pista,
            Difficulty = difficulty
        };
    }

    public GameState GuessLetter(char letter)
    {
        letter = char.ToUpper(letter);
        if (!_guessed.Contains(letter))
        {
            _guessed.Add(letter);
            if (!_word.Contains(letter))
                _lives--;
        }
        return GetState();
    }

    public GameState RevealHint()
    {
        if (_hint is null)
            _hint = $"La palabra tiene {_word.Length} letras. Empieza por '{_word[0]}' y acaba por '{_word[^1]}'.";
        _hintRevealed = true;
        return GetState();
    }

    private static string Normalize(string s) => s
        .Replace('Á', 'A').Replace('É', 'E').Replace('Í', 'I')
        .Replace('Ó', 'O').Replace('Ú', 'U').Replace('Ü', 'U');

    public GameState GetState()
    {
        var visible = _word.Select(c => _guessed.Contains(c) ? c : '_').ToList();
        var won  = !visible.Contains('_');
        var lost = _lives <= 0;

        return new GameState
        {
            WordVisible  = visible,
            UsedLetters  = [.. _guessed],
            Lives        = _lives,
            Won          = won,
            Lost         = lost,
            Hint         = _hintRevealed ? _hint : null,
            Word         = (won || lost) ? _word : null
        };
    }
}
