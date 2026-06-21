# Frontend Contract: Drafts UI

## Route

- Path: `/drafts`
- Navigation item: `Drafts`
- View: `DraftsView`

## Primary Layout

- Header with title, subtitle and `Criar Draft` action.
- Filter row with status filter and search by draft name/player.
- Active draft operation area with cards for Team A, Team B, next pick and available players.
- Draft history list showing latest sessions and statuses.
- Empty state with friendly message and CTA.

## Components

- `DraftCreateModal`: vertical form, max two columns on desktop.
- `DraftBoard`: shows captains, teams, available players and next action.
- `DraftPickHistory`: ordered pick timeline.
- `DraftStatusBadge`: maps `Aberto`, `Concluido`, `Cancelado` to existing semantic tokens.
- `DraftCancelDialog`: confirmation modal for cancellation.

## User Feedback

- Loading uses skeleton-like cards instead of blocking spinner where practical.
- Successful create/pick/cancel shows localized feedback.
- Validation errors from backend are surfaced near the action area.

## Responsive Behavior

- Desktop: three-column board for Team A, available players, Team B.
- Tablet: two columns with available players below.
- Mobile: single-column cards with next action pinned near top.
