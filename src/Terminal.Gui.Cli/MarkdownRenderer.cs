using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Terminal.Gui.Cli;

/// <summary>Markdown-to-ANSI helper for help and viewer output.</summary>
public static class MarkdownRenderer
{
    /// <summary>Renders markdown as ANSI to <paramref name="output" /> and sanitizes rendered output.</summary>
    public static void RenderToAnsi (string markdown, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull (markdown);
        ArgumentNullException.ThrowIfNull (output);

        markdown = TerminalEscapeSanitizer.Sanitize (markdown)!;

        // On Windows the default Console.OutputEncoding is the OEM code page which
        // mangles Unicode box-drawing characters. Force UTF-8 for the render pass
        // and restore on exit. Only mutate when writing to the real console.
        Encoding? previousEncoding = null;
        TextWriter target = output;

        if (ReferenceEquals (output, Console.Out) && !Console.IsOutputRedirected)
        {
            previousEncoding = Console.OutputEncoding;
            Console.OutputEncoding = Encoding.UTF8;
            target = Console.Out;
        }

        int width;

        try
        {
            width = Console.WindowWidth;
        }
        catch
        {
            width = 0;
        }

        if (width <= 0)
        {
            width = 80;
        }

        int height;

        try
        {
            height = Console.WindowHeight;
        }
        catch
        {
            height = 0;
        }

        if (height <= 0)
        {
            height = 24;
        }

        var previousDriverIO = Environment.GetEnvironmentVariable ("DisableRealDriverIO");
        Environment.SetEnvironmentVariable ("DisableRealDriverIO", "1");
        IApplication app = Application.Create ();

        try
        {
            app.Init (DriverRegistry.Names.ANSI);
            app.Driver?.SetScreenSize (width, height);

            Markdown markdownView = new ()
            {
                App = app,
                UseThemeBackground = false,
                ShowCopyButtons = false,
                Width = Dim.Fill (),
                Height = Dim.Fill (),
                Text = markdown
            };

            markdownView.SetRelativeLayout (app.Screen.Size);
            markdownView.Layout ();

            var contentHeight = markdownView.GetContentHeight ();
            app.Driver?.SetScreenSize (width, contentHeight);
            markdownView.SetRelativeLayout (app.Screen.Size);
            markdownView.Frame = app.Screen with { X = 0, Y = 0 };
            markdownView.Layout ();

            app.Driver?.ClearContents ();
            markdownView.Draw ();

            var rendered = app.Driver?.ToAnsi () ?? string.Empty;
            rendered = TerminalEscapeSanitizer.SanitizeRenderedOutput (rendered);
            target.WriteLine (rendered);
        }
        finally
        {
            app.Dispose ();
            Environment.SetEnvironmentVariable ("DisableRealDriverIO", previousDriverIO);

            if (previousEncoding is not null)
            {
                Console.OutputEncoding = previousEncoding;
            }
        }
    }
}
