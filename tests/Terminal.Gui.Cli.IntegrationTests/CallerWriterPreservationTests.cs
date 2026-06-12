using System.Text;
using Terminal.Gui.App;
using Xunit;

namespace Terminal.Gui.Cli.IntegrationTests;

/// <summary>
///     Verifies that CliHost.RunAsync keeps writing to caller-supplied writers after a
///     Terminal.Gui session, and only re-acquires writers that are the real console.
/// </summary>
public sealed class CallerWriterPreservationTests
{
    [Fact]
    public async Task RunAsync_CallerSuppliedStreamWriter_ReceivesResultAfterTuiSession ()
    {
        CliHost host = new ();
        host.Registry.Register (new EchoInputCommand ());
        using MemoryStream stdoutStream = new ();
        await using StreamWriter stdout = new (stdoutStream, Encoding.UTF8);
        using StringWriter stderr = new ();

        var exitCode = await host.RunAsync (["echo"], TestContext.Current.CancellationToken, stdout, stderr);
        await stdout.FlushAsync (TestContext.Current.CancellationToken);

        Assert.Equal (ExitCodes.Ok, exitCode);
        var output = Encoding.UTF8.GetString (stdoutStream.ToArray ());
        Assert.Contains ("echoed-result", output);
        Assert.Equal (string.Empty, stderr.ToString ());
    }

    /// <summary>Input command that stops immediately and returns a fixed string result.</summary>
    private sealed class EchoInputCommand : ICliCommand
    {
        public string PrimaryAlias => "echo";
        public IReadOnlyList<string> Aliases { get; } = ["echo"];
        public string Description => "Echo command for testing.";
        public CommandKind Kind => CommandKind.Input;
        public Type ResultType => typeof (string);
        public IReadOnlyList<CommandOptionDescriptor> Options { get; } = [];

        public Task<CommandResult> RunAsync (IApplication app, string? initial, CommandRunOptions options,
            CancellationToken cancellationToken)
        {
            app.RequestStop ();

            return Task.FromResult (new CommandResult (CommandStatus.Ok, "echoed-result", null, null));
        }
    }
}
