using System.Text;

namespace Terminal.Gui.Cli;

/// <summary>Generates help text from registry metadata.</summary>
public sealed class MetadataHelpProvider : IHelpProvider
{
    /// <inheritdoc />
    public string? GetRootHelp (ICommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull (registry);

        StringBuilder builder = new ();
        builder.AppendLine ("## Commands");
        builder.AppendLine ();
        builder.AppendLine ("| Command | Description |");
        builder.AppendLine ("|---------|-------------|");

        foreach (ICliCommand command in registry.All)
        {
            builder.AppendLine ($"| `{command.PrimaryAlias}` | {EscapeCell (command.Description)} |");
        }

        builder.AppendLine ();
        builder.AppendLine ("## Framework Options");
        builder.AppendLine ();
        builder.AppendLine ("| Option | Description |");
        builder.AppendLine ("|--------|-------------|");
        builder.AppendLine ("| `--help`, `-h` | Show help |");
        builder.AppendLine ("| `--version` | Show version |");
        builder.AppendLine ("| `--opencli` | Emit OpenCLI metadata JSON |");
        builder.AppendLine ("| `--json` | Emit JSON envelope output |");
        builder.AppendLine ("| `--initial <value>` | Pre-fill input value |");
        builder.AppendLine ("| `--title`, `--prompt <value>` | Set window title |");
        builder.AppendLine ("| `--timeout <duration>` | Cancel after duration |");
        builder.AppendLine ("| `--cat` | Render viewer content to stdout |");

        return builder.ToString ();
    }

    /// <inheritdoc />
    public string? GetCommandHelp (ICliCommand command)
    {
        ArgumentNullException.ThrowIfNull (command);

        StringBuilder builder = new ();
        builder.AppendLine ($"# {command.PrimaryAlias}");
        builder.AppendLine (EscapeCell (command.Description));

        if (command.Options.Count > 0)
        {
            builder.AppendLine ();
            builder.AppendLine ("Options:");

            foreach (CommandOptionDescriptor option in command.Options)
            {
                var shortName = option.ShortName is null ? string.Empty : $" -{option.ShortName},";
                builder.AppendLine ($" {shortName} --{option.Name}\t{EscapeCell (option.Description)}");
            }
        }

        return builder.ToString ();
    }

    private static string EscapeCell (string? value)
    {
        if (string.IsNullOrEmpty (value))
        {
            return string.Empty;
        }

        return value.Replace ("|", "\\|").Replace ("\r\n", " ").Replace ("\n", " ").Replace ("\r", " ");
    }
}
