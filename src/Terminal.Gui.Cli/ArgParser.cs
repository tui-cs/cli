using System.Globalization;

namespace Terminal.Gui.Cli;

/// <summary>Data-driven parser for framework flags, consumer globals, and per-command options.</summary>
public sealed class ArgParser
{
    /// <summary>Root flags that exit without command dispatch.</summary>
    public enum RootFlag
    {
        /// <summary>Root --help or -h.</summary>
        Help,

        /// <summary>Root --version.</summary>
        Version,

        /// <summary>Root --opencli.</summary>
        OpenCli
    }

    private readonly IReadOnlyList<GlobalOptionDescriptor> _globalOptions;
    private readonly int _maxInitialChars;

    /// <summary>Creates a parser with registered consumer globals and an --initial limit.</summary>
    public ArgParser (List<GlobalOptionDescriptor> globalOptions, int maxInitialChars = 64 * 1024)
    {
        _globalOptions = globalOptions ?? throw new ArgumentNullException (nameof (globalOptions));
        _maxInitialChars = maxInitialChars;
    }

    /// <summary>Parses command-line arguments, optionally validating against a resolved command.</summary>
    /// <param name="args">The raw command-line arguments.</param>
    /// <param name="command">The resolved command to validate options against, when known.</param>
    public ParseResult Parse (string[] args, ICliCommand? command = null)
    {
        return Parse (args, command, false);
    }

    /// <summary>Parses command-line arguments, optionally validating against a resolved command.</summary>
    /// <param name="args">The raw command-line arguments.</param>
    /// <param name="command">The resolved command to validate options against, when known.</param>
    /// <param name="unknownOptionsAsArguments">
    ///     When true, dash-prefixed tokens that match no framework, global, or command option are passed
    ///     through verbatim as positional arguments instead of failing the parse. Used by the
    ///     default-command fallback so original tokens reach the default command (issue #30).
    /// </param>
    public ParseResult Parse (string[] args, ICliCommand? command, bool unknownOptionsAsArguments)
    {
        ArgumentNullException.ThrowIfNull (args);

        if (args.Length == 0)
        {
            return new ParseResult { Success = true, RootFlag = RootFlag.Help };
        }

        var index = 0;

        if (IsRootFlag (args[0], out RootFlag rootFlag))
        {
            if (args.Length > 1)
            {
                return ParseResult.Fail ($"Unexpected argument '{args[1]}'.");
            }

            return new ParseResult { Success = true, RootFlag = rootFlag };
        }

        Dictionary<string, string> commandOptions = new (StringComparer.OrdinalIgnoreCase);
        Dictionary<string, List<string>> extensionValues = new (StringComparer.OrdinalIgnoreCase);
        List<string> arguments = new ();
        string? alias = null;
        string? initial = null;
        string? title = null;
        var json = false;
        TimeSpan? timeout = null;
        var fullscreen = false;
        var cat = false;
        string? outputPath = null;
        int? rows = null;

        while (index < args.Length)
        {
            var token = args[index];

            if (alias is null && !token.StartsWith ('-'))
            {
                alias = token;
                index++;
                continue;
            }

            if (alias is not null && !token.StartsWith ('-'))
            {
                arguments.Add (token);
                index++;
                continue;
            }

            if (TryParseFrameworkOption (args, ref index, token, ref initial, ref title, ref json, ref timeout,
                    ref fullscreen, ref cat, ref outputPath, ref rows, out var frameworkError))
            {
                if (frameworkError is not null)
                {
                    return ParseResult.Fail (frameworkError);
                }

                continue;
            }

            if (TryFindGlobalOption (token, out GlobalOptionDescriptor? globalOption))
            {
                if (!AddOptionValue (args, ref index, token, globalOption!.Name, globalOption.IsFlag,
                        globalOption.Repeatable, extensionValues, out var extensionError))
                {
                    return ParseResult.Fail (extensionError ?? $"Invalid option '{token}'.");
                }

                continue;
            }

            if (command is not null &&
                TryFindCommandOption (command, token, out CommandOptionDescriptor? commandOption))
            {
                var isFlag = commandOption!.ValueType == typeof (bool);

                if (!AddCommandOptionValue (args, ref index, token, commandOption.Name, isFlag, commandOptions,
                        out var commandError))
                {
                    return ParseResult.Fail (commandError ?? $"Invalid option '{token}'.");
                }

                continue;
            }

            if (command is null && alias is not null)
            {
                // First pass (no command): skip unknown options after alias; they'll be validated in second pass.
                index++;

                if (index < args.Length && !args[index].StartsWith ('-'))
                {
                    index++;
                }

                continue;
            }

            if (unknownOptionsAsArguments)
            {
                arguments.Add (token);
                index++;
                continue;
            }

            return ParseResult.Fail ($"Unknown option '{token}'.");
        }

        if (alias is null)
        {
            return ParseResult.Fail ("Missing command alias.");
        }

        if (initial is not null && initial.Length > _maxInitialChars)
        {
            return ParseResult.Fail ($"--initial exceeds the maximum length of {_maxInitialChars} characters.");
        }

        if (command is not null)
        {
            foreach (CommandOptionDescriptor option in command.Options)
            {
                if (option.Required && !commandOptions.ContainsKey (option.Name) && option.DefaultValue is null)
                {
                    return ParseResult.Fail ($"Missing required option '--{option.Name}'.");
                }

                if (!commandOptions.ContainsKey (option.Name) && option.DefaultValue is not null)
                {
                    commandOptions.Add (option.Name, option.DefaultValue);
                }
            }

            if (!command.AcceptsPositionalArgs && arguments.Count > 0)
            {
                return ParseResult.Fail ($"Command '{command.PrimaryAlias}' does not accept positional arguments.");
            }
        }

        Dictionary<string, IReadOnlyList<string>> extensions = extensionValues.ToDictionary (
            static pair => pair.Key,
            static pair => (IReadOnlyList<string>)pair.Value,
            StringComparer.OrdinalIgnoreCase);

        CommandRunOptions options = new ()
        {
            Initial = initial,
            Title = title,
            JsonOutput = json,
            Timeout = timeout,
            Fullscreen = fullscreen,
            Cat = cat,
            OutputPath = outputPath,
            Rows = rows,
            Arguments = arguments,
            CommandOptions = commandOptions,
            Extensions = extensions
        };

        return new ParseResult
        {
            Success = true,
            Alias = alias,
            Initial = initial,
            Options = options
        };
    }

