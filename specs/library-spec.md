# Terminal.Gui.Cli Library Specification

This repository implements the `Terminal.Gui.Cli` package API described by issue "Terminal.Gui.Cli Library Specification".

Public API additions must keep the following contracts aligned with implementation:

- Command model: `CommandKind`, `CommandStatus`, `CommandOptionDescriptor`, `CommandResult`, and `CommandResult<T>`.
- Command interfaces: `ICliCommand`, `ICliCommand<TValue>`, and `IViewerCommand`.
- Registry: `ICommandRegistry` and `CommandRegistry` with case-insensitive alias resolution and duplicate rejection.
- Host and parser: `CliHost`, `CliHostOptions`, `CommandRunOptions`, `GlobalOptionDescriptor`, and `ArgParser`.
- Help and built-ins: `IHelpProvider`, `MetadataHelpProvider`, `EmbeddedMarkdownHelpProvider`, `HelpCommand`, and `AgentGuideCommand`.
- Output and metadata: `JsonEnvelope`, `ResultWriter`, `OpenCliWriter`, `ExitCodes`, `TypeNames`, `TerminalEscapeSanitizer`, and `MarkdownRenderer`.
- Input helper: `InputCommandRunner`.

## Result value JSON serialization

The `--json` envelope serializes `CommandResult.Value` through the source-generated
`CliJsonContext` (constitution C4). That built-in context only resolves the library's own
value types, so consumer commands that return custom result types must supply a
source-generated resolver:

- `CliHostOptions.ResultJsonResolver` (`IJsonTypeInfoResolver?`) — a consumer
  `JsonSerializerContext` (or any resolver) registered on the host.
- `JsonEnvelope.ToJson(IJsonTypeInfoResolver?)` and the optional `resultJsonResolver`
  parameter on `ResultWriter.Write` thread that resolver through serialization.

The resolver is combined with `CliJsonContext` via `JsonTypeInfoResolver.Combine`, keeping
the path reflection-free and AOT-compatible. When `ResultJsonResolver` is null, envelope
values remain restricted to the built-in value types.

## Default command dispatch

`CliHostOptions.DefaultCommand` (`string?`) names the alias to invoke when args do not
resolve to a registered command. When set, `CliHost` routes to that command in three cases:

- Args fail to parse (instead of a usage error).
- Args are empty (which otherwise maps to root `Help`).
- The leading token is not a recognized command alias.

In each case the host re-parses `[DefaultCommand, ..args]` against the resolved default
command, so bare positional args and unrecognized options are retried as args to it. The
reparse uses `ArgParser.Parse(args, command, unknownOptionsAsArguments: true)`: dash-prefixed
tokens that match no framework, global, or default-command option pass through verbatim as
positional arguments (e.g. `app --literal` and `app Alice --suffix` reach the default command
as positionals), while recognized options still parse as options. If the default command does
not accept positional args, leftover tokens still produce the usual positional-args usage
error. If `DefaultCommand` names an alias that is not registered, the host emits
`Default command '<name>' is not registered.` and returns a usage error. When
`DefaultCommand` is null, the original parse/usage-error behavior is preserved.

`CommandResult` and `CommandResult<T>` intentionally live together in `CommandResult.cs`. `ICliCommand<TValue>` intentionally lives in `ICliCommandGeneric.cs`; do not use angle brackets in filenames.
