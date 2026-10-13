namespace HangmeIfYouCan.Resources.Strings;

public static class AppResources
{
    // Home
    public const string ChooseDifficulty = "Elige la dificultad:";
    public const string Easy             = "Fácil";
    public const string Medium           = "Medio";
    public const string Hard             = "Difícil";
    public const string Play             = "▶  Jugar";

    // Game
    public const string HintButton       = "💡 Pista";
    public const string PlayAgain        = "▶  Jugar otra vez";
    public const string HomeButton       = "🏠";
    public const string Won              = "🎉 ¡Ganaste!";
    public const string LostPrefix       = "😢 ¡Perdiste!  Palabra: ";

    // Localized difficulty names
    public static string DifficultyName(Difficulty d) => d switch
    {
        Difficulty.Easy  => Easy,
        Difficulty.Hard  => Hard,
        _                => Medium
    };

    // Language — flag image filenames (add entries when new languages are added)
    public const string CurrentLang      = "es";
    public const string FlagImageES      = "flag_esp.png";
    public const string FlagImageEN      = "flag_uk.png";
    public const string FlagImageCA      = "flag_cat.png";
    public const string CurrentFlagImage = FlagImageES;
}
