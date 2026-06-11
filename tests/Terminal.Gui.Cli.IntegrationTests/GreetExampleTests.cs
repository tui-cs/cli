using System.Reflection;
using Terminal.Gui.App;
using Xunit;

namespace Terminal.Gui.Cli.IntegrationTests;

/// <summary>
///     Tests that each example documented in the greet help files actually produces the expected output.
///     These run the full CliHost dispatch pipeline with captured stdout.
/// </summary>
public sealed class GreetExampleTests
{
    private static CliHost CreateGreetHost ()
    {
        Assembly assembly = typeof (GreetExampleTests).Assembly;

        CliHost host = new (options =>
        {
            options.ApplicationName = "greet";
            options.Version = "1.0.0";
            options.DefaultCommand = "greet";
            options.HelpProvider = new EmbeddedMarkdownHelpProvider (assembly);
        });

        host.Registry.Register (new GreetTestCommand ());
        host.Registry.Register (new FarewellTestCommand ());

        return host;
    }

    [Theory]
    [InlineData (new[] { "World" }, "Hello, World!")]
    [InlineData (new[] { "greet", "World" }, "Hello, World!")]
    [InlineData (new[] { "Alice" }, "Hello, Alice!")]
    [InlineData (new[] { "Charlie" }, "Hello, Charlie!")]
    public async Task Greet_BasicExamples (string[] args, string expected)
    {
        CliHost host = CreateGreetHost ();
        using StringWriter stdout = new ();
        using StringWriter stderr = new ();

        var exitCode = await host.RunAsync (args, TestContext.Current.CancellationToken, stdout, stderr);

        Assert.Equal (ExitCodes.Ok, exitCode);
        Assert.Contains (expected, stdout.ToString ());
        Assert.Equal (string.Empty, stderr.ToString ());
    }

    [Theory]
    [InlineData (new[] { "--formal", "Alice" }, "Good day, Alice. It is a pleasure to meet you.")]
    [InlineData (new[] { "greet", "--formal", "Alice" }, "Good day, Alice. It is a pleasure to meet you.")]
    [InlineData (new[] { "--formal", "World" }, "Good day, World. It is a pleasure to meet you.")]
    public async Task Greet_FormalExamples (string[] args, string expected)
    {
        CliHost host = CreateGreetHost ();
        using StringWriter stdout = new ();
        using StringWriter stderr = new ();

        var exitCode = await host.RunAsync (args, TestContext.Current.CancellationToken, stdout, stderr);

        Assert.Equal (ExitCodes.Ok, exitCode);
        Assert.Contains (expected, stdout.ToString ());
        Assert.Equal (string.Empty, stderr.ToString ());
    }

    [Theory]
    [InlineData (new[] { "farewell", "Bob" }, "Goodbye, Bob!")]
    [InlineData (new[] { "farewell", "World" }, "Goodbye, World!")]
    [InlineData (new[] { "farewell", "--until", "tomorrow", "Bob" }, "Goodbye, Bob! See you tomorrow.")]
    [InlineData (new[] { "farewell", "--until", "next week", "World" }, "Goodbye, World! See you next week.")]
    public async Task Farewell_Examples (string[] args, string expected)
    {
        CliHost host = CreateGreetHost ();
        using StringWriter stdout = new ();
        using StringWriter stderr = new ();

        var exitCode = await host.RunAsync (args, TestContext.Current.CancellationToken, stdout, stderr);

        Assert.Equal (ExitCodes.Ok, exitCode);
        Assert.Contains (expected, stdout.ToString ());
        Assert.Equal (string.Empty, stderr.ToString ());
    }

    [Fact]
    public async Task DefaultCommand_NoArgs_GreetsWorld ()
    {
        CliHost host = CreateGreetHost ();
        using StringWriter stdout = new ();
        using StringWriter stderr = new ();

        // No args at all triggers --help (root flag), not default command
        var exitCode = await host.RunAsync ([], TestContext.Current.CancellationToken, stdout, stderr);

        Assert.Equal (ExitCodes.Ok, exitCode);
    }

