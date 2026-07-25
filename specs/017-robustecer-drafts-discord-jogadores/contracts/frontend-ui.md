# Contract: Frontend UI

## Draft route link

- When `draftId` is present in the draft page URL, the page attempts to open that draft first.
- If the draft cannot be opened, the page shows a localized error and keeps the list usable.

## Publication status

- Draft detail displays Discord publication state for presence, presence CTA and final teams.
- Administrators can request republish when state is failed, missing or stale.
- Reconciliation-required state is visible to administrators and cannot trigger automatic resend.
- Operational Discord identifiers, errors and audit reasons are loaded only from the administrative endpoint.
- Administrators have an explicit CTA-only republication action when `ChamadaPresenca` is in a recoverable state; the existing presence and final-team actions remain unchanged.

## Manual presence search

- Manual presence selector searches eligible players from the backend.
- It does not require loading every player in the system.
- It excludes players already confirmed in the selected draft.
- Each request is tied to the active draft, draft generation, normalized current term and monotonically increasing request version.
- Starting a newer search aborts the previous request when supported; responses that are no longer current are ignored even if cancellation races.

## Administrative reasons

- Cancel draft prompts for a reason.
- Removing a player administratively prompts for a reason when required by the action.
- Success and error messages use localized frontend keys.

## Realtime behavior

- Presence and publication changes update the currently opened draft without manual reload when the realtime connection is active.
- If realtime reconnects, the page refreshes the current draft state.
- Realtime payloads contain only the public draft projection.
