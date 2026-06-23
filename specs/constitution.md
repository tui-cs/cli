# tui-cs/cli Constitution

**Version**: 1.1 | **Ratified**: 2026-05-23 | **Last Amended**: 2026-05-23

This constitution governs all contributions to `tui-cs/cli`. It is the highest-authority engineering document in this repository.

## I. Purpose & Scope

`Terminal.Gui.Cli` is a `.NET` class library for exposing `Terminal.Gui` capabilities through scriptable CLI surfaces.

- Package ID: `Terminal.Gui.Cli`
- Namespace: `Terminal.Gui.Cli`
- TFM: `net10.0`

## II. Non-Goals

Until implementation begins, this repository remains scaffold-first and should not accrue speculative production features.

## III. Architectural and Engineering Rules (C1-C8)

Every PR must comply with all rules below.

### C1 — Only CliHost calls Terminal.Gui lifecycle APIs

Initialization and shutdown of Terminal.Gui runtime lifecycle APIs must be centralized in `CliHost`. No command, helper, or utility type may call lifecycle entrypoints directly.

### C2 — Public API changes require spec updates

Any change to public API surface must include corresponding updates in `specs/` in the same PR.

### C3 — No reflection-based command discovery

Command discovery must be explicit and deterministic. Reflection scanning for command registration or dispatch is prohibited.

### C4 — Source-generated JSON only

Runtime reflection-based JSON serialization is disallowed. JSON paths must use source-generated `System.Text.Json` contexts.

### C5 — Tests run in parallel and must avoid process-global mutation

Test projects run in parallel by default. Tests must not mutate process-global state unless explicitly isolated with collection-level opt-outs.

### C6 — Commands must never call Environment.Exit

Command implementations return exit codes/results through framework abstractions and must not terminate the process directly.

### C7 — Schema v1 is append-only

For versioned machine-readable contracts, `v1` schemas are append-only. Existing fields/semantics are not removed or redefined.

### C8 — Zero warnings

Warnings are treated as errors. Builds and CI must remain warning-free.

## IV. Public API Layout

Public API is documented in `specs/library-spec.md`. Two narrow file-layout exceptions are intentional: `CommandResult` and `CommandResult<T>` live together in `CommandResult.cs`, and `ICliCommand<TValue>` lives in `ICliCommandGeneric.cs`. Do not use angle brackets in filenames; the `Generic` suffix is the established convention for this single generic-interface companion file.

## V. Testing Tiers

- `Terminal.Gui.Cli.Tests` — unit tests
- `Terminal.Gui.Cli.IntegrationTests` — integration tests
- `Terminal.Gui.Cli.SmokeTests` — smoke-level validation

## VI. Governance

Constitution changes require a pull request that updates this file and explains the rationale and migration impact.
