using System.Reflection;
using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Xunit;

namespace Terminal.Gui.Cli.IntegrationTests;

/// <summary>
///     In-process UI render harness for commands. Ported from clet's CletUiHarness.
///     Runs a command with the ANSI driver at a fixed screen size, captures the initial
///     render, then stops. Enables visual golden-file assertions.
/// </summary>
internal sealed class CommandUiHarness : IAsyncDisposable
{
    private readonly string? _ansiSnapshot;
    private readonly IApplication _app;
    private readonly Task<CommandResult> _commandTask;
    private readonly CancellationTokenSource _cts;
    private readonly string? _textSnapshot;

    private CommandUiHarness (
        IApplication app,
        CancellationTokenSource cts,
        Task<CommandResult> commandTask,
        string? ansiSnapshot,
        string? textSnapshot)
    {
        _app = app;
        _cts = cts;
        _commandTask = commandTask;
        _ansiSnapshot = ansiSnapshot;
        _textSnapshot = textSnapshot;
    }

    public async ValueTask DisposeAsync ()
    {
        try
        {
            if (!_commandTask.IsCompleted)
            {
                await _cts.CancelAsync ();

                try
                {
                    await _commandTask;
                }
                catch (OperationCanceledException)
                {
                }
            }
        }
        finally
        {
            _cts.Dispose ();
            _app.Dispose ();
        }
    }

    /// <summary>Start a harness for a viewer command.</summary>
    public static async Task<CommandUiHarness> StartViewerAsync (
        IViewerCommand viewer,
        CommandRunOptions? options = null,
        int width = 80,
        int height = 18)
    {
        Application.AppModel = AppModel.FullScreen;

        IApplication app = Application.Create ().Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (width, height);

        CancellationTokenSource cts = new ();
        string? ansiSnapshot = null;
        string? textSnapshot = null;
        var iterations = 0;
        var previousHash = 0;
        var sawNonEmpty = false;
        var stableCount = 0;
        const int stableThreshold = 2;
        const int maxIterations = 50;

        EventHandler<EventArgs<IApplication?>> handler = (_, _) =>
        {
            iterations++;
            ansiSnapshot = app.Driver?.ToAnsi ()?.Replace ("\r\n", "\n").Replace ("\r", "\n");
            textSnapshot = BuildTextSnapshot (app.Driver?.Contents);

            var (hash, nonEmpty) = HashContents (app.Driver?.Contents);

            if (!sawNonEmpty)
            {
                if (nonEmpty)
                {
                    sawNonEmpty = true;
                    previousHash = hash;
                    stableCount = 1;
                }

                return;
            }

            if (hash == previousHash)
            {
                stableCount++;

                if (stableCount >= stableThreshold)
                {
                    app.RequestStop ();
                }
            }
            else
            {
                previousHash = hash;
                stableCount = 1;
            }

            if (iterations >= maxIterations)
            {
                app.RequestStop ();
            }
        };

        app.Iteration += handler;
        Task<CommandResult> task;

        try
        {
            task = viewer.RunAsync (app, null, options ?? new CommandRunOptions (), cts.Token);
            await task;
        }
        finally
        {
            app.Iteration -= handler;
        }

        return new CommandUiHarness (app, cts, task, ansiSnapshot, textSnapshot);
    }

    /// <summary>Get the rendered screen as plain text.</summary>
    public string SnapshotText ()
    {
        return _textSnapshot ?? BuildTextSnapshot (_app.Driver?.Contents);
    }

    /// <summary>Get the rendered screen as ANSI with styling.</summary>
    public string SnapshotAnsi ()
    {
        return _ansiSnapshot ?? _app.Driver?.ToAnsi () ?? string.Empty;
    }

    /// <summary>
    ///     Compare against a golden file under Goldens/. Set UPDATE_SNAPSHOTS=1 to regenerate.
    /// </summary>
    public void AssertMatchesAnsiGolden (string fileName)
    {
        var actual = SnapshotAnsi ();
        var path = ResolveGoldenPath (fileName);
        var regen = Environment.GetEnvironmentVariable ("UPDATE_SNAPSHOTS") is "1" or "true";

        if (!File.Exists (path))
        {
            if (regen)
            {
                WriteGolden (path, actual);
                Assert.Fail ($"Golden created at {path}. Re-run without UPDATE_SNAPSHOTS to verify.");
            }

            Assert.Fail ($"Golden not found: {path}. Run with UPDATE_SNAPSHOTS=1 to create it.");
        }

        var expected = File.ReadAllText (path).Replace ("\r\n", "\n").Replace ("\r", "\n");

        if (expected == actual)
        {
            return;
        }

        var actualPath = path + ".actual";
        WriteGolden (actualPath, actual);

        if (regen)
        {
            WriteGolden (path, actual);
            Assert.Fail ($"Golden updated at {path}. Re-run without UPDATE_SNAPSHOTS to verify.");
        }

        Assert.Fail ($"""
                      Golden '{fileName}' does not match.

                      Plain-text render:
                      ---
                      {SnapshotText ()}
                      ---

                      Actual written to: {actualPath}
                      Expected at:       {path}
                      """);
    }

    private static string ResolveGoldenPath (string fileName)
    {
        var sourcePath = typeof (CommandUiHarness).Assembly
            .GetCustomAttributes (typeof (AssemblyMetadataAttribute), false)
            .Cast<AssemblyMetadataAttribute> ()
            .FirstOrDefault (a => a.Key == "GoldensSourcePath")
            ?.Value;

        if (!string.IsNullOrEmpty (sourcePath))
        {
            return Path.Combine (sourcePath, fileName);
        }

        var assemblyDir = Path.GetDirectoryName (typeof (CommandUiHarness).Assembly.Location)!;
        return Path.Combine (assemblyDir, "Goldens", fileName);
    }

    private static void WriteGolden (string path, string content)
    {
        Directory.CreateDirectory (Path.GetDirectoryName (path)!);
        File.WriteAllText (path, content, new UTF8Encoding (false));
    }

    private static string BuildTextSnapshot (Cell[,]? contents)
    {
        if (contents is null)
        {
            return string.Empty;
        }

        var rows = contents.GetLength (0);
        var cols = contents.GetLength (1);
        StringBuilder sb = new (rows * (cols + 1));

        for (var r = 0; r < rows; r++)
        {
            var lineStart = sb.Length;

            for (var c = 0; c < cols; c++)
            {
                var g = contents[r, c].Grapheme;
                sb.Append (string.IsNullOrEmpty (g) ? " " : g);
            }

            var end = sb.Length;

            while (end > lineStart && char.IsWhiteSpace (sb[end - 1]))
            {
                end--;
            }

            sb.Length = end;
            sb.Append ('\n');
        }

        return sb.ToString ();
    }

    private static (int Hash, bool NonEmpty) HashContents (Cell[,]? contents)
    {
        if (contents is null)
        {
            return (0, false);
        }

        var rows = contents.GetLength (0);
        var cols = contents.GetLength (1);
        var hash = 17;
        var nonEmpty = false;

        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                var g = contents[r, c].Grapheme;
                hash = unchecked(hash * 31 + (string.IsNullOrEmpty (g) ? 0 : g.GetHashCode ()));

                if (!string.IsNullOrEmpty (g) && g != " ")
                {
                    nonEmpty = true;
                }
            }
        }

        return (hash, nonEmpty);
    }
}
