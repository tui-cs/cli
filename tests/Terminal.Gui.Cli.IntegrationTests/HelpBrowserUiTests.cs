using System.Reflection;
using Terminal.Gui.App;
using Xunit;

namespace Terminal.Gui.Cli.IntegrationTests;

/// <summary>Tests that prove the help browser renders correctly with BrowseBar and navigation.</summary>
public sealed class HelpBrowserUiTests
{
    [Fact]
    public async Task HelpBrowser_ShowsBackForwardButtons ()
    {
        CommandRegistry registry = new ();
        MetadataHelpProvider helpProvider = new ();
        HelpCommand help = new (registry, helpProvider);
        registry.Register (help);

        await using CommandUiHarness harness = await CommandUiHarness.StartViewerAsync (help, width: 80, height: 18);

        var text = harness.SnapshotText ();

        // BrowseBar should render back/forward arrows in the status bar
        Assert.Contains ("\u25c4", text); // ◄ left arrow
        Assert.Contains ("\u25ba", text); // ► right arrow
        Assert.Contains ("Quit", text);
    }

    [Fact]
    public async Task HelpBrowser_WithEmbeddedHelp_ShowsRootContent ()
    {
        Assembly assembly = typeof (HelpBrowserUiTests).Assembly;
        EmbeddedMarkdownHelpProvider provider = new (assembly);
        CommandRegistry registry = new ();
        HelpCommand help = new (registry, provider);
        registry.Register (help);
        registry.Register (new StubCommand ("greet", "Prompt for a name and return a greeting."));
        registry.Register (new StubCommand ("farewell", "Say goodbye to someone."));

        await using CommandUiHarness harness = await CommandUiHarness.StartViewerAsync (help, width: 100, height: 24);

        var text = harness.SnapshotText ();

        Assert.Contains ("greet", text);
        Assert.Contains ("farewell", text);
    }

    [Fact]
    public async Task HelpBrowser_SubcommandView_ShowsCommandHelp ()
    {
        CommandRegistry registry = new ();
        MetadataHelpProvider helpProvider = new ();
        HelpCommand help = new (registry, helpProvider);
        StubCommand greet = new ("greet", "Greet someone.")
        {
            CommandOptions =
            [
                new CommandOptionDescriptor ("formal", "f", typeof (bool), "Use a formal greeting style.", false, null)
            ]
        };
        registry.Register (help);
        registry.Register (greet);

        CommandRunOptions options = new () { Arguments = ["greet"] };

        await using CommandUiHarness harness = await CommandUiHarness.StartViewerAsync (help, options, 80,
            20);

        var text = harness.SnapshotText ();

        Assert.Contains ("greet", text);
        Assert.Contains ("Greet someone.", text);
        Assert.Contains ("--formal", text);
    }

    [Fact]
    public async Task HelpBrowser_MatchesGolden ()
    {
        CommandRegistry registry = new ();
        MetadataHelpProvider helpProvider = new ();
        HelpCommand help = new (registry, helpProvider);
        registry.Register (help);
        registry.Register (new StubCommand ("greet", "Greet someone."));
        registry.Register (new StubCommand ("farewell", "Say goodbye."));

        await using CommandUiHarness harness = await CommandUiHarness.StartViewerAsync (help, width: 80, height: 20);

        harness.AssertMatchesAnsiGolden ("help-browser.ans");
    }

    private sealed class StubCommand : ICliCommand
    {
        public StubCommand (string alias, string description)
        {
            PrimaryAlias = alias;
            Aliases = [alias];
            Description = description;
        }

        public IReadOnlyList<CommandOptionDescriptor> CommandOptions
        {
            get => Options;
            init => Options = value;
        }

        public string PrimaryAlias { get; }
        public IReadOnlyList<string> Aliases { get; }
        public string Description { get; }
        public CommandKind Kind => CommandKind.Input;
        public Type ResultType => typeof (void);
        public IReadOnlyList<CommandOptionDescriptor> Options { get; init; } = [];

        public Task<CommandResult> RunAsync (IApplication app, string? initial,
            CommandRunOptions options, CancellationToken cancellationToken)
        {
            throw new NotImplementedException ();
        }
    }
}
