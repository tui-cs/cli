using System.Text.Json.Serialization.Metadata;

namespace Terminal.Gui.Cli;

/// <summary>Formats command results to stdout, stderr, or an output file.</summary>
public static class ResultWriter
{
    /// <summary>Writes <paramref name="result" /> and returns false when output file creation fails.</summary>
    public static bool Write (CommandResult result, bool jsonOutput, TextWriter stdout, TextWriter stderr,
        string? outputPath = null, IJsonTypeInfoResolver? resultJsonResolver = null)
    {
        ArgumentNullException.ThrowIfNull (stdout);
        ArgumentNullException.ThrowIfNull (stderr);

        var text = jsonOutput ? ToEnvelope (result).ToJson (resultJsonResolver) : ToPlainText (result);
        var writeToOutput = result.Status is CommandStatus.Ok or CommandStatus.NoResult;
        TextWriter writer = result.Status == CommandStatus.Error && !jsonOutput ? stderr : stdout;

        if (writeToOutput && outputPath is not null)
        {
            try
            {
                File.WriteAllText (outputPath, text);
            }
            catch (IOException ex)
            {
                stderr.WriteLine (ex.Message);
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                stderr.WriteLine (ex.Message);
                return false;
            }

            return true;
        }

        if (text.Length > 0)
        {
            writer.WriteLine (text);
        }

        return true;
    }

    private static JsonEnvelope ToEnvelope (CommandResult result)
    {
        return result.Status switch
        {
            CommandStatus.Ok => JsonEnvelope.Ok (result.Value),
            CommandStatus.Cancelled => JsonEnvelope.Cancelled (),
            CommandStatus.NoResult => JsonEnvelope.NoResult (),
            CommandStatus.Error => JsonEnvelope.Error (result.ErrorCode ?? "error",
                result.ErrorMessage ?? "Command failed."),
            _ => JsonEnvelope.Error ("error", "Command failed.")
        };
    }

    private static string ToPlainText (CommandResult result)
    {
        return result.Status switch
        {
            CommandStatus.Ok => result.Value?.ToString () ?? string.Empty,
            CommandStatus.Cancelled => "cancelled",
            CommandStatus.NoResult => string.Empty,
            CommandStatus.Error => result.ErrorMessage ?? result.ErrorCode ?? "Command failed.",
            _ => string.Empty
        };
    }
}
