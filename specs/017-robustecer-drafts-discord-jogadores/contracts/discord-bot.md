# Contract: Discord Bot

## Command behavior

- Bot checks integration enabled state before creating or publishing drafts.
- Bot rejects past dates and invalid calendar dates before creating draft.
- Bot maps backend message codes to localized Discord messages.
- Bot logs technical details without exposing them in Discord replies.
- Mutating commands require `ManageGuild` or a role listed in `DRAFT_ADMIN_ROLE_IDS` before any API mutation.
- Read-only status and list commands remain available under the current visibility rules.
- Every mutating command and presence button checks `botEnabled` before mutation.
- Draft cancellation is web-only. The bot does not register `/draft-cancelar` and does not call the administrative draft cancellation endpoint.
- The four mutating slash commands are create draft, close presence, define captains and define pick order; their authorization requirements remain unchanged.

## Polling behavior

- Bot uses persisted publication state to decide whether a draft requires publication.
- Bot does not rely on process memory as source of truth for duplicate prevention.
- Bot records failed publication attempts through backend state.
- Bot acquires an atomic backend claim before sending.
- Bot completes or fails publication with the same claim identifier.
- Bot never republishes a state that requires reconciliation.
- Failure of one draft does not stop the remaining drafts in the polling cycle.
- Presence embed/buttons use publication type `Presenca`; the optional role CTA uses the independent type `ChamadaPresenca`.
- `Presenca` completes immediately after its own message is sent, independently of CTA outcome.
- The bot considers `ChamadaPresenca` only when `DRAFT_NOTIFY_ROLE_ID` is configured and acquires its claim before sending.
- Known pre-send CTA failures are registered as `Falha`; send or completion uncertainty remains `EmAndamento` until backend reconciliation.
- Polling and recovery of `ChamadaPresenca` never resend the `Presenca` embed.

## Permission behavior

- Missing channel access, message send, embed and role mention capabilities are reported distinctly.
- Role mention permission is required only when a role mention is configured.
- Final-team publication never requires role mention permission.
- Missing view, send, embed and mention capabilities produce distinct localized guidance.

## Republishing behavior

- Bot can publish when backend/admin requests republish.
- Republish updates backend state with new channel/message identifiers or failure details.
- CTA-only recovery claims, sends and completes only `ChamadaPresenca`.
