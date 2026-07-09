# Survey App Agent Guide

This document describes how AI agents should interact with `survey`.

## Available Commands

### survey

An input command that collects a profile and returns it as structured data.

- **Alias:** `survey`
- **Kind:** Input
- **Result type:** `object` (`SurveyAnswers`)
- **Options:** `--name`/`-n`, `--fruits`/`-f` (comma-separated), `--sport`/`-s`,
  `--age`/`-a` (1-120), `--password`/`-p`, `--color`/`-c`

**Usage:**

```bash
survey --name Ada --age 36 --sport Fencing --fruits "Apple,Cherry" --json
```

Provide `--name` to run headless (no TUI). Use `--json` for the structured envelope.

## JSON Envelope

`survey --json` emits the structured result. The result type is serialized through a
source-generated JSON context registered on the host (no reflection):

```json
{
  "schemaVersion": 1,
  "status": "ok",
  "value": {
    "name": "Ada",
    "fruits": ["Apple", "Cherry"],
    "favoriteFruit": "Apple",
    "sport": "Fencing",
    "age": 36,
    "password": "Passw0rd!",
    "color": "Teal"
  }
}
```
