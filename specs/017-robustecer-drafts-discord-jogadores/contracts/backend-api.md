# Contract: Backend API

## Draft link resolution

- Existing draft detail endpoint returns draft when the caller can view it.
- Missing, cancelled or inaccessible drafts return a localized standard error response.

## Discord configuration usage

- Discord configuration exposes whether the bot is enabled.
- Bot-facing operations must avoid side effects when Discord integration is disabled.

## Structured error response

All bot-consumed endpoints return the standard error body with a stable message code when a known business or validation failure occurs.

Expected error shape:

```json
{
  "messageCode": "MV072",
  "message": "Localized message",
  "errors": []
}
```

## Publication state

Draft responses include publication state summary for presence and final teams when applicable.

Required publication summary fields:
- type
- status
- channel identifier
- message identifier
- last attempted date
- published date
- last error code
- claim expiration when an attempt is in progress

Public responses expose only publication type and status. Operational identifiers and errors are restricted to bot or administrator projections.

## Publication claim

`POST /api/v1/draft-montagens/{id}/discord/publicacoes/claim` is restricted to the internal bot scheme.

Request fields:
- publication type

Successful claim response fields:
- acquired flag
- claim identifier
- claim expiration
- publication status

Rules:
- Claim acquisition is atomic and exactly one concurrent caller succeeds.
- Completion and failure requests include the claim identifier.
- A different or stale claim is rejected with a stable localized code.
- Expired attempts become reconciliation required and are not automatically publishable.

## Republish operations

Administrators can request republication of presence list or final teams for a draft.

Expected outcomes:
- Returns updated draft/publication state.
- Does not duplicate active publication when an existing message is still valid.
- Records failed state with reason when Discord publication cannot be completed.

## Eligible manual presence search

Administrators can search eligible players for a draft presence.

Required filters:
- search text
- page
- page size

Expected result:
- paginated eligible players
- excludes already confirmed players
- excludes inactive/unlinked players

## Realtime notifications

Draft state updates are emitted after presence, publication, republish, pick and administrative changes.

Realtime payloads use the public projection and never include administrative reasons, actors, Discord IDs or failure details.

## Administrative projection

`GET /api/v1/draft-montagens/{id}/administracao` requires `CanManageDrafts` and returns audit entries plus operational publication details. The common detail endpoint does not return those fields.

## Authentication and throttling

- Production startup rejects missing, placeholder or shorter-than-32-character internal tokens.
- Internal token comparison is constant-time.
- API throttling is partitioned by bot identity, authenticated user or anonymous IP.
- Authentication, authorization and rate-limit failures use localized `ApiErrorResponse` bodies.
