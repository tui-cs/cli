using Terminal.Gui.App;
using Xunit;

namespace Terminal.Gui.Cli.Tests;

public sealed class MetadataHelpProviderTests
{
    [Fact]
    public void GetRootHelp_ProducesMarkdown ()
    {
        MetadataHelpProvider provider = new ();
        CommandRegistry registry = new ();
        registry.Register (new StubCommand ("demo", "A demo command."));

        var result = provider.GetRootHelp (registry);

        Assert.NotNull (result);
        Assert.Contains ("## Commands", result);
        Assert.Contains ("| `demo` | A demo command. |", result);
        Assert.Contains ("## Framework Options", result);
        Assert.Contains ("| `--help`, `-h` | Show help |", result);
    }

    [Fact]
    public void GetCommandHelp_ProducesMarkdown ()
    {
        MetadataHelpProvider provider = new ();
        StubCommand command = new ("test", "Test command.");

        var result = provider.GetCommandHelp (command);

        Assert.NotNull (result);
        Assert.Contains ("# test", result);
        Assert.Contains ("Test command.", result);
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
