using Terminal.Gui.App;

namespace Terminal.Gui.Cli.Greet;

/// <summary>An input command that says goodbye to someone.</summary>
public sealed class FarewellCommand : ICliCommand<string>
{
    /// <inheritdoc />
    public string PrimaryAlias => "farewell";

    /// <inheritdoc />
    public IReadOnlyList<string> Aliases { get; } = ["farewell", "bye"];

    /// <inheritdoc />
    public string Description => "Say goodbye to someone.";

    /// <inheritdoc />
    public CommandKind Kind => CommandKind.Input;

    /// <inheritdoc />
    public Type ResultType => typeof (string);

    /// <inheritdoc />
    public IReadOnlyList<CommandOptionDescriptor> Options { get; } =
    [
        new ("until", "u", typeof (string), "When you expect to meet again.", false, null)
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
        var until = options.CommandOptions.TryGetValue ("until", out var untilValue)
            ? untilValue
            : null;

        var farewell = until is not null
            ? $"Goodbye, {name}! See you {until}."
            : $"Goodbye, {name}!";

        return Task.FromResult (new CommandResult<string> (CommandStatus.Ok, farewell, null, null));
    }
}
