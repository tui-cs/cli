using System.Reflection;
using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;

namespace Terminal.Gui.Cli;

/// <summary>The main entry point. Owns parsing, dispatch, Terminal.Gui lifecycle, and output.</summary>
public sealed class CliHost
{
    private readonly IHelpProvider _helpProvider;
    private readonly CliHostOptions _options;
    private readonly ArgParser _parser;

    /// <summary>Creates a host, applies configuration, creates its registry, and registers built-ins.</summary>
    public CliHost (Action<CliHostOptions>? configure = null)
    {
        _options = new CliHostOptions ();
        configure?.Invoke (_options);
        _helpProvider = _options.HelpProvider ?? new MetadataHelpProvider ();
        Registry = new CommandRegistry ();
        RegisterBuiltIns ();
        _parser = new ArgParser (_options.GlobalOptions, _options.MaxInitialChars);
    }

    /// <summary>The command registry owned by this host. Register consumer commands before RunAsync.</summary>
    public ICommandRegistry Registry { get; }

    /// <summary>Parses args, dispatches a command, writes output, and returns a process exit code.</summary>
    public async Task<int> RunAsync (
        string[] args,
        CancellationToken cancellationToken = default,
        TextWriter? stdout = null,
        TextWriter? stderr = null)
    {
        stdout ??= Console.Out;
        stderr ??= Console.Error;

        ArgParser.ParseResult initialParse = _parser.Parse (args);

        if (!initialParse.Success)
        {
            if (_options.DefaultCommand is not null)
            {
                return await RunWithDefaultCommandAsync (args, cancellationToken, stdout, stderr);
            }

            stderr.WriteLine (initialParse.Error);
            return ExitCodes.UsageError;
        }

        if (initialParse.RootFlag is { } rootFlag)
        {
            // When a DefaultCommand is set and args are empty (which maps to Help),
            // run the default command instead of showing help.
            if (rootFlag == ArgParser.RootFlag.Help && args.Length == 0 && _options.DefaultCommand is not null)
            {
                return await RunWithDefaultCommandAsync (args, cancellationToken, stdout, stderr);
            }

            WriteRootFlag (rootFlag, stdout);
            return ExitCodes.Ok;
        }

        if (initialParse.Alias is null || !Registry.TryResolve (initialParse.Alias, out ICliCommand? command) ||
            command is null)
        {
            if (_options.DefaultCommand is not null)
            {
                return await RunWithDefaultCommandAsync (args, cancellationToken, stdout, stderr);
            }

            stderr.WriteLine ($"Unknown command '{initialParse.Alias}'.");
            return ExitCodes.UsageError;
        }

        return await DispatchCommandAsync (args, command, cancellationToken, stdout, stderr);
    }

    private async Task<int> RunWithDefaultCommandAsync (
        string[] args,
        CancellationToken cancellationToken,
        TextWriter stdout,
        TextWriter stderr)
    {
        if (!Registry.TryResolve (_options.DefaultCommand!, out ICliCommand? defaultCmd) || defaultCmd is null)
        {
            stderr.WriteLine ($"Default command '{_options.DefaultCommand}' is not registered.");
            return ExitCodes.UsageError;
        }

        string[] adjusted = [_options.DefaultCommand!, .. args];

        // The fallback fires precisely because the original tokens didn't resolve to a known
        // command/options, so unknown dash-prefixed tokens must pass through verbatim as
        // positional arguments rather than failing the reparse (issue #30).
        ArgParser.ParseResult parse = _parser.Parse (adjusted, defaultCmd, true);

        if (!parse.Success || parse.Options is null)
        {
            stderr.WriteLine (parse.Error);
            return ExitCodes.UsageError;
        }

        return await ExecuteCommandAsync (defaultCmd, parse.Options, cancellationToken, stdout, stderr);
    }

