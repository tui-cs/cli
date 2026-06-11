using Terminal.Gui.App;

namespace Terminal.Gui.Cli.Greet;

/// <summary>An input command that prompts for a name and returns a greeting.</summary>
public sealed class GreetCommand : ICliCommand<string>
{
    /// <inheritdoc />
    public string PrimaryAlias => "greet";

    /// <inheritdoc />
    public IReadOnlyList<string> Aliases { get; } = ["greet"];

    /// <inheritdoc />
    public string Description => "Prompt for a name and return a greeting.";

    /// <inheritdoc />
    public CommandKind Kind => CommandKind.Input;

    /// <inheritdoc />
    public Type ResultType => typeof (string);

    /// <inheritdoc />
    public IReadOnlyList<CommandOptionDescriptor> Options { get; } =
    [
        new ("formal", "f", typeof (bool), "Use a formal greeting style.", false, null)
    ];

    /// <inheritdoc />
    public bool AcceptsPositionalArgs => true;

    /// <inheritdoc />
    public Task<CommandResult<string>> RunAsync (
        IApplication app,
        string? initial,
        CommandRunOptions options,
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
