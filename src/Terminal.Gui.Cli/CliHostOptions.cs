using System.Reflection;

namespace Terminal.Gui.Cli;

/// <summary>Configuration options for <see cref="CliHost" />.</summary>
public sealed class CliHostOptions
{
    private readonly Dictionary<string, ICliCommand> _builtInReplacements = new (StringComparer.OrdinalIgnoreCase);

    /// <summary>Application name shown in help, version output, and OpenCLI.</summary>
    public string ApplicationName { get; set; } = "app";

    /// <summary>Version string shown in --version and OpenCLI. Null uses 0.0.0.</summary>
    public string? Version { get; set; }

    /// <summary>Custom help provider. Null uses <see cref="MetadataHelpProvider" />.</summary>
    public IHelpProvider? HelpProvider { get; set; }

    /// <summary>Maximum characters accepted by --initial. Default is 64 KiB.</summary>
    public int MaxInitialChars { get; set; } = 64 * 1024;

    /// <summary>Agent guide embedded resource name or literal markdown. Null disables agent-guide.</summary>
    public string? AgentGuide { get; set; }

    /// <summary>True when <see cref="AgentGuide" /> is an embedded resource name; false when literal content.</summary>
    public bool AgentGuideIsResource { get; set; } = true;

    /// <summary>Assembly used to resolve embedded resources. Null falls back to <see cref="Assembly.GetEntryAssembly" />.</summary>
    public Assembly? ResourceAssembly { get; set; }

    /// <summary>Consumer-defined global options parsed into <see cref="CommandRunOptions.Extensions" />.</summary>
    public List<GlobalOptionDescriptor> GlobalOptions { get; } = [];

    /// <summary>
    ///     Alias of the default command to invoke when args don't match any registered command.
    ///     When set, bare positional args or unrecognized options are retried as args to this command.
    /// </summary>
    public string? DefaultCommand { get; set; }

    internal IReadOnlyDictionary<string, ICliCommand> BuiltInReplacements => _builtInReplacements;

    /// <summary>Replaces a library built-in command before it is registered.</summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="alias" /> is not a replaceable built-in alias.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the same built-in alias is replaced more than once.</exception>
    public void ReplaceBuiltInCommand (string alias, ICliCommand replacement)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace (alias);
        ArgumentNullException.ThrowIfNull (replacement);

        if (!IsReplaceableBuiltIn (alias))
        {
            throw new ArgumentException ($"'{alias}' is not a replaceable built-in alias.", nameof (alias));
        }

        if (!replacement.Aliases.Contains (alias, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException ($"Replacement for '{alias}' must include that alias.", nameof (replacement));
        }

        if (_builtInReplacements.ContainsKey (alias))
        {
            throw new InvalidOperationException ($"Built-in alias '{alias}' was already replaced.");
        }

        _builtInReplacements.Add (alias, replacement);
    }

    private static bool IsReplaceableBuiltIn (string alias)
    {
        return alias.Equals ("help", StringComparison.OrdinalIgnoreCase)
               || alias.Equals ("agent-guide", StringComparison.OrdinalIgnoreCase);
    }
}