    private async Task<int> DispatchCommandAsync (
        string[] args,
        ICliCommand command,
        CancellationToken cancellationToken,
        TextWriter stdout,
        TextWriter stderr)
    {
        ArgParser.ParseResult parse = _parser.Parse (args, command);

        if (!parse.Success || parse.Options is null)
        {
            stderr.WriteLine (parse.Error);
            return ExitCodes.UsageError;
        }

        return await ExecuteCommandAsync (command, parse.Options, cancellationToken, stdout, stderr);
    }

    private async Task<int> ExecuteCommandAsync (
        ICliCommand command,
        CommandRunOptions runOptions,
        CancellationToken cancellationToken,
        TextWriter stdout,
        TextWriter stderr)
    {
        // Capture whether each writer is the real console before Terminal.Gui runs. Changing
        // Console.OutputEncoding replaces Console.Out/Error, so this comparison is only valid now.
        var stdoutIsConsole = ReferenceEquals (stdout, Console.Out);
        var stderrIsConsole = ReferenceEquals (stderr, Console.Error);

        if (runOptions.Initial is not null && !command.TryValidateInitial (runOptions.Initial, runOptions))
        {
            stderr.WriteLine ("Invalid --initial value.");
            return ExitCodes.ValidationError;
        }

        using CancellationTokenSource? timeoutSource = runOptions.Timeout is null
            ? null
            : CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);

        if (timeoutSource is not null && runOptions.Timeout is { } timeout)
        {
            timeoutSource.CancelAfter (timeout);
        }

        CancellationToken effectiveToken = timeoutSource?.Token ?? cancellationToken;

        if (command is IViewerCommand viewer && runOptions.Cat)
        {
            CommandResult? catResult;

            try
            {
                catResult = await viewer.RenderCatAsync (runOptions, stdout, effectiveToken);
            }
            catch (OperationCanceledException)
            {
                return ExitCodes.Cancelled;
            }

            if (catResult is { } cat)
            {
                // RenderCatAsync writes its own rendered output for successful results. For
                // non-success results it produced no output, so surface the diagnostic (to stderr
                // in plain text, or the error envelope under --json) instead of exiting silently.
                if (cat.Status is not (CommandStatus.Ok or CommandStatus.NoResult))
                {
                    ResultWriter.Write (cat, runOptions.JsonOutput, stdout, stderr, runOptions.OutputPath,
                        _options.ResultJsonResolver);
                }

                return ExitCodes.FromResult (cat);
            }
        }

        CommandResult result;

        try
        {
            result = await RunWithTerminalGuiAsync (command, runOptions, effectiveToken);
        }
        catch (OperationCanceledException)
        {
            result = CreateCancelledResult ();
        }

        // Terminal.Gui may change Console.OutputEncoding during its session (e.g. to UTF-8 for
        // rendering). After shutdown, the encoding might be restored to OEM or left as UTF-8.
        // Either way, console writer references captured before TG ran are now stale
        // (Console.Out is replaced whenever OutputEncoding changes). Ensure UTF-8 and re-acquire
        // the current Console.Out/Error so Unicode content (box-drawing, etc.) renders correctly.
        // Caller-supplied writers are left untouched — only real console writers go stale.
        if (stdoutIsConsole || stderrIsConsole)
        {
            if (Console.OutputEncoding.CodePage != Encoding.UTF8.CodePage)
            {
                Console.OutputEncoding = Encoding.UTF8;
            }

            if (stdoutIsConsole)
            {
                stdout = Console.Out;
            }

            if (stderrIsConsole)
            {
                stderr = Console.Error;
            }
        }

        if (!ResultWriter.Write (result, runOptions.JsonOutput, stdout, stderr, runOptions.OutputPath,
                _options.ResultJsonResolver))
        {
            return ExitCodes.UsageError;
        }

