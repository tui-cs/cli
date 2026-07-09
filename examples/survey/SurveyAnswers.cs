namespace Terminal.Gui.Cli.Survey;

/// <summary>The structured result of the survey: a person's profile.</summary>
public sealed record SurveyAnswers (
    string Name,
    IReadOnlyList<string> Fruits,
    string? FavoriteFruit,
    string Sport,
    int Age,
    string Password,
    string? Color)
{
    /// <summary>Renders the profile as a Spectre.Console table (with ANSI color codes) for terminal output.</summary>
    public override string ToString ()
    {
        using StringWriter sw = new ();
        SpectreProfile.RenderToAnsi (this, sw);
        return sw.ToString ().TrimEnd ();
    }
}
