using System.Runtime.CompilerServices;
using Xunit;

[assembly: CollectionBehavior (DisableTestParallelization = true)]

namespace Terminal.Gui.Cli.IntegrationTests;

internal static class TestSetup
{
    [ModuleInitializer]
    internal static void Init ()
    {
        Environment.SetEnvironmentVariable ("DisableRealDriverIO", "1");
        Console.SetIn (TextReader.Null);
    }
}
