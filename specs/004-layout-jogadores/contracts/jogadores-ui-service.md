# UI Service Contract: Jogadores

**Feature**: Layout Base e Gestao de Jogadores
**Depends on**: `specs/003-cadastro-jogadores-rotas/contracts/jogadores-api.md`

## Purpose

Provide one frontend-facing player service contract that allows the Jogadores screen to work with the existing API or a temporary fake service without moving business rules into visual components.

## Operations

### listPlayers

Input:

- `somenteAtivos` (boolean, optional)

Output:

- Array of `PlayerListItem`

Expected behavior:

- API implementation calls `GET /api/v1/jogadores`.
- Fake implementation returns deterministic sample players for design and offline validation.
- Empty arrays must be supported.

### createPlayer

Input:

- `PlayerFormDraft` in create mode

Output:

- Created `PlayerListItem`

Expected behavior:

- API implementation calls the existing create-player endpoint.
- Fake implementation creates a stable client-side id and inserts the player into the temporary list.
- Validation errors are returned as user-friendly messages.

### updatePlayer

Input:

- Player id
- `PlayerFormDraft` in edit mode

Output:

- Updated `PlayerListItem`

Expected behavior:

- API implementation calls the existing update endpoint(s) for basic data and route preferences as needed.
- Fake implementation updates the matching temporary record.
- Missing player returns a not-found style error message.

### deletePlayer

Input:

- Player id

Output:

- Success or user-friendly error

Expected behavior:

- API-backed implementation maps this action to the current inactivation flow unless a later backend contract adds physical deletion.
- Fake implementation may remove the record from the temporary list after confirmation.
- UI must require confirmation before applying the action.

## Error Contract

All implementations return errors to the view using the same shape:

```text
{
  errors: string[]
}
```

Rules:
- Prefer backend validation messages when available.
- Use clear fallback messages when API is unreachable.
- Do not expose technical transport details to the user.

## Fake Data Contract

Fake data must:

- Include active and inactive players.
- Include different elos and route preference orders.
- Include at least one player with OP.GG.
- Include at least one player without optional external links.
- Be deterministic across reloads unless implementation explicitly chooses session-only behavior.
