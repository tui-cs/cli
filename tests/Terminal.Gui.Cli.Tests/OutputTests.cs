using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Terminal.Gui.Cli.Tests;

public sealed class OutputTests
{
    [Fact]
    public void JsonEnvelope_ToJson_UsesCamelCaseAndOmitsNulls ()
    {
        var json = JsonEnvelope.Ok ("value").ToJson ();

        using JsonDocument document = JsonDocument.Parse (json);
        Assert.Equal (1, document.RootElement.GetProperty ("schemaVersion").GetInt32 ());
        Assert.Equal ("ok", document.RootElement.GetProperty ("status").GetString ());
        Assert.Equal ("value", document.RootElement.GetProperty ("value").GetString ());
        Assert.False (document.RootElement.TryGetProperty ("code", out _));
    }

    [Fact]
    public void JsonEnvelope_ToJson_WithResolver_EmbedsConsumerTypeAsObject ()
    {
        var json = JsonEnvelope.Ok (new SampleResult ("Alice", 30, null))
            .ToJson (SampleJsonContext.Default);

        using JsonDocument document = JsonDocument.Parse (json);
        JsonElement value = document.RootElement.GetProperty ("value");
        Assert.Equal (JsonValueKind.Object, value.ValueKind);
        Assert.Equal ("Alice", value.GetProperty ("name").GetString ());
        Assert.Equal (30, value.GetProperty ("age").GetInt32 ());
        Assert.False (value.TryGetProperty ("note", out _));
    }

    [Fact]
    public void ResultWriter_WritesErrorsToStderrInPlainText ()
    {
        using StringWriter stdout = new ();
        using StringWriter stderr = new ();

        var success = ResultWriter.Write (new CommandResult (CommandStatus.Error, null, "validation", "bad"), false,
            stdout, stderr);

        Assert.True (success);
        Assert.Equal (string.Empty, stdout.ToString ());
        Assert.Contains ("bad", stderr.ToString ());
    }

    [Fact]
    public void TerminalEscapeSanitizer_RemovesOscAndPreservesRenderedSgr ()
    {
        Assert.Equal ("title", TerminalEscapeSanitizer.Sanitize ("\u001b]0;bad\u0007title"));
        Assert.Equal ("\u001b[1mstrong\u001b[0m",
            TerminalEscapeSanitizer.SanitizeRenderedOutput ("\u001b[1mstrong\u001b[0m"));
    }
}

internal sealed record SampleResult (string Name, int Age, string? Note);

[JsonSourceGenerationOptions (
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable (typeof (SampleResult))]
internal sealed partial class SampleJsonContext : JsonSerializerContext;
