# Contract: Discord Bot

## Command behavior

- Bot checks integration enabled state before creating or publishing drafts.
- Bot rejects past dates and invalid calendar dates before creating draft.
- Bot maps backend message codes to localized Discord messages.
- Bot logs technical details without exposing them in Discord replies.

## Polling behavior

- Bot uses persisted publication state to decide whether a draft requires publication.
- Bot does not rely on process memory as source of truth for duplicate prevention.
- Bot records failed publication attempts through backend state.

## Permission behavior

- Missing channel access, message send, embed and role mention capabilities are reported distinctly.
- Role mention permission is required only when a role mention is configured.

## Republishing behavior

- Bot can publish when backend/admin requests republish.
- Republish updates backend state with new channel/message identifiers or failure details.
