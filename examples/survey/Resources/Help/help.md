# survey

A sample app showing how `Terminal.Gui.Cli` and `Spectre.Console` complement each
other: Terminal.Gui handles interaction via a Wizard, Spectre.Console renders rich
output, and the host adds scriptable JSON, OpenCLI, and an agent guide.

## Commands

| Command  | Description                                          |
|----------|------------------------------------------------------|
| `survey` | Collect a profile and return it as structured data.  |
| `help`   | Show command help in a TUI markdown viewer.          |

See [survey](help:survey) for details.

## Framework Options

| Option | Description |
|--------|-------------|
| `--help` / `-h` | Show help |
| `--version` | Show version |
| `--opencli` | Emit OpenCLI metadata JSON |
| `--json` | Emit JSON envelope output |
