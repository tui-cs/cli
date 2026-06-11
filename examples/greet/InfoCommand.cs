using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Terminal.Gui.Cli.Greet;

/// <summary>A viewer command that displays application information in a TUI markdown view.</summary>
public sealed class InfoCommand : IViewerCommand
{
    private const string InfoMarkdown = """
                                        # greet — Info

                                        **Version:** 1.0.0

                                        A demonstration of the `Terminal.Gui.Cli` library.

                                        ## Features

                                        This app shows how to:

                                        - Register input and viewer commands
                                        - Use `--help`, `--json`, `--opencli`, and `agent-guide`
                                        - Embed an `agent-guide.md` resource
                                        - Support `--cat` for headless content rendering
                                        - Launch a TUI markdown viewer for `help` and `info`

                                        ## Usage

                                        ```
                                        greet [name]              Greet someone (default: World)
                                        greet --formal [name]     Use a formal greeting style
                                        greet help                Browse help topics
                                        greet help greet          Help for the greet command
                                        greet info                Show this info page
                                        greet --help              Render help as ANSI to stdout
                                        ```
                                        """;

    /// <inheritdoc />
    public string PrimaryAlias => "info";

    /// <inheritdoc />
    public IReadOnlyList<string> Aliases { get; } = ["info"];

    /// <inheritdoc />
    public string Description => "Display application information.";

    /// <inheritdoc />
    public CommandKind Kind => CommandKind.Viewer;

    /// <inheritdoc />
    public Type ResultType => typeof (void);

    /// <inheritdoc />
    public IReadOnlyList<CommandOptionDescriptor> Options { get; } = [];

    /// <inheritdoc />
    public async Task<CommandResult> RunAsync (
        IApplication app,
        string? initial,
        CommandRunOptions options,
        CancellationToken cancellationToken)
    {
        Runnable window = new ()
        {
            Title = "Info",
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        Markdown markdownView = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (1)
        };

        StatusBar statusBar = new (
        [
            new Shortcut (Application.GetDefaultKey (Command.Quit), "Quit", window.RequestStop)
        ]);

        window.Add (markdownView, statusBar);

        window.Initialized += (_, _) => { markdownView.Text = InfoMarkdown; };

        await app.RunAsync (window, cancellationToken);

        return new CommandResult (CommandStatus.Ok, null, null, null);
    }

    /// <inheritdoc />
    public Task<CommandResult?> RenderCatAsync (
        CommandRunOptions options,
        TextWriter stdout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull (stdout);
        MarkdownRenderer.RenderToAnsi (InfoMarkdown, stdout);
        return Task.FromResult<CommandResult?> (new CommandResult (CommandStatus.Ok, null, null, null));
    }
}
