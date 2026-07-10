---
name: "code-organization-audit"
description: "Use when auditing code organization, file responsibilities, naming, enums/constants separation, too many classes per file, large files, module boundaries, or maintainability structure."
---

# Code Organization Audit

Use this skill to identify organization problems that make a codebase harder to understand, test, and evolve. Focus on responsibilities, boundaries, names, file structure, constants/enums, and module cohesion.

This skill is read-only by default. Do not change files unless the user explicitly asks for refactoring.

## When To Use

- The user asks about code organization, architecture cleanliness, naming, standards, file structure, or maintainability.
- Files contain many classes, DTOs, commands, handlers, validators, constants, or enum-like values.
- The user asks whether something should be split, moved, renamed, or reorganized.
- A feature became hard to review because unrelated concepts are grouped together.

## What To Inspect

- Large files and modules.
- Files with many classes, records, interfaces, components, hooks, commands, DTOs, or validators.
- Folders that mix layers, feature areas, or unrelated responsibilities.
- Constants, enum values, string literals, status values, route names, action names, permissions, and message codes.
- Naming consistency across backend, frontend, clients, bots, tests, and docs.
- Public API names versus internal implementation names.
- Imports that reveal dependency direction problems.

## Heuristics

- One primary public type per file is usually easier to navigate.
- Group by feature or responsibility, not by accidental chronology.
- Split files when changes to one concept force reviewers to scan unrelated concepts.
- Keep reusable validation or mapping in focused helpers only when duplication already exists or is likely.
- Avoid central mega-files for unrelated constants.
- Prefer names that explain business intent over transport or UI implementation.
- Avoid creating abstractions just to make files smaller.

## Checklist

- Each file has a clear primary responsibility.
- Each folder has a clear organizing principle.
- Commands, queries, handlers, DTOs, validators, components, and services are easy to locate.
- Enum-like values are centralized in stable modules and reused instead of duplicated strings.
- Constants are split by domain or feature, not dumped into a global bucket.
- Names are consistent across layers for the same concept.
- Tests mirror the structure enough to be discoverable.
- Dependency direction follows the intended architecture.
- Generated code, migrations, and build artifacts are not mixed with hand-written source.

## Red Flags

- `Dtos.cs`, `Commands.cs`, `Handlers.cs`, `Validators.cs`, `Constants.ts`, or `Utils.ts` contains many unrelated concepts.
- A file has multiple public classes that change for different reasons.
- A component mixes data fetching, authorization decisions, formatting, layout, and mutation logic.
- A service handles authentication, persistence, validation, mapping, and notifications in one place.
- Constants or enum strings are duplicated between frontend, backend, bot, scripts, or tests with no synchronization strategy.
- Folder names do not match project architecture or domain language.

## Safe Refactoring Guidance

- Prefer moving types to separate files without changing code first.
- Keep namespaces/module exports stable unless there is a clear reason to change them.
- Add barrel/index exports only when they improve imports and do not hide ownership.
- Split validators by request/use case while extracting shared validation only when it removes real duplication.
- Move constants into feature-specific modules with explicit names.
- Run formatting and tests after moves.

## Output Format

For each finding include:

- Location.
- Responsibility smell.
- Why it matters.
- Minimal refactor.
- Behavior risk.
- Suggested verification.

Include a refactoring sequence when multiple files are involved:

1. Move-only changes.
2. Import/export cleanup.
3. Optional shared helper extraction.
4. Tests/format verification.

## Example Triggers

- "Identifica arquivos com muitas responsabilidades"
- "Procura muitas classes no mesmo arquivo"
- "Revisa organização de enums e constantes"
- "Audita padrões de nomes e estrutura"
- "Esse módulo deveria ser quebrado?"
