---
name: "endpoint-auth-audit"
description: "Use when auditing routes, controllers, API endpoints, webhooks, RPC methods, GraphQL resolvers, bot commands, or public handlers for missing authentication and authorization."
---

# Endpoint Auth Audit

Use this skill to find exposed endpoints or public handlers that are missing authentication, authorization, validation, or coverage. It applies to REST APIs, GraphQL, RPC, serverless functions, webhooks, bot commands, background control endpoints, and admin routes.

This skill is read-only by default. Do not change files unless the user explicitly asks for fixes.

## When To Use

- The user asks for endpoints críticos desprotegidos, route audit, API security review, controller review, webhook security, or bot command security.
- New routes, controllers, resolvers, or command handlers were added.
- A system has internal service endpoints or admin operations.
- The user wants an endpoint coverage matrix.

## Endpoint Discovery

Build a route inventory from framework metadata, source files, OpenAPI/Swagger, route declarations, generated clients, or tests.

For each endpoint capture:

- Method or command name.
- Route or trigger.
- Handler/controller/resolver file.
- Input source: route, query, body, headers, cookies, claims, event payload.
- Authentication requirement.
- Authorization policy, role, claim, scope, tenant, or ownership check.
- Whether anonymous access is intentional.
- Whether it mutates state or exposes sensitive data.
- Test coverage.

## Critical Endpoint Categories

- User, role, permission, organization, tenant, team, billing, admin, audit, and configuration management.
- Login, logout, refresh, reset password, invite, account linking, OAuth callback, MFA, token exchange.
- Create, update, delete, activate, deactivate, publish, cancel, approve, import, export, sync.
- Internal service, webhook, bot, scheduler, maintenance, metrics, debug, seed, migration, and backoffice routes.
- File upload/download, report export, PII access, secrets/configuration reads.

## Checklist

- Every non-public endpoint has authentication.
- Every sensitive or mutating endpoint has authorization beyond "is authenticated".
- Ownership checks use authenticated principal, not only request payload IDs.
- Internal endpoints have a dedicated scheme, token, signature, IP allowlist, mTLS, or equivalent control.
- Webhooks validate signatures, timestamps, replay windows, and provider identity where applicable.
- Admin endpoints are not accidentally covered by broad user policies.
- Anonymous endpoints are explicitly justified and tested.
- Route ordering does not let generic dynamic routes shadow protected fixed routes.
- Generated API docs do not expose private endpoints unintentionally.
- Endpoint tests cover success, unauthenticated, unauthorized, and cross-user/tenant access for critical flows.

## Red Flags

- `[AllowAnonymous]`, public resolver, or unguarded handler on state-changing endpoints.
- Only frontend route guards protect sensitive operations.
- Handler accepts `userId`, `tenantId`, `role`, or `scope` from body and trusts it directly.
- Same API key/token grants every internal capability.
- Bot or webhook endpoints reuse normal user auth without a clear service identity.
- Test environment bypasses auth so completely that security behavior is never tested.

## Suggested Matrix Format

| Risk | Method | Route/Command | Handler | Auth | Authorization | Coverage | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |

Use risk values:

- Critical: public or weakly protected sensitive write/admin/internal operation.
- High: authenticated but missing specific authorization or ownership checks.
- Medium: protected but missing negative tests or policy is too broad.
- Low: naming/docs/coverage clarity issue.

## Output Format

Start with exposed critical findings. Then provide the endpoint matrix or a summarized matrix if the system is large.

For each fix, recommend:

- Required auth scheme.
- Required policy/role/claim/scope.
- Ownership or tenant boundary check.
- Negative tests to add.

## Example Triggers

- "Procura endpoints desprotegidos"
- "Audita autorização das rotas"
- "Monta uma matriz de endpoints"
- "Confere se os endpoints admin estão protegidos"
- "Revisa segurança dos webhooks"
