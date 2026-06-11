using Terminal.Gui.App;
using Xunit;

namespace Terminal.Gui.Cli.IntegrationTests;

/// <summary>Visual rendering tests for the built-in HelpCommand.</summary>
public sealed class HelpCommandUiTests
{
    [Fact]
    public async Task HelpCommand_InitialRender_ShowsMarkdownContent ()
    {
        CommandRegistry registry = new ();
        MetadataHelpProvider helpProvider = new ();
        HelpCommand help = new (registry, helpProvider);
        registry.Register (help);

        await using CommandUiHarness harness = await CommandUiHarness.StartViewerAsync (help, width: 80, height: 18);

        var text = harness.SnapshotText ();

        // The viewer should display the markdown help content
        Assert.Contains ("Commands", text);
        Assert.Contains ("help", text);
    }

    [Fact]
    public async Task HelpCommand_InitialRender_ContainsFrameworkOptions ()
    {
        CommandRegistry registry = new ();
        MetadataHelpProvider helpProvider = new ();
        HelpCommand help = new (registry, helpProvider);
        registry.Register (help);

        await using CommandUiHarness harness = await CommandUiHarness.StartViewerAsync (help, width: 80, height: 24);

        var text = harness.SnapshotText ();

        Assert.Contains ("Framework Options", text);
        Assert.Contains ("--help", text);
    }

    [Fact]
    public async Task HelpCommand_InitialRender_ProducesAnsiOutput ()
    {
        CommandRegistry registry = new ();
        MetadataHelpProvider helpProvider = new ();
        HelpCommand help = new (registry, helpProvider);
        registry.Register (help);

        await using CommandUiHarness harness = await CommandUiHarness.StartViewerAsync (help, width: 80, height: 18);

        var ansi = harness.SnapshotAnsi ();

        // ANSI output must contain escape sequences for styling
        Assert.Contains ("\x1b[", ansi);
        Assert.False (string.IsNullOrWhiteSpace (ansi));
    }

    [Fact]
    public async Task HelpCommand_WithSubcommand_ShowsCommandHelp ()
    {
        CommandRegistry registry = new ();
        MetadataHelpProvider helpProvider = new ();
        HelpCommand help = new (registry, helpProvider);
        registry.Register (help);
        registry.Register (new StubCommand ("greet", "Greet someone."));

        CommandRunOptions options = new () { Arguments = ["greet"] };

        await using CommandUiHarness harness = await CommandUiHarness.StartViewerAsync (help, options);

        var text = harness.SnapshotText ();

        Assert.Contains ("greet", text);
        Assert.Contains ("Greet someone.", text);
    }

    [Fact]
    public async Task HelpCommand_MatchesAnsiGolden ()
    {
        CommandRegistry registry = new ();
        MetadataHelpProvider helpProvider = new ();
        HelpCommand help = new (registry, helpProvider);
        registry.Register (help);

        await using CommandUiHarness harness = await CommandUiHarness.StartViewerAsync (help, width: 80, height: 18);

        harness.AssertMatchesAnsiGolden ("help.ans");
    }

    private sealed class StubCommand : ICliCommand
    {
        public StubCommand (string alias, string description)
        {
            PrimaryAlias = alias;
            Aliases = [alias];
            Description = description;
        }

        public string PrimaryAlias { get; }

        public IReadOnlyList<string> Aliases { get; }

        public string Description { get; }

        public CommandKind Kind => CommandKind.Input;

        public Type ResultType => typeof (void);

        public IReadOnlyList<CommandOptionDescriptor> Options { get; } = [];

        public Task<CommandResult> RunAsync (IApplication app, string? initial,
            CommandRunOptions options, CancellationToken cancellationToken)
        {
            throw new NotImplementedException ();
        }
    }
}
