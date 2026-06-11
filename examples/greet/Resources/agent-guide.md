# Greet App Agent Guide

This document describes how AI agents should interact with `greet`.

## Available Commands

### greet

An input command that prompts the user for their name and returns a greeting.

- **Alias:** `greet`
- **Kind:** Input
- **Result type:** `string`
- **Options:**
  - `--formal` / `-f`: Use a formal greeting style (flag).

**Usage:**

```bash
greet greet --initial "World"
greet greet --initial "World" --json
greet greet --initial "World" --formal
```

### info

A viewer command that displays application information.

- **Alias:** `info`
- **Kind:** Viewer
- **Result type:** `string`
- **Supports `--cat`:** Yes

**Usage:**

```bash
greet info --cat
greet info --cat --json
```

## Framework Options

All commands support these framework options:

| Option | Description |
|--------|-------------|
| `--help` / `-h` | Show help |
| `--version` | Show version |
| `--opencli` | Emit OpenCLI metadata JSON |
| `--json` | Emit JSON envelope output |
| `--initial <value>` | Pre-fill input value |
| `--timeout <duration>` | Cancel after duration (e.g., `30s`, `5m`) |
| `--output <path>` / `-o` | Write output to file |

## JSON Envelope

All commands support `--json` to emit structured output:

```json
{
  "schemaVersion": 1,
  "status": "ok",
  "value": "Hello, World!"
}
```
