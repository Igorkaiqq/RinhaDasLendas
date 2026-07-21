# Contract: Frontend UI

## Draft route link

- When `draftId` is present in the draft page URL, the page attempts to open that draft first.
- If the draft cannot be opened, the page shows a localized error and keeps the list usable.

## Publication status

- Draft detail displays Discord publication state for presence and final teams.
- Administrators can request republish when state is failed, missing or stale.
- Reconciliation-required state is visible to administrators and cannot trigger automatic resend.
- Operational Discord identifiers, errors and audit reasons are loaded only from the administrative endpoint.

## Manual presence search

- Manual presence selector searches eligible players from the backend.
- It does not require loading every player in the system.
- It excludes players already confirmed in the selected draft.

## Administrative reasons

- Cancel draft prompts for a reason.
- Removing a player administratively prompts for a reason when required by the action.
- Success and error messages use localized frontend keys.

## Realtime behavior

- Presence and publication changes update the currently opened draft without manual reload when the realtime connection is active.
- If realtime reconnects, the page refreshes the current draft state.
- Realtime payloads contain only the public draft projection.
