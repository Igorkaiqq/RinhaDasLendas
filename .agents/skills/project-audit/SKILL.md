---
name: "project-audit"
description: "Use when asked for a general project audit, pente fino, codebase review, quality review, architecture/security/test audit, or broad risk assessment across any software system."
---

# Project Audit

Use this skill to run a broad, practical audit of any software project. It is a general-purpose "pente fino" workflow for finding risks, inconsistencies, missing protections, weak tests, architectural drift, duplicated logic, naming problems, and maintainability issues.

This skill should be read-only by default. Do not change files unless the user explicitly asks for implementation after the audit.

## When To Use

- The user asks for a project audit, full review, pente fino, quality review, architecture review, or risk assessment.
- The user wants to know what should be fixed before production, release, merge, or handoff.
- The user asks for a standardized analysis across backend, frontend, scripts, infrastructure, tests, or documentation.
- The task is broader than one bug, one endpoint, or one isolated file.

## Core Principles

- Build context before judging. Read project instructions, architecture docs, specs, README, package files, test setup, and entry points first.
- Prefer concrete evidence over generic advice. Every finding should reference files, lines, commands, configs, or observed behavior when possible.
- Separate risk from taste. Report only issues that can affect security, correctness, maintainability, operability, testability, or consistency.
- Do not recommend rewrites when a small refactor or guardrail is enough.
- Preserve behavior unless a behavior is explicitly unsafe or incorrect.
- Prioritize fixes that can be tested.

## Audit Flow

1. Identify project shape.
2. Read governing documentation.
3. Map application boundaries.
4. Inspect security posture.
5. Inspect endpoint or public API exposure.
6. Inspect authorization and ownership rules.
7. Inspect organization, naming, and responsibilities.
8. Inspect duplications and validation reuse.
9. Inspect tests and coverage gaps.
10. Produce a severity-ranked report.

## Context Checklist

- Project instructions: `AGENTS.md`, `README.md`, `.cursor/rules`, `.github/copilot-instructions.md`, or similar.
- Architecture docs: `docs/architecture`, ADRs, specs, plans, design docs.
- Runtime entry points: API startup, frontend bootstrap, workers, CLIs, bots, serverless handlers.
- Dependency manifests: `.csproj`, `package.json`, `pom.xml`, `build.gradle`, `requirements.txt`, `pyproject.toml`, `go.mod`, `Cargo.toml`.
- Configuration: environment examples, Dockerfiles, compose files, deployment manifests, appsettings, config modules.
- Tests: unit, integration, e2e, security, contract, snapshot, migration tests.

## General Checklist

- Architecture boundaries are clear and respected.
- Business rules are not hidden only in UI, controllers, routes, or persistence code.
- Public APIs do not expose persistence/domain entities directly unless intentionally designed.
- Critical write operations have authentication, authorization, validation, and tests.
- Secrets are not committed and insecure defaults are rejected in production.
- Errors do not leak secrets or internals.
- User-facing messages follow the project localization strategy.
- Large files and modules have coherent responsibilities.
- Repeated validation, mapping, constants, enums, and branching logic are centralized where useful.
- Tests cover security boundaries, ownership checks, and important workflows.
- Documentation reflects current behavior.

## Findings Severity

- Critical: exploitable security issue, data loss/corruption, production-blocking misconfiguration, or public critical endpoint without protection.
- High: authorization bypass, unsafe default in production, missing validation for sensitive flow, broken architecture boundary likely to cause defects.
- Medium: maintainability or consistency issue that increases defect risk, duplicated rules, incomplete tests for important behavior.
- Low: naming, organization, documentation, or cleanup issue with limited immediate risk.

## Output Format

Start with findings ordered by severity. Keep summaries secondary.

For each finding include:

- Severity
- Location
- Evidence
- Impact
- Recommended fix
- Suggested test or verification

Then include:

- Open questions
- Positive observations, only if useful
- Suggested phased remediation plan

## Example Triggers

- "Faz um pente fino no projeto"
- "Audita esse sistema antes de produção"
- "Revisa arquitetura, segurança e padrões"
- "Quais riscos você vê nesse codebase?"
- "Quero uma auditoria geral sem alterar arquivos"
