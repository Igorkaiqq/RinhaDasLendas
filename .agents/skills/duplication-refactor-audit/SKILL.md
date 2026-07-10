---
name: "duplication-refactor-audit"
description: "Use when auditing duplicated code, repeated methods, validations, mappings, constants, messages, conditions, or when suggesting safe refactoring without changing behavior."
---

# Duplication Refactor Audit

Use this skill to find duplicated logic and propose safe, incremental refactors. The goal is to reduce defect risk without changing behavior unexpectedly.

This skill is read-only by default. Do not change files unless the user explicitly asks for implementation.

## When To Use

- The user asks for duplicated code, DRY audit, refactoring suggestions, or repeated validation review.
- Similar methods, validators, handlers, DTO mappings, constants, messages, or conditions appear in multiple places.
- The user wants suggestions that do not break existing behavior.
- A code review reveals repeated bug fixes across files.

## Duplication Types

- Exact copy-paste.
- Same rule expressed with different syntax.
- Same validation split across frontend/backend without a source of truth.
- Same mapping or formatting repeated in controllers, services, clients, or components.
- Same enum/status/action strings duplicated across modules.
- Same authorization check repeated inconsistently.
- Same error handling or response building repeated manually.
- Same test data setup copied across tests.

## Checklist

- Search for repeated names, literals, conditions, and validation messages.
- Compare similar handlers/controllers/components for drift.
- Look for bug-prone repeated `if` chains.
- Identify constants that should be named and reused.
- Identify validations that should share a focused validator or helper.
- Identify repeated response/error construction that should use a factory/helper.
- Check whether duplication is intentional because contexts differ.
- Avoid extracting abstraction if it hides meaningful differences.

## Refactoring Safety Rules

- Do not combine code paths until behavior is understood.
- Add or identify tests before changing shared behavior.
- Prefer small extractions over framework-level rewrites.
- Preserve public API shape unless explicitly changing it.
- Preserve error messages/codes, status codes, persistence shape, and event side effects.
- Move duplicated constants before changing logic.
- Use existing project patterns before introducing new abstractions.

## Good Refactor Candidates

- Repeated validation for the same data shape.
- Repeated URL/domain validation.
- Repeated ownership checks.
- Repeated error response construction.
- Repeated enum/status option arrays.
- Repeated API request wrappers.
- Repeated test builders or fixtures.

## Bad Refactor Candidates

- Two functions that look similar but encode different business rules.
- Premature generic repository/service abstractions.
- Helpers with boolean flags that recreate all original branches.
- Shared utilities that force unrelated modules to depend on each other.
- Refactors that require broad renames without clear payoff.

## Output Format

For each duplication finding include:

- Locations.
- Type of duplication.
- Risk of drift.
- Recommended extraction or consolidation.
- Why this is safe.
- Tests to protect behavior.

Then provide a priority list:

- Quick wins.
- Medium refactors.
- Avoid/refactor later.

## Example Triggers

- "Procura duplicações de código"
- "Audita validações repetidas"
- "Sugere refatorações sem quebrar comportamento"
- "Tem métodos parecidos demais?"
- "Quero reduzir repetição com segurança"
