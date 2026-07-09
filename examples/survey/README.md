# survey

A sample CLI app built with `Terminal.Gui.Cli` that demonstrates the
**Terminal.Gui + Spectre.Console** collaboration described in
[spectre.console#2128](https://github.com/spectreconsole/spectre.console/issues/2128).

It is a port of Spectre.Console's `Prompt` example: where Spectre uses blocking
console prompts, this app uses a Terminal.Gui Wizard for interaction, then renders
the collected profile with Spectre.Console.

| Concern | Owner |
|---------|-------|
| Interaction (Wizard, navigation, validation) | Terminal.Gui |
| Rich rendering (Panel, Table) | Spectre.Console |
| Scriptable surfaces (`--json`, `--opencli`, agent guide) | `Terminal.Gui.Cli` |

## Usage

```bash
# Interactive Terminal.Gui Wizard (Enter to accept, Esc to quit)
survey

# Headless: provide answers as options
survey --name Ada --age 36 --sport Fencing --fruits "Apple,Cherry" --color Teal

# Structured JSON envelope for scripts and agents
survey --name Ada --age 36 --fruits "Apple,Cherry" --json

# Browse help in the TUI markdown viewer
survey help
```

## Commands

| Command  | Description |
|----------|-------------|
| `survey` | Collect a profile and return it as structured data. |
| `help`   | Show command help in a TUI markdown viewer. |

## Running

```bash
dotnet run --project examples/survey -- survey --name Ada --json
```
