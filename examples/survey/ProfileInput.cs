using System.Globalization;

namespace Terminal.Gui.Cli.Survey;

/// <summary>Shared option descriptors and headless parsing for the survey command.</summary>
public static class ProfileInput
{
    /// <summary>Per-command options accepted by the survey command.</summary>
    public static IReadOnlyList<CommandOptionDescriptor> Options { get; } =
    [
        new ("name", "n", typeof (string), "The person's name.", false, null),
        new ("fruits", "f", typeof (string), "Comma-separated list of favorite fruits.", false, null),
        new ("sport", "s", typeof (string), "Favorite sport.", false, null),
        new ("age", "a", typeof (int), "Age in years (1-120).", false, null),
        new ("password", "p", typeof (string), "Password (secret).", false, null),
        new ("color", "c", typeof (string), "Favorite color (optional).", false, null),
        new ("confirm", null, typeof (bool), "Show a confirmation step before finishing.", false, null)
    ];

    /// <summary>A sample profile used when invoked without options in headless mode.</summary>
    public static SurveyAnswers Sample { get; } =
        new ("Ada Lovelace", ["Apple", "Cherry"], "Apple", "Fencing", 36, "Passw0rd!", "Teal");

    /// <summary>
    ///     Builds a <see cref="SurveyAnswers" /> from command-line options. Returns false with an
    ///     <paramref name="error" /> when a provided value is invalid. A missing name is not an error;
    ///     callers inspect <see cref="SurveyAnswers.Name" /> to decide whether to prompt interactively.
    /// </summary>
    public static bool TryBuild (
        CommandRunOptions options,
        string? initial,
        out SurveyAnswers answers,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull (options);
        error = null;
        answers = null!;

        var name = options.CommandOptions.TryGetValue ("name", out var nameValue) &&
                   !string.IsNullOrWhiteSpace (nameValue)
            ? nameValue
            : options.Arguments.Count > 0
                ? string.Join (" ", options.Arguments)
                : initial ?? string.Empty;

        var fruits =
            options.CommandOptions.TryGetValue ("fruits", out var fruitsValue) &&
            !string.IsNullOrWhiteSpace (fruitsValue)
                ? fruitsValue.Split (',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                : [];

        var sport = options.CommandOptions.TryGetValue ("sport", out var sportValue) &&
                    !string.IsNullOrWhiteSpace (sportValue)
            ? sportValue
            : "Unspecified";

        var age = 0;

        if (options.CommandOptions.TryGetValue ("age", out var ageText))
        {
            if (!int.TryParse (ageText, NumberStyles.None, CultureInfo.InvariantCulture, out age) || age < 1 ||
                age > 120)
            {
                error = $"Invalid age '{ageText}'. Provide a whole number between 1 and 120.";
                return false;
            }
        }

        var password = options.CommandOptions.TryGetValue ("password", out var passwordValue) &&
                       !string.IsNullOrWhiteSpace (passwordValue)
            ? passwordValue
            : string.Empty;

        var color = options.CommandOptions.TryGetValue ("color", out var colorValue) &&
                    !string.IsNullOrWhiteSpace (colorValue)
            ? colorValue
            : null;

        // Determine favorite fruit: first fruit if only one, otherwise null (to be picked interactively)
        var favoriteFruit = fruits.Length == 1 ? fruits[0] : null;

        answers = new SurveyAnswers (name, fruits, favoriteFruit, sport, age, password, color);
        return true;
    }
}
