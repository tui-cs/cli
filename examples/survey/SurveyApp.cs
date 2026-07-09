using System.Reflection;

namespace Terminal.Gui.Cli.Survey;

/// <summary>Builds the configured <see cref="CliHost" /> for the survey example.</summary>
public static class SurveyApp
{
    /// <summary>Creates and configures the host with the survey and card commands registered.</summary>
    public static CliHost CreateHost ()
    {
        Assembly assembly = typeof (SurveyApp).Assembly;

        CliHost host = new (options =>
        {
            options.ApplicationName = "survey";
            options.Version = "1.0.0";
            options.DefaultCommand = "survey";
            options.AgentGuide = "Terminal.Gui.Cli.Survey.agent-guide.md";
            options.AgentGuideIsResource = true;
            options.ResourceAssembly = assembly;
            options.HelpProvider = new EmbeddedMarkdownHelpProvider (assembly);
            options.ResultJsonResolver = SurveyJsonContext.Default;
        });

        host.Registry.Register (new SurveyCommand ());
        return host;
    }
}