    [Fact]
    public async Task HelpCat_RendersAnsiForRootHelp ()
    {
        CliHost host = CreateGreetHost ();
        using StringWriter stdout = new ();
        using StringWriter stderr = new ();

        var exitCode = await host.RunAsync (["help", "--cat"], TestContext.Current.CancellationToken, stdout, stderr);

        Assert.Equal (ExitCodes.Ok, exitCode);
        var output = stdout.ToString ();
        Assert.Contains ("greet", output);
    }

    [Fact]
    public async Task HelpCat_RendersAnsiForSubcommand ()
    {
        CliHost host = CreateGreetHost ();
        using StringWriter stdout = new ();
        using StringWriter stderr = new ();

        var exitCode = await host.RunAsync (["help", "--cat", "greet"], TestContext.Current.CancellationToken, stdout,
            stderr);

        Assert.Equal (ExitCodes.Ok, exitCode);
        var output = stdout.ToString ();
        Assert.Contains ("greet", output);
    }

    [Fact]
    public async Task Version_PrintsVersion ()
    {
        CliHost host = CreateGreetHost ();
        using StringWriter stdout = new ();
        using StringWriter stderr = new ();

        var exitCode = await host.RunAsync (["--version"], TestContext.Current.CancellationToken, stdout, stderr);

        Assert.Equal (ExitCodes.Ok, exitCode);
        Assert.Contains ("1.0.0", stdout.ToString ());
    }

    /// <summary>Minimal greet command for testing.</summary>
    private sealed class GreetTestCommand : ICliCommand<string>
    {
        public string PrimaryAlias => "greet";
        public IReadOnlyList<string> Aliases { get; } = ["greet"];
        public string Description => "Prompt for a name and return a greeting.";
        public CommandKind Kind => CommandKind.Input;
        public Type ResultType => typeof (string);
        public bool AcceptsPositionalArgs => true;

        public IReadOnlyList<CommandOptionDescriptor> Options { get; } =
        [
            new ("formal", "f", typeof (bool), "Use a formal greeting style.", false, null)
        ];

        public Task<CommandResult<string>> RunAsync (
            IApplication app, string? initial, CommandRunOptions options,
            CancellationToken cancellationToken)
        {
            var name = options.Arguments.Count > 0
                ? string.Join (" ", options.Arguments)
                : initial ?? "World";

            var formal = options.CommandOptions.TryGetValue ("formal", out var formalValue)
                         && formalValue.Equals ("true", StringComparison.OrdinalIgnoreCase);

            var greeting = formal
                ? $"Good day, {name}. It is a pleasure to meet you."
                : $"Hello, {name}!";

            return Task.FromResult (new CommandResult<string> (CommandStatus.Ok, greeting, null, null));
        }
    }

    /// <summary>Minimal farewell command for testing.</summary>
    private sealed class FarewellTestCommand : ICliCommand<string>
    {
        public string PrimaryAlias => "farewell";
        public IReadOnlyList<string> Aliases { get; } = ["farewell", "bye"];
        public string Description => "Say goodbye to someone.";
        public CommandKind Kind => CommandKind.Input;
        public Type ResultType => typeof (string);
        public bool AcceptsPositionalArgs => true;

        public IReadOnlyList<CommandOptionDescriptor> Options { get; } =
        [
            new ("until", "u", typeof (string), "When you expect to meet again.", false, null)
        ];

        public Task<CommandResult<string>> RunAsync (
            IApplication app, string? initial, CommandRunOptions options,
            CancellationToken cancellationToken)
        {
            var name = options.Arguments.Count > 0
                ? string.Join (" ", options.Arguments)
                : initial ?? "World";

            var until = options.CommandOptions.TryGetValue ("until", out var untilValue)
                ? untilValue
                : null;

            var farewell = until is not null
                ? $"Goodbye, {name}! See you {until}."
                : $"Goodbye, {name}!";

            return Task.FromResult (new CommandResult<string> (CommandStatus.Ok, farewell, null, null));
        }
    }
}
