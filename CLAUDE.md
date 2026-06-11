# CLAUDE.md

This file provides guidance to AI coding agents working in this repository.

## Project status

This repository implements the `Terminal.Gui.Cli` library — a hosting layer
for Terminal.Gui apps that provides CLI parsing, dispatch, JSON output, and
AI-agent discoverability.

## Source of truth

- Engineering authority: `specs/constitution.md`
- Build/style defaults: `.editorconfig`, `Directory.Build.props`

If guidance conflicts, follow `specs/constitution.md`.

## Project structure

- `src/Terminal.Gui.Cli` — class library package (`Terminal.Gui.Cli`)
- `tests/Terminal.Gui.Cli.Tests` — unit tests
- `tests/Terminal.Gui.Cli.IntegrationTests` — integration tests
- `tests/Terminal.Gui.Cli.SmokeTests` — smoke tests
- `examples/greet` — sample console app

## Build and test

```bash
dotnet restore Terminal.Gui.Cli.slnx
dotnet build Terminal.Gui.Cli.slnx --no-restore -c Debug
dotnet run --project tests/Terminal.Gui.Cli.Tests --no-build -c Debug
dotnet run --project tests/Terminal.Gui.Cli.IntegrationTests --no-build -c Debug
dotnet run --project tests/Terminal.Gui.Cli.SmokeTests --no-build -c Debug
```

## Code style verification (must pass before committing)

CI runs JetBrains ReSharper cleanup (`dotnet jb cleanupcode`) followed by
`dotnet format` and checks for a clean `git diff`. To replicate locally:

```bash
dotnet tool restore
dotnet jb cleanupcode Terminal.Gui.Cli.slnx --no-build --verbosity=WARN
dotnet format Terminal.Gui.Cli.slnx --no-restore
git diff --exit-code   # must produce no output
```

Run these commands **before committing** to avoid CI failures.

## Coding standards

- Target framework is `net10.0`.
- Keep warnings at zero (`TreatWarningsAsErrors=true`).
- Use file-scoped namespaces for new C# files.
- Use Allman braces and always include braces for conditionals/loops.
- Prefer guard clauses / early returns to deep nesting.
- One type per file for non-trivial public/internal types.
- **Use target-typed `new()`** when the type is clear from context (ReSharper enforces this).
- **Do not add redundant `using` directives** — if a namespace matches the file's own namespace prefix, it's already in scope. ReSharper will remove these.
- Do not add unrelated implementation while scaffolding.
