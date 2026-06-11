# greet

A CLI greeting application built with `Terminal.Gui.Cli`.

## Commands

| Command | Description |
|---------|-------------|
| [greet](help:greet) | Prompt for a name and return a greeting |
| [farewell](help:farewell) | Say goodbye to someone |
| [info](help:info) | Display application information |
| [help](help:help) | Show this help page |

## Global Flags

| Flag | Description |
|------|-------------|
| `--help` | Print help as ANSI markdown to stdout |
| `--version` | Print the application version |
| `--json` | Output results as JSON envelopes |
| `--cat` | Render viewer content to stdout (no TUI) |

## Examples

```
greet World              Hello, World!
greet --formal Alice     Good day, Alice. It is a pleasure to meet you.
farewell Bob             Goodbye, Bob!
farewell --until tomorrow Bob
help greet               Help for the greet command
help farewell            Help for the farewell command
```

## See Also

- [Agent Guide](help:agent-guide) - Machine-readable guide for AI agents
