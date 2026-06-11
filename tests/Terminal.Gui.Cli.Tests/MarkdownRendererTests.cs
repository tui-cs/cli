using Xunit;

namespace Terminal.Gui.Cli.Tests;

[Collection ("MarkdownRenderer")]
public sealed class MarkdownRendererTests
{
    [Fact]
    public void RenderToAnsi_RestoresDisableRealDriverIO_WhenUnset ()
    {
        // Arrange: clear the env var
        Environment.SetEnvironmentVariable ("DisableRealDriverIO", null);

        using StringWriter output = new ();

        // Act
        MarkdownRenderer.RenderToAnsi ("# Hello", output);

        // Assert: should be restored to null (unset)
        var after = Environment.GetEnvironmentVariable ("DisableRealDriverIO");
        Assert.Null (after);
    }

    [Fact]
    public void RenderToAnsi_RestoresDisableRealDriverIO_WhenPreviouslySet ()
    {
        // Arrange: set the env var to a known value
        Environment.SetEnvironmentVariable ("DisableRealDriverIO", "0");

        using StringWriter output = new ();

        // Act
        MarkdownRenderer.RenderToAnsi ("# Hello", output);

        // Assert: should restore to the previous value, not leave it as "1"
        var after = Environment.GetEnvironmentVariable ("DisableRealDriverIO");
        Assert.Equal ("0", after);

        // Cleanup
        Environment.SetEnvironmentVariable ("DisableRealDriverIO", null);
    }

    [Fact]
    public void RenderToAnsi_ProducesOutput ()
    {
        using StringWriter output = new ();

        MarkdownRenderer.RenderToAnsi ("**bold**", output);

        Assert.NotEmpty (output.ToString ());
    }
}
