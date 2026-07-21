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
  "messageCode": "PresenceAlreadyClosed",
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