        return ExitCodes.FromResult (result);
    }

    private static CommandResult CreateCancelledResult ()
    {
        return new CommandResult (
            CommandStatus.Cancelled,
            null,
            null,
            null);
    }

    /// <summary>
    ///     Creates, initializes, and disposes a headless ANSI-driver Terminal.Gui application around
    ///     <paramref name="render" />. Centralizes the Terminal.Gui lifecycle here (constitution C1) so
    ///     helpers such as <see cref="MarkdownRenderer" /> never call lifecycle entrypoints directly.
    /// </summary>
    internal static void RunHeadlessRender (int width, int height, Action<IApplication> render)
    {
        var previousDriverIO = Environment.GetEnvironmentVariable ("DisableRealDriverIO");
        Environment.SetEnvironmentVariable ("DisableRealDriverIO", "1");
        IApplication app = Application.Create ();

        try
        {
            app.Init (DriverRegistry.Names.ANSI);
            app.Driver?.SetScreenSize (width, height);
            render (app);
        }
        finally
        {
            app.Dispose ();
            Environment.SetEnvironmentVariable ("DisableRealDriverIO", previousDriverIO);
        }
    }

    private async Task<CommandResult> RunWithTerminalGuiAsync (ICliCommand command, CommandRunOptions runOptions,
        CancellationToken cancellationToken)
    {
        var useInline = command.Kind == CommandKind.Input && !runOptions.Fullscreen;

        // Application.AppModel is process-wide state; restore it after dispatch so later
        // Terminal.Gui sessions in the same process (embedding, headless rendering) do not
        // inherit this command's app model.
        AppModel previousAppModel = Application.AppModel;
        Application.AppModel = useInline ? AppModel.Inline : AppModel.FullScreen;

        try
        {
            using IApplication app = Application.Create ();
            app.Init ();

            return await command.RunAsync (app, runOptions.Initial, runOptions, cancellationToken);
        }
        finally
        {
            Application.AppModel = previousAppModel;
        }
    }

    private void WriteRootFlag (ArgParser.RootFlag rootFlag, TextWriter stdout)
    {
        switch (rootFlag)
        {
            case ArgParser.RootFlag.Help:
                var helpMarkdown = _helpProvider.GetRootHelp (Registry) ??
                                   new MetadataHelpProvider ().GetRootHelp (Registry) ?? string.Empty;
                MarkdownRenderer.RenderToAnsi (helpMarkdown, stdout);
                break;
            case ArgParser.RootFlag.Version:
                stdout.WriteLine ($"{_options.ApplicationName} {_options.Version ?? "0.0.0"}");
                break;
            case ArgParser.RootFlag.OpenCli:
                stdout.WriteLine (OpenCliWriter.Generate (Registry, _options));
                break;
        }
    }

    private void RegisterBuiltIns ()
    {
        if (_options.BuiltInReplacements.TryGetValue ("help", out ICliCommand? helpReplacement))
        {
            Registry.Register (helpReplacement);
        }
        else
        {
            Registry.Register (new HelpCommand (Registry, _helpProvider));
        }

        if (_options.BuiltInReplacements.TryGetValue ("agent-guide", out ICliCommand? agentGuideReplacement))
        {
            Registry.Register (agentGuideReplacement);
        }
        else if (_options.AgentGuide is not null)
        {
            Registry.Register (new AgentGuideCommand (ResolveAgentGuide ()));
        }
    }

    private string ResolveAgentGuide ()
    {
        if (!_options.AgentGuideIsResource)
        {
            return _options.AgentGuide ?? string.Empty;
        }

        Assembly assembly = _options.ResourceAssembly ?? Assembly.GetEntryAssembly ()
            ?? throw new InvalidOperationException ("No assembly is available for resolving AgentGuide.");

        using Stream? stream = assembly.GetManifestResourceStream (_options.AgentGuide!);

        if (stream is null)
        {
            throw new InvalidOperationException ($"AgentGuide resource '{_options.AgentGuide}' was not found.");
        }

        using StreamReader reader = new (stream);
        return reader.ReadToEnd ();
    }
}
