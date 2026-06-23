# Hero GIF Recording Guide

Produces `docs/images/hero.gif` — an animated GIF demonstrating the example app's CLI features.

## Prerequisites

- [tuirec](https://github.com/tui-cs/tuirec) v0.3.4+ on PATH (`go install github.com/tui-cs/tuirec/cmd/tuirec@latest`)
- .NET 10 SDK (for building the example app)
- PowerShell 7+ (`pwsh`) on PATH
- `agg` is auto-downloaded by tuirec on first use

## Build the example app

```powershell
dotnet build examples/Terminal.Gui.Cli.ExampleApp -c Debug --nologo
```

## Record

The recording uses a PowerShell script to run multiple CLI invocations in sequence,
captured by tuirec in `--inline` mode (no alternate screen, normal scrolling buffer).

```powershell
$binary = (Resolve-Path "examples/Terminal.Gui.Cli.ExampleApp/bin/Debug/net10.0/Terminal.Gui.Cli.ExampleApp.exe").Path

# Create the demo script that runs each CLI feature
$script = @"
Write-Host '`$ example-app --help' -ForegroundColor Green
& '$binary' --help
Write-Host ''
Write-Host '`$ example-app greet --initial "World" --json' -ForegroundColor Green
& '$binary' greet --initial 'World' --json
Write-Host ''
Write-Host '`$ example-app info --cat' -ForegroundColor Green
& '$binary' info --cat
Write-Host ''
Write-Host '`$ example-app --opencli' -ForegroundColor Green
& '$binary' --opencli
"@
$script | Set-Content -Path artifacts/demo.ps1 -Encoding utf8

tuirec record `
    --binary "pwsh" `
    --args "-NoProfile","-File","artifacts/demo.ps1" `
    --name "hero" `
    --inline `
    --show-command '$ ./demo.ps1' `
    --keystrokes 'wait:5000' `
    --startup-delay 3000 `
    --drain 2000 `
    --cols 100 `
    --rows 35 `
    --keystroke-delay 60 `
    --max-duration 20 `
    --verbosity high

# Copy to final location
Copy-Item ./artifacts/hero.gif ./docs/images/hero.gif -Force
```

## Demo sequence

The script runs four CLI invocations back-to-back. All output appears in a single scrolling terminal frame:

| Feature                | Command                                         |
|------------------------|-------------------------------------------------|
| Help                   | `example-app --help` — lists commands + options |
| JSON envelope          | `example-app greet --initial "World" --json`    |
| Headless viewer        | `example-app info --cat`                        |
| Agent discovery        | `example-app --opencli` — machine-readable JSON |

## Tuning tips

- **Terminal size**: 100x35 gives enough vertical space to show all outputs without scrolling previous content off-screen.
- **`--inline` mode**: Required because the demo runs non-interactive commands that print to stdout and exit. Without `--inline`, tuirec captures an empty alternate screen.
- **`--startup-delay 3000`**: Allows `pwsh` to start. Reduce on fast machines.
- **`--drain 2000`**: Holds the final frame so viewers can read the opencli output.
- **GIF too large**: Reduce `--rows` or remove one of the demo commands. Current result is ~75KB.
- **Wrong binary path**: After building, verify the exe exists. On Linux/macOS omit `.exe`.

## Troubleshooting

1. **Empty GIF (just the show-command prompt)**: You probably forgot `--inline`. Without it, the normal-screen output from pwsh is invisible.
2. **Output garbled or overlapping**: Increase `--startup-delay` so pwsh finishes initialization before tuirec starts capturing.
3. **GIF not generated**: Ensure `agg` was downloaded. Check `./artifacts/` for the `.cast` file; if present, re-run `agg` manually.
4. **PowerShell colors wrong**: The demo uses `Write-Host -ForegroundColor Green` for prompts. This renders as ANSI color 10 which most themes show as green.
