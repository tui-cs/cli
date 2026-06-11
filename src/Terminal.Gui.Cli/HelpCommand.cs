using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Command = Terminal.Gui.Input.Command;

namespace Terminal.Gui.Cli;

/// <summary>Interactive TUI markdown help viewer with back/forward navigation and --cat support.</summary>
public sealed class HelpCommand : IViewerCommand
{
    private readonly IHelpProvider _helpProvider;
    private readonly ICommandRegistry _registry;

    /// <summary>Creates a help command that lazily reads command metadata from <paramref name="registry" />.</summary>
    public HelpCommand (ICommandRegistry registry, IHelpProvider helpProvider)
    {
        _registry = registry ?? throw new ArgumentNullException (nameof (registry));
        _helpProvider = helpProvider ?? throw new ArgumentNullException (nameof (helpProvider));
    }

    /// <inheritdoc />
    public string PrimaryAlias => "help";

    /// <inheritdoc />
    public IReadOnlyList<string> Aliases { get; } = ["help"];

    /// <inheritdoc />
    public string Description => "Show command help.";

    /// <inheritdoc />
    public CommandKind Kind => CommandKind.Viewer;

    /// <inheritdoc />
    public Type ResultType => typeof (void);

    /// <inheritdoc />
    public IReadOnlyList<CommandOptionDescriptor> Options { get; } = [];

    /// <inheritdoc />
    public bool AcceptsPositionalArgs => true;

    /// <inheritdoc />
    public async Task<CommandResult> RunAsync (IApplication app, string? initial, CommandRunOptions options,
        CancellationToken cancellationToken)
    {
        var alias = options.Arguments.Count > 0 ? options.Arguments[0] : null;
        var (markdown, title) = BuildHelpContent (alias);

        Runnable window = new ()
        {
            Title = title,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        Markdown markdownView = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (1),
            SyntaxHighlighter = new TextMateSyntaxHighlighter ()
        };

        markdownView.ViewportSettings |= ViewportSettingsFlags.HasHorizontalScrollBar;

        Shortcut statusShortcut = new (Key.Empty, title, null) { MouseHighlightStates = MouseState.None };

        var initialKey = alias ?? "(overview)";
        BrowseBar browseBar = new (initialKey)
        {
            OnNavigate = NavigateTo
        };

        markdownView.LinkClicked += (_, e) =>
        {
            if (e.Url is null)
            {
                return;
            }

            if (e.Url.StartsWith ("help:", StringComparison.OrdinalIgnoreCase))
            {
                var linkTopic = e.Url["help:".Length..];
                var key = linkTopic.Equals ("help", StringComparison.OrdinalIgnoreCase)
                    ? "(overview)"
                    : linkTopic;
                browseBar.Push (key);
                NavigateTo (key);
                e.Handled = true;
            }
        };

        List<Shortcut> statusItems =
        [
            browseBar.Back,
            browseBar.Forward,
            new (Application.GetDefaultKey (Command.Quit), "Quit", window.RequestStop),
            statusShortcut
        ];

        StatusBar statusBar = new (statusItems)
        {
            AlignmentModes = AlignmentModes.IgnoreFirstOrLast
        };
        browseBar.ApplyStyle ();

        window.Add (markdownView, statusBar);

        window.Initialized += (_, _) =>
        {
            markdownView.Text = markdown;

            // The Markdown view may auto-scroll to a focused link after layout.
            // Reset viewport on the second draw to counteract this.
            var drawCount = 0;

            markdownView.DrawComplete += ResetViewport;

            void ResetViewport (object? sender, DrawEventArgs e)
            {
                drawCount++;

                if (drawCount >= 2)
                {
                    markdownView.DrawComplete -= ResetViewport;
                    markdownView.Viewport = markdownView.Viewport with { Y = 0 };
                }
            }
        };

        void NavigateTo (string key)
        {
            var targetAlias = key == "(overview)" ? null : key;
            var (md, t) = BuildHelpContent (targetAlias);
            markdownView.Text = md;
            window.Title = t;
            statusShortcut.Title = t;
        }

        await app.RunAsync (window, cancellationToken);

        return new CommandResult (CommandStatus.Ok, null, null, null);
    }

    /// <inheritdoc />
    public Task<CommandResult?> RenderCatAsync (CommandRunOptions options, TextWriter stdout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull (stdout);
        var alias = options.Arguments.Count > 0 ? options.Arguments[0] : null;
        var (markdown, _) = BuildHelpContent (alias);
        MarkdownRenderer.RenderToAnsi (markdown, stdout);
        return Task.FromResult<CommandResult?> (new CommandResult (CommandStatus.Ok, null, null, null));
    }

    private (string Markdown, string Title) BuildHelpContent (string? alias)
    {
        if (alias is null)
        {
            var rootHelp = _helpProvider.GetRootHelp (_registry) ??
                           new MetadataHelpProvider ().GetRootHelp (_registry) ?? string.Empty;
            return (rootHelp, "Help");
        }

        if (_registry.TryResolve (alias, out ICliCommand? command) && command is not null)
        {
            var commandHelp = _helpProvider.GetCommandHelp (command) ??
                              new MetadataHelpProvider ().GetCommandHelp (command) ?? string.Empty;
            return (commandHelp, $"Help - {command.PrimaryAlias}");
        }

        return ($"# Unknown command: {alias}\n\nTry `help` to see available commands.", "Help");
    }
}
