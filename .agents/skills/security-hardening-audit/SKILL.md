---
name: "security-hardening-audit"
description: "Use when auditing security hardening, production safety, secrets, JWT/session/cookies, CORS, HTTPS, rate limiting, headers, logging, dependency exposure, or unsafe defaults in any system."
---

# Security Hardening Audit

Use this skill to review whether a system is safe to run in production and resilient against common security failures. Focus on practical, testable risks rather than theoretical checklists.

This skill is read-only by default. Do not change files unless the user explicitly asks for fixes.

## When To Use

- The user asks for security, hardening, production-readiness, or configuration audit.
- The project adds auth, sessions, JWT, OAuth, internal tokens, bot/service integrations, webhooks, or admin features.
- The user asks whether secrets, environment variables, Docker, deployment, or logs are safe.
- You see a bug report involving unauthorized access, token handling, CORS, cookies, or production config.

## Security Areas To Inspect

- Secrets and credentials.
- Authentication and session management.
- Authorization and ownership checks.
- Internal service authentication.
- Input validation and payload trust boundaries.
- Transport security and browser security controls.
- Rate limiting and abuse controls.
- Error handling and information disclosure.
- Logging and observability safety.
- Production configuration validation.
- Dependency and supply-chain exposure.

## Hardening Checklist

- Production refuses missing, blank, placeholder, or dev-only secrets.
- JWT keys, session secrets, cookie signing keys, OAuth secrets, and API tokens are not hardcoded.
- `.env.example` documents required variables without real secrets.
- Cookies use appropriate `HttpOnly`, `Secure`, `SameSite`, path, domain, and expiration settings.
- Token validation checks issuer, audience, lifetime, signing key, and small clock skew when applicable.
- Refresh/session flows rotate or revoke tokens where appropriate.
- Password reset, account linking, invite, webhook, and callback flows validate state and expiry.
- Internal APIs use explicit service tokens, mTLS, signed requests, or equivalent controls.
- CORS is not wildcarded with credentials and is environment-specific.
- HTTPS redirection and HSTS are enabled in production web apps when applicable.
- Rate limiting protects login, reset password, public write endpoints, webhooks, and expensive reads.
- Error responses avoid stack traces, raw exception messages, SQL errors, tokens, and provider internals.
- Logs do not include passwords, tokens, cookies, auth headers, reset codes, or personal data beyond policy.
- Health checks do not leak sensitive dependency details publicly.
- Debug endpoints, Swagger, admin panels, consoles, and seed tooling are disabled or protected in production.
- Container images avoid unnecessary root privileges, secrets in layers, and broad exposed ports.

## Red Flags

- Production boot succeeds with `change-me`, `dev-only`, `password`, `postgres`, `admin`, or blank secrets.
- Auth failures are swallowed or treated as anonymous success.
- A user ID from request body overrides the authenticated user ID.
- Internal tokens are accepted from query strings without strong reason.
- Admin or internal endpoints share the same protection as normal authenticated user endpoints.
- Authorization exists in UI only.
- Tests authenticate everything automatically without a separate security test path.
- Security messages are hardcoded inconsistently or leak sensitive details.

## Verification Ideas

- Add tests proving production rejects insecure defaults.
- Add tests proving invalid internal tokens fail.
- Add tests proving users cannot operate on resources owned by others.
- Add tests proving critical anonymous endpoints are intentionally anonymous.
- Run dependency audit tools suitable for the stack.
- Run the app in production-like environment with missing secrets and verify startup fails safely.

## Output Format

Report findings by severity:

- Critical and High first.
- Include file/line references when possible.
- Explain exploit path or failure mode.
- Recommend the smallest safe fix.
- Include a verification test for each High/Critical finding.

End with:

- Production readiness verdict: `Ready`, `Ready with caveats`, or `Not ready`.
- Required fixes before production.
- Optional hardening improvements.

## Example Triggers

- "Audita segurança e hardening"
- "Esse projeto está pronto para produção?"
- "Verifica secrets, JWT, cookies e CORS"
- "Procura configs inseguras"
- "Revisa segurança do bot interno/webhook"