    /// <summary>Parses duration strings accepted by --timeout: ms, s, m, h.</summary>
    public static bool TryParseTimeout (string input, out TimeSpan timeout)
    {
        timeout = default;

        if (string.IsNullOrWhiteSpace (input))
        {
            return false;
        }

        var suffix = input.EndsWith ("ms", StringComparison.OrdinalIgnoreCase) ? "ms" : input[^1..].ToLowerInvariant ();
        var numberText = suffix == "ms" ? input[..^2] : input[..^1];

        if (!double.TryParse (numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            || !double.IsFinite (value)
            || value < 0)
        {
            return false;
        }

        try
        {
            timeout = suffix switch
            {
                "ms" => TimeSpan.FromMilliseconds (value),
                "s" => TimeSpan.FromSeconds (value),
                "m" => TimeSpan.FromMinutes (value),
                "h" => TimeSpan.FromHours (value),
                _ => default
            };
        }
        catch (OverflowException)
        {
            timeout = default;
            return false;
        }

        return timeout != default || value == 0;
    }

    private static bool IsRootFlag (string token, out RootFlag rootFlag)
    {
        rootFlag = token switch
        {
            "--help" or "-h" => RootFlag.Help,
            "--version" => RootFlag.Version,
            "--opencli" => RootFlag.OpenCli,
            _ => default
        };

        return token is "--help" or "-h" or "--version" or "--opencli";
    }

    private static bool TryParseFrameworkOption (
        string[] args,
        ref int index,
        string token,
        ref string? initial,
        ref string? title,
        ref bool json,
        ref TimeSpan? timeout,
        ref bool fullscreen,
        ref bool cat,
        ref string? outputPath,
        ref int? rows,
        out string? error)
    {
        error = null;

        switch (token)
        {
            case "--initial":
                return ReadValue (args, ref index, token, out initial, out error);
            case "--title" or "-t" or "--prompt" or "-p":
                return ReadValue (args, ref index, token, out title, out error);
            case "--json":
                json = true;
                index++;
                return true;
            case "--timeout":
                if (!ReadValue (args, ref index, token, out var timeoutText, out error))
                {
                    return true;
                }

                if (!TryParseTimeout (timeoutText, out TimeSpan parsedTimeout))
                {
                    error = $"Invalid timeout '{timeoutText}'.";
                    return true;
                }

                timeout = parsedTimeout;
                return true;
            case "--fullscreen":
                fullscreen = true;
                index++;
                return true;
            case "--cat":
                cat = true;
                index++;
                return true;
            case "--output" or "-o":
                return ReadValue (args, ref index, token, out outputPath, out error);
            case "--rows":
                if (!ReadValue (args, ref index, token, out var rowsText, out error))
                {
                    return true;
                }

                if (!int.TryParse (rowsText, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedRows) ||
                    parsedRows <= 0)
                {
                    error = $"Invalid rows value '{rowsText}'.";
                    return true;
                }

                rows = parsedRows;
                return true;
            default:
                return false;
        }
    }

    private bool TryFindGlobalOption (string token, out GlobalOptionDescriptor? option)
    {
        option = _globalOptions.FirstOrDefault (candidate =>
            MatchesOption (token, candidate.Name, candidate.ShortName));
        return option is not null;
    }

    private static bool TryFindCommandOption (ICliCommand command, string token, out CommandOptionDescriptor? option)
    {
        option =
            command.Options.FirstOrDefault (candidate => MatchesOption (token, candidate.Name, candidate.ShortName));
        return option is not null;
    }

    private static bool MatchesOption (string token, string name, string? shortName)
    {
        return token.Equals ($"--{name}", StringComparison.OrdinalIgnoreCase)
               || (shortName is not null && token.Equals ($"-{shortName}", StringComparison.OrdinalIgnoreCase));
    }

    private static bool AddOptionValue (
        string[] args,
        ref int index,
        string token,
        string name,
        bool isFlag,
        bool repeatable,
        Dictionary<string, List<string>> values,
        out string? error)
    {
        error = null;

        if (!repeatable && values.ContainsKey (name))
        {
            error = $"Option '{token}' cannot be specified more than once.";
            return false;
        }

        string value;

        if (isFlag)
        {
            value = "true";
            index++;
        }
        else if (!ReadValue (args, ref index, token, out value!, out error))
        {
            return false;
        }

        if (!values.TryGetValue (name, out List<string>? optionValues))
        {
            optionValues = [];
            values.Add (name, optionValues);
        }

        optionValues.Add (value);
        return true;
    }

    private static bool AddCommandOptionValue (
        string[] args,
        ref int index,
        string token,
        string name,
        bool isFlag,
        Dictionary<string, string> values,
        out string? error)
    {
        error = null;

        if (values.ContainsKey (name))
        {
            error = $"Option '{token}' cannot be specified more than once.";
            return false;
        }

        string value;

        if (isFlag)
        {
            value = "true";
            index++;
        }
        else if (!ReadValue (args, ref index, token, out value!, out error))
        {
            return false;
        }

        values.Add (name, value);
        return true;
    }

    private static bool ReadValue (string[] args, ref int index, string token, out string value, out string? error)
    {
        value = string.Empty;
        error = null;

        if (index + 1 >= args.Length)
        {
            error = $"Option '{token}' requires a value.";
            return false;
        }

        value = args[index + 1];
        index += 2;
        return true;
    }

    /// <summary>Represents the result of parsing arguments.</summary>
    public sealed class ParseResult
    {
        /// <summary>True if parsing succeeded.</summary>
        public bool Success { get; init; }

        /// <summary>Error message when parsing failed.</summary>
        public string? Error { get; init; }

        /// <summary>The command alias, when this is not a root flag.</summary>
        public string? Alias { get; init; }

        /// <summary>The parsed initial value.</summary>
        public string? Initial { get; init; }

        /// <summary>The parsed options bag.</summary>
        public CommandRunOptions? Options { get; init; }

        /// <summary>Root flag detected before command dispatch.</summary>
        public RootFlag? RootFlag { get; init; }

        /// <summary>Creates a failed parse result.</summary>
        public static ParseResult Fail (string error)
        {
            return new ParseResult { Success = false, Error = error };
        }
    }
}
