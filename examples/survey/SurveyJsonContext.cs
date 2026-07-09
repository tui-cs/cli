using System.Text.Json.Serialization;

namespace Terminal.Gui.Cli.Survey;

/// <summary>
///     Source-generated JSON context for <see cref="SurveyAnswers" />. Registered on the host via
///     <c>CliHostOptions.ResultJsonResolver</c> so the <c>--json</c> envelope can serialize the result
///     without reflection (constitution C4).
/// </summary>
[JsonSourceGenerationOptions (
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable (typeof (SurveyAnswers))]
public sealed partial class SurveyJsonContext : JsonSerializerContext;
