---
name: "architecture-standards-audit"
description: "Use when auditing architectural standards, layers, dependency direction, DDD/CQRS boundaries, controller/service responsibilities, DTO/entity exposure, dependency injection, or module architecture in any system."
---

# Architecture Standards Audit

Use this skill to verify whether a system follows its intended architecture and whether responsibilities are placed in the correct layer or module.

This skill is read-only by default. Do not change files unless the user explicitly asks for implementation.

## When To Use

- The user asks for architecture review, standards audit, DDD/CQRS review, layering review, or module boundary review.
- A feature added controllers, handlers, services, repositories, UI state, jobs, integrations, or persistence changes.
- The project has architecture documents, specs, ADRs, or established conventions.
- The user wants refactoring suggestions aligned with architecture without behavior changes.

## Architecture Sources

Read project-specific authority first:

- Constitution or engineering principles.
- Architecture docs and ADRs.
- Feature specs, plans, tasks, and design docs.
- Existing implementation patterns.
- Test structure.
- Public API standards.
- UI/design system standards when frontend is involved.

If no documentation exists, infer current architecture conservatively and call out assumptions.

## Checklist

- Dependencies point inward or in the intended direction.
- Domain/business rules are not implemented only in controllers, routes, UI components, database triggers, or infrastructure adapters.
- Controllers/routes/handlers stay thin where the architecture expects use cases/services.
- DTOs/contracts do not leak persistence entities unintentionally.
- Application/use-case layer coordinates behavior without owning infrastructure details.
- Infrastructure adapters do not leak provider-specific concepts into domain logic.
- Queries and commands are separated when the architecture requires CQRS.
- Dependency injection is explicit and avoids service locator patterns unless intentionally used.
- Transactions and unit-of-work boundaries are clear.
- External integrations are wrapped behind ports/adapters/facades when useful.
- Validation is layered appropriately: syntactic input validation, business invariants, persistence constraints.
- Tests exist at the right level: domain rules, validators, use cases, integration boundaries.

## Common Violations

- Domain depends on HTTP, database ORM, framework types, or external provider SDKs.
- UI contains the only copy of a business rule.
- Controller performs complex branching, authorization, persistence, and mapping directly.
- Repository contains business decisions unrelated to persistence.
- Handler directly reads configuration or external APIs instead of using an abstraction.
- DTOs and persistence entities are the same by accident.
- A single service becomes a transaction script for unrelated use cases.
- CQRS commands return large read models inconsistently without project convention.
- Tests only exercise happy-path HTTP and miss domain/application rules.

## Refactoring Guidance

- Prefer moving logic to the nearest appropriate existing layer.
- Do not introduce DDD/CQRS/patterns where the project does not use or need them.
- Use ports/adapters for external systems only when it improves testability or isolation.
- Keep controllers/routes as orchestration boundaries, not business rule owners.
- Keep domain pure when the architecture requires it.
- Add tests before moving business logic.

## Output Format

For each finding include:

- Architectural rule or convention.
- Current violation.
- Location.
- Impact.
- Minimal corrective refactor.
- Tests or docs to update.

End with:

- Architecture conformance summary.
- Top three structural risks.
- Suggested remediation sequence.

## Example Triggers

- "Audita arquitetura e padrões"
- "Confere se as camadas estão corretas"
- "Revisa DDD/CQRS"
- "Procura regras de negócio no lugar errado"
- "Sugere refatorações arquiteturais seguras"
