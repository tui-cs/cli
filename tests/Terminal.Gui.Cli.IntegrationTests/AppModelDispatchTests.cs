using Terminal.Gui.App;
using Xunit;

namespace Terminal.Gui.Cli.IntegrationTests;

/// <summary>
///     Verifies that CliHost.RunWithTerminalGuiAsync sets IApplication.AppModel
///     correctly based on CommandKind and the --fullscreen option.
/// </summary>
public sealed class AppModelDispatchTests
{
    [Fact]
    public async Task InputCommand_WithoutFullscreen_SetsInlineAppModel ()
    {
        AppModel? captured = null;
        SpyInputCommand spy = new (app => captured = app.AppModel);

        CliHost host = new ();
        host.Registry.Register (spy);
        using StringWriter stdout = new ();
        using StringWriter stderr = new ();

        await host.RunAsync (["spy-input"], TestContext.Current.CancellationToken, stdout, stderr);

        Assert.NotNull (captured);
        Assert.Equal (AppModel.Inline, captured);
    }

    [Fact]
    public async Task InputCommand_WithFullscreen_SetsFullScreenAppModel ()
    {
        AppModel? captured = null;
        SpyInputCommand spy = new (app => captured = app.AppModel);

        CliHost host = new ();
        host.Registry.Register (spy);
        using StringWriter stdout = new ();
        using StringWriter stderr = new ();

        await host.RunAsync (["spy-input", "--fullscreen"], TestContext.Current.CancellationToken, stdout, stderr);

        Assert.NotNull (captured);
        Assert.Equal (AppModel.FullScreen, captured);
    }

    [Fact]
    public async Task ViewerCommand_SetsFullScreenAppModel ()
    {
        AppModel? captured = null;
        SpyViewerCommand spy = new (app => captured = app.AppModel);

        CliHost host = new ();
        host.Registry.Register (spy);
        using StringWriter stdout = new ();
        using StringWriter stderr = new ();

        await host.RunAsync (["spy-viewer"], TestContext.Current.CancellationToken, stdout, stderr);

        Assert.NotNull (captured);
        Assert.Equal (AppModel.FullScreen, captured);
    }

    /// <summary>Spy input command that captures IApplication.AppModel then stops immediately.</summary>
    private sealed class SpyInputCommand : ICliCommand
    {
        private readonly Action<IApplication> _onRun;

        public SpyInputCommand (Action<IApplication> onRun)
        {
            _onRun = onRun;
        }

        public string PrimaryAlias => "spy-input";
        public IReadOnlyList<string> Aliases { get; } = ["spy-input"];
        public string Description => "Spy input command for testing.";
        public CommandKind Kind => CommandKind.Input;
        public Type ResultType => typeof (string);
        public IReadOnlyList<CommandOptionDescriptor> Options { get; } = [];

        public Task<CommandResult> RunAsync (IApplication app, string? initial, CommandRunOptions options,
            CancellationToken cancellationToken)
        {
            _onRun (app);
            app.RequestStop ();

            return Task.FromResult (new CommandResult (CommandStatus.Ok, "test", null, null));
        }
    }

    /// <summary>Spy viewer command that captures IApplication.AppModel then stops immediately.</summary>
    private sealed class SpyViewerCommand : IViewerCommand
    {
        private readonly Action<IApplication> _onRun;

        public SpyViewerCommand (Action<IApplication> onRun)
        {
            _onRun = onRun;
        }

        public string PrimaryAlias => "spy-viewer";
        public IReadOnlyList<string> Aliases { get; } = ["spy-viewer"];
        public string Description => "Spy viewer command for testing.";
        public CommandKind Kind => CommandKind.Viewer;
        public Type ResultType => typeof (void);
        public IReadOnlyList<CommandOptionDescriptor> Options { get; } = [];

        public Task<CommandResult> RunAsync (IApplication app, string? initial, CommandRunOptions options,
            CancellationToken cancellationToken)
        {
            _onRun (app);
            app.RequestStop ();

            return Task.FromResult (new CommandResult (CommandStatus.Ok, null, null, null));
        }

        public Task<CommandResult?> RenderCatAsync (CommandRunOptions options, TextWriter stdout,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<CommandResult?> (null);
        }
    }
}
