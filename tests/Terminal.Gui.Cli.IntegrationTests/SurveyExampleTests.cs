using System.Text.Json;
using Terminal.Gui.Cli.Survey;
using Xunit;

namespace Terminal.Gui.Cli.IntegrationTests;

/// <summary>
///     Exercises the survey example end-to-end through the real configured host: headless input
///     and structured JSON (via the host's ResultJsonResolver).
/// </summary>
public sealed class SurveyExampleTests
{
    private static readonly string[] FullProfileArgs =
    [
        "survey", "--name", "Ada", "--age", "36", "--sport", "Fencing", "--fruits", "Apple,Cherry", "--color", "Teal",
        "--password", "secret123"
    ];

    [Fact]
    public async Task Survey_Headless_ReturnsSummary ()
    {
        using StringWriter stdout = new ();
        using StringWriter stderr = new ();

        var exitCode = await SurveyApp.CreateHost ()
            .RunAsync (FullProfileArgs, TestContext.Current.CancellationToken, stdout, stderr);

        Assert.Equal (ExitCodes.Ok, exitCode);
        Assert.Contains ("Ada", stdout.ToString ());
        Assert.Contains ("Fencing", stdout.ToString ());
        Assert.Equal (string.Empty, stderr.ToString ());
    }

    [Fact]
    public async Task Survey_Json_EmitsStructuredObject ()
    {
        string[] args = [.. FullProfileArgs, "--json"];
        using StringWriter stdout = new ();
        using StringWriter stderr = new ();

        var exitCode = await SurveyApp.CreateHost ()
            .RunAsync (args, TestContext.Current.CancellationToken, stdout, stderr);

        Assert.Equal (ExitCodes.Ok, exitCode);

        using JsonDocument document = JsonDocument.Parse (stdout.ToString ());
        JsonElement value = document.RootElement.GetProperty ("value");
        Assert.Equal (JsonValueKind.Object, value.ValueKind);
        Assert.Equal ("Ada", value.GetProperty ("name").GetString ());
        Assert.Equal (36, value.GetProperty ("age").GetInt32 ());
        Assert.Equal ("Teal", value.GetProperty ("color").GetString ());
        Assert.Equal (2, value.GetProperty ("fruits").GetArrayLength ());
        Assert.Equal ("secret123", value.GetProperty ("password").GetString ());
    }

    [Fact]
    public async Task Survey_Json_OmitsNullColor ()
    {
        string[] args = ["survey", "--name", "Bob", "--age", "20", "--json"];
        using StringWriter stdout = new ();
        using StringWriter stderr = new ();

        var exitCode = await SurveyApp.CreateHost ()
            .RunAsync (args, TestContext.Current.CancellationToken, stdout, stderr);

        Assert.Equal (ExitCodes.Ok, exitCode);

        using JsonDocument document = JsonDocument.Parse (stdout.ToString ());
        JsonElement value = document.RootElement.GetProperty ("value");
        Assert.False (value.TryGetProperty ("color", out _));
    }

    [Fact]
    public async Task Survey_InvalidAge_ReturnsValidationError ()
    {
        string[] args = ["survey", "--name", "Ada", "--age", "999"];
        using StringWriter stdout = new ();
        using StringWriter stderr = new ();

        var exitCode = await SurveyApp.CreateHost ()
            .RunAsync (args, TestContext.Current.CancellationToken, stdout, stderr);

        Assert.Equal (ExitCodes.ValidationError, exitCode);
        Assert.Contains ("Invalid age", stderr.ToString ());
    }

    [Fact]
    public async Task Survey_EmptyArgs_WithName_RunsDefaultCommand ()
    {
        // When empty args are passed but --name is provided via the default command routing,
        // the host should route to the survey command (not print help).
        // We verify this by passing just "--name" which goes through default command dispatch.
        string[] args = ["--name", "Ada", "--age", "25"];
        using StringWriter stdout = new ();
        using StringWriter stderr = new ();

        var exitCode = await SurveyApp.CreateHost ()
            .RunAsync (args, TestContext.Current.CancellationToken, stdout, stderr);

        // Default command routing should run the survey, not print help
        Assert.Equal (ExitCodes.Ok, exitCode);
        Assert.Contains ("Ada", stdout.ToString ());
        Assert.DoesNotContain ("--help", stdout.ToString ());
    }

    [Fact]
    public async Task Survey_ConfirmOption_Accepted ()
    {
        // The --confirm option should be recognized and not cause an error
        string[] args = ["survey", "--name", "Ada", "--age", "25", "--confirm"];
        using StringWriter stdout = new ();
        using StringWriter stderr = new ();

        var exitCode = await SurveyApp.CreateHost ()
            .RunAsync (args, TestContext.Current.CancellationToken, stdout, stderr);

        Assert.Equal (ExitCodes.Ok, exitCode);
        Assert.Contains ("Ada", stdout.ToString ());
    }
}
