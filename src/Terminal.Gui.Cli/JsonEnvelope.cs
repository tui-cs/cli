using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Terminal.Gui.Cli;

/// <summary>The stable wire format for CLI output.</summary>
public sealed class JsonEnvelope
{
    /// <summary>Wire schema version. Always 1 for library major version 1.x.</summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>Status string: ok, cancelled, error, or no-result.</summary>
    public string Status { get; init; } = "ok";

    /// <summary>Result value. Omitted when null.</summary>
    public object? Value { get; init; }

    /// <summary>Error code. Omitted when null.</summary>
    public string? Code { get; init; }

    /// <summary>Error message. Omitted when null.</summary>
    public string? Message { get; init; }

    /// <summary>Creates an ok envelope.</summary>
    public static JsonEnvelope Ok (object? value = null)
    {
        return new JsonEnvelope { Status = "ok", Value = value };
    }

    /// <summary>Creates a cancelled envelope.</summary>
    public static JsonEnvelope Cancelled ()
    {
        return new JsonEnvelope { Status = "cancelled" };
    }

    /// <summary>Creates an error envelope.</summary>
    public static JsonEnvelope Error (string code, string message)
    {
        return new JsonEnvelope { Status = "error", Code = code, Message = message };
    }

    /// <summary>Creates a no-result envelope.</summary>
    public static JsonEnvelope NoResult ()
    {
        return new JsonEnvelope { Status = "no-result" };
    }

    /// <summary>Serializes using the source-generated JSON context.</summary>
    public string ToJson ()
    {
        return ToJson (null);
    }

    /// <summary>
    ///     Serializes the envelope using the source-generated context. When <paramref name="resultResolver" /> is
    ///     provided it is combined with the built-in context so consumer-defined <see cref="Value" /> types resolve
    ///     without reflection.
    /// </summary>
    public string ToJson (IJsonTypeInfoResolver? resultResolver)
    {
        if (resultResolver is null)
        {
            return JsonSerializer.Serialize (this, CliJsonContext.Default.JsonEnvelope);
        }

        JsonSerializerOptions options = new (CliJsonContext.Default.Options)
        {
            TypeInfoResolver = JsonTypeInfoResolver.Combine (CliJsonContext.Default, resultResolver)
        };

        return JsonSerializer.Serialize (this, options.GetTypeInfo (typeof (JsonEnvelope)));
    }
}

[JsonSourceGenerationOptions (
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable (typeof (JsonEnvelope))]
internal partial class CliJsonContext : JsonSerializerContext
{
}
