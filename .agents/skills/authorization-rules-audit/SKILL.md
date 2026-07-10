---
name: "authorization-rules-audit"
description: "Use when auditing authentication and authorization rules, roles, policies, claims, scopes, ownership checks, impersonation risks, tenant boundaries, or permission models in any system."
---

# Authorization Rules Audit

Use this skill to review whether authenticated identities are allowed to do only what they should. It complements endpoint audits by focusing on permission semantics and domain boundaries.

This skill is read-only by default. Do not change files unless the user explicitly asks for fixes.

## When To Use

- The user asks about authorization rules, permission model, roles, policies, claims, scopes, or ownership checks.
- A feature uses `userId`, `tenantId`, `organizationId`, `teamId`, `accountId`, `role`, or similar boundaries.
- The system has admin, moderator, support, bot, service account, or cross-tenant functionality.
- The user reports a possible impersonation or privilege escalation bug.

## Mental Model

Authentication answers: who is calling?

Authorization answers: what can that caller do?

Ownership answers: can that caller do it to this specific resource?

Audit all three separately.

## Checklist

- Authentication identity comes from trusted auth context, not request body.
- Authorization policy is explicit for sensitive operations.
- Ownership or tenant boundary is checked inside application/domain logic, not only in UI or route filters.
- Admin override behavior is explicit and tested.
- Service accounts and bots have limited scopes.
- Role names and permission names are centralized and consistent.
- Permission checks are not duplicated with slight differences across handlers.
- Queries filter data by user/tenant/organization when required.
- Commands reject cross-user or cross-tenant mutations.
- Audit logs capture sensitive administrative changes where applicable.
- Tests cover negative cases: unauthenticated, wrong role, wrong owner, wrong tenant, invalid service token.

## Impersonation Patterns To Look For

- Request body includes `userId` and handler uses it instead of current user.
- Query parameter includes `tenantId` and repository filters only by that parameter.
- Client-controlled role/scope changes behavior.
- Bot or internal endpoint accepts a user identifier without validating linkage.
- Admin endpoints reuse normal user update code without additional checks.
- Bulk operations skip per-item authorization.

## Permission Model Review

- List all roles and policies.
- Map role to capability.
- Map capability to endpoint/use case.
- Identify overlapping or ambiguous permissions.
- Identify permissions that are too broad for their name.
- Identify sensitive operations without dedicated permission.
- Verify names express intent: `CanManageUsers` is clearer than `AdminOnly` when multiple admin-like roles exist.

## Good Fix Patterns

- Use current authenticated principal as source of user identity.
- Resolve resource ownership server-side before mutation.
- Add policy names for capabilities, not implementation details.
- Keep authorization checks close to the use case when business ownership matters.
- Keep framework-level guards for coarse endpoint access.
- Add focused tests for cross-user and cross-tenant denial.

## Output Format

For each finding include:

- Resource or capability affected.
- Current rule.
- Missing or weak rule.
- Abuse scenario.
- Recommended rule.
- Required tests.

End with a compact permission matrix when useful:

| Capability | Roles/Scopes | Resource Boundary | Enforcement Location | Tests |
| --- | --- | --- | --- | --- |

## Example Triggers

- "Revisa as regras de autorização"
- "Confere se dá pra um usuário editar recurso de outro"
- "Audita roles e policies"
- "Procura riscos de impersonation"
- "Verifica boundary de tenant/organização"
