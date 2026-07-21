# Research: Robustecer Drafts, Discord e Jogadores

## Decision: Treat Discord publication as persisted operational state

**Rationale**: In-memory sets prevent duplicates only during one bot process lifetime. Persisting publication status, channel/message identifiers, last error and timestamps lets the system dedupe after restart and support republish.

**Alternatives considered**: Keep in-memory tracking only; rejected because restarts can duplicate messages. Store only one message ID on draft; rejected because presence and final teams need separate lifecycle and status.

## Decision: Use structured backend error codes for bot messages

**Rationale**: Bot substring matching against localized/raw error text is brittle. A structured code lets the bot map known failures to localized Discord messages reliably while logging technical details separately.

**Alternatives considered**: Continue substring matching; rejected due false positives and language coupling. Make backend return bot-specific text; rejected because bot should own Discord copy and localization.

## Decision: Keep Discord disabled as a first-class operational state

**Rationale**: The constitution requires integrations not to block the product. When disabled, bot commands and polling should avoid side effects and direct users/admins to site/manual flow.

**Alternatives considered**: Let calls fail naturally; rejected because users receive confusing errors. Disable only polling; rejected because commands could still create inconsistent expectations.

## Decision: Make presence idempotency a backend invariant

**Rationale**: Site and Discord can trigger presence changes concurrently. The backend/domain/storage layer must ensure at most one confirmed presence per player per draft.

**Alternatives considered**: Prevent duplicates only in frontend/bot; rejected because concurrent requests bypass client checks.

## Decision: Add eligible player search for manual presence

**Rationale**: Loading all players into the draft page does not scale and leaks unnecessary options. A server-side eligible search can filter active, linked, not-confirmed players.

**Alternatives considered**: Keep full list client-side; rejected because it grows poorly and duplicates business filters in UI.

## Decision: Notify draft realtime clients after presence and publication state changes

**Rationale**: The draft page already has realtime connection for draft state. Reusing it keeps users synchronized after site, bot and admin changes.

**Alternatives considered**: Poll frontend list/details; rejected because it is slower and increases API traffic.

## Decision: Administrative audit should be lightweight and scoped

**Rationale**: The feature needs traceability for cancel/removal/republication without building a full audit product. Record responsible user, timestamp, action type and optional reason.

**Alternatives considered**: Full generic audit pipeline; rejected as too broad for this feature. Logs only; rejected because operators need user-visible history.
