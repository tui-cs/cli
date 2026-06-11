# greet

A sample CLI app built with `Terminal.Gui.Cli` that generates greetings.

## Usage

```bash
# Launch the TUI greeting prompt
greet greet

# Provide a name directly (headless)
greet greet --initial "Alice"

# Formal greeting style
greet greet --initial "Bob" --formal

# JSON envelope output
greet greet --initial "World" --json

# Show help in the TUI markdown viewer
greet help

# Render help as ANSI markdown to stdout
greet help --cat

# Show root help (ANSI markdown to stdout)
greet --help

# Show version
greet --version
```

## Commands

| Command | Description |
|---------|-------------|
| `greet` | Prompt for a name and return a greeting. |
| `info`  | Display application information. |
| `help`  | Show command help in a TUI markdown viewer. |

## Building

```bash
dotnet build examples/Terminal.Gui.Cli.Greet/Terminal.Gui.Cli.Greet.csproj
```

## Running

```bash
dotnet run --project examples/Terminal.Gui.Cli.Greet -- greet --initial "World"
```
