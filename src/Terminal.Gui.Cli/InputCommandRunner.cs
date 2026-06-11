using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Terminal.Gui.Cli;

/// <summary>Shared boilerplate for input commands that wrap a control in RunnableWrapper.</summary>
public static class InputCommandRunner
{
    /// <summary>Configures, runs, and maps the result from an input command wrapper when raw result and output value differ.</summary>
    public static async Task<CommandResult<TValue>> RunAsync<TControl, TRawResult, TValue> (
        IApplication app,
        RunnableWrapper<TControl, TRawResult> wrapper,
        CommandRunOptions options,
        string defaultTitle,
        CancellationToken cancellationToken,
        Func<TRawResult?, CommandResult<TValue>> resultMapper,
        bool addEnterBinding = true)
        where TControl : View, new()
    {
        ArgumentNullException.ThrowIfNull (app);
        ArgumentNullException.ThrowIfNull (wrapper);
        ArgumentNullException.ThrowIfNull (options);
        ArgumentNullException.ThrowIfNull (resultMapper);

        wrapper.Title = options.Title ?? defaultTitle;
        await app.RunAsync (wrapper, cancellationToken);
        return resultMapper (wrapper.Result);
    }

    /// <summary>Configures and runs a wrapper whose raw result is already the output value.</summary>
    public static Task<CommandResult<TValue>> RunAsync<TControl, TValue> (
        IApplication app,
        RunnableWrapper<TControl, TValue> wrapper,
        CommandRunOptions options,
        string defaultTitle,
        CancellationToken cancellationToken,
        bool addEnterBinding = true)
        where TControl : View, new()
    {
        return RunAsync<TControl, TValue, TValue> (
            app,
            wrapper,
            options,
            defaultTitle,
            cancellationToken,
            result => result is null
                ? new CommandResult<TValue> (CommandStatus.Cancelled, default, null, null)
                : new CommandResult<TValue> (CommandStatus.Ok, result, null, null),
            addEnterBinding);
    }
}
