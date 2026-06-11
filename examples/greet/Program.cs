using System.Reflection;
using Terminal.Gui.Cli;
using Terminal.Gui.Cli.Greet;

Assembly assembly = Assembly.GetExecutingAssembly ();

CliHost host = new (options =>
{
    options.ApplicationName = "greet";
    options.Version = "1.0.0";
    options.DefaultCommand = "greet";
    options.AgentGuide = "Terminal.Gui.Cli.Greet.agent-guide.md";
    options.AgentGuideIsResource = true;
    options.ResourceAssembly = assembly;
    options.HelpProvider = new EmbeddedMarkdownHelpProvider (assembly);
});

host.Registry.Register (new GreetCommand ());
host.Registry.Register (new FarewellCommand ());
host.Registry.Register (new InfoCommand ());

return await host.RunAsync (args);
