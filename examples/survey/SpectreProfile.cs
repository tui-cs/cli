using System.Globalization;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Terminal.Gui.Cli.Survey;

/// <summary>
///     Builds the Spectre.Console renderable for a profile and renders it to a text writer.
///     Spectre is the rendering engine; Terminal.Gui (via the host and, in the TUI, the
///     Terminal.Gui.Interop.Spectre bridge) handles presentation and interaction.
/// </summary>
public static class SpectreProfile
{
    /// <summary>Builds the results table renderable describing the profile.</summary>
    /// <param name="answers">The survey answers to render.</param>
    /// <param name="backgroundColor">
    ///     Optional background color for the table borders and padding. When rendering inside
    ///     a Terminal.Gui view (e.g. the confirm step), pass the superview's background so
    ///     the table blends in. Not used for final stdout output.
    /// </param>
    public static IRenderable Build (SurveyAnswers answers, Color? backgroundColor = null)
    {
        ArgumentNullException.ThrowIfNull (answers);

        Table table = new Table ()
            .Border (TableBorder.Rounded)
            .AddColumn (new TableColumn ("[bold]Question[/]"))
            .AddColumn (new TableColumn ("[bold]Answer[/]"));

        if (backgroundColor is not null)
        {
            table.BorderColor (backgroundColor.Value);
        }

        table.AddRow (new Markup ("Name"), new Markup ($"[green]{Markup.Escape (answers.Name)}[/]"));

        var favFruit = answers.FavoriteFruit ?? "none";
        table.AddRow (new Markup ("Favorite fruit"), new Markup (Markup.Escape (favFruit)));
        table.AddRow (new Markup ("Favorite sport"), new Markup (Markup.Escape (answers.Sport)));
        table.AddRow (new Markup ("Age"), new Markup (answers.Age.ToString (CultureInfo.InvariantCulture)));

        var password = answers.Password.Length > 0 ? new string ('*', answers.Password.Length) : "[grey]none[/]";
        table.AddRow (new Markup ("Password"), new Markup (password));

        var color = answers.Color is null ? "[grey]unspecified[/]" : Markup.Escape (answers.Color);
        table.AddRow (new Markup ("Favorite color"), new Markup (color));

        return table;
    }

    /// <summary>Renders the profile to <paramref name="writer" /> as ANSI (or plain text when not a terminal).</summary>
    public static void RenderToAnsi (SurveyAnswers answers, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull (writer);

        IAnsiConsole console = AnsiConsole.Create (new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput (writer),
            Ansi = AnsiSupport.Detect,
            ColorSystem = ColorSystemSupport.Detect,
            Interactive = InteractionSupport.No
        });

        if (console.Profile.Width < 40)
        {
            console.Profile.Width = 100;
        }

        console.Write (Build (answers));
    }
}
