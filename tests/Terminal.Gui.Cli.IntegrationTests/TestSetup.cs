using System.Runtime.CompilerServices;

namespace Terminal.Gui.Cli.IntegrationTests;

/// <summary>
///     Disables real driver I/O so tests never interact with the terminal or launch processes.
/// </summary>
internal static class TestSetup
{
    [ModuleInitializer]
    internal static void Init ()
    {
        Environment.SetEnvironmentVariable ("DisableRealDriverIO", "1");
        Console.SetIn (TextReader.Null);
    }
}
