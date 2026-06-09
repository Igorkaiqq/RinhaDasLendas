# Data Model: Layout Base e Gestao de Jogadores

**Feature**: Layout Base e Gestao de Jogadores
**Created**: 2026-06-09

This feature does not introduce new persistent backend entities. It defines UI state models and frontend service contracts that consume the player domain created in feature 003.

## UI Entities

### AppShell

- `sidebar`: `SidebarState`
- `topbar`: `TopbarState`
- `contentTitle` (string, optional)
- `contentActions` (array, optional)

Validation rules:
- Shell must wrap protected pages only.
- Shell must always expose a content region with a stable accessible landmark.

### SidebarNavigationItem

- `id` (string, required)
- `label` (string, required)
- `icon` (string or component key, required)
- `routeName` (string, optional)
- `path` (string, optional)
- `status` (enum: `active`, `available`, `placeholder`, `disabled`)

Required items:
- Dashboard
- Jogadores
- Times
- Draft
- Partidas
- Estatisticas
- Configuracoes

Validation rules:
- `id` values must be unique.
- Items with `available` or `active` status must define a route or path.
- The current route must mark exactly one matching item as active when a match exists.

### TopbarUserSummary

- `displayName` (string, required)
- `subtitle` (string, optional)
- `avatarUrl` (string, optional)
- `initials` (string, fallback when no avatar exists)
- `menuItems` (array of profile actions)

Validation rules:
- If `avatarUrl` is absent, initials must be available.
- Profile menu actions must be visible but may use placeholder behavior until authentication/profile features are implemented.

### PlayerListItem

Derived from the existing `Jogador` API response.

- `id` (UUID string)
- `nomeExibicao` (string)
- `discord` (string, optional)
- `riotId` (string, optional)
- `opGgUrl` (URL string, optional)
- `elo` (string, optional)
- `divisao` (string, optional)
- `status` (enum: `Ativo`, `Inativo`)
- `preferencias` (array of `RoutePreference`)

Validation rules:
- `nomeExibicao` is required for display.
- `status` must render as a badge.
- `opGgUrl` renders as an external link only when present and valid.
- `preferencias` must preserve the route order from priority 1 to 5.

### PlayerFormDraft

- `mode` (enum: `create`, `edit`)
- `id` (UUID string, required for edit)
- `nomeExibicao` (string)
- `discord` (string, optional)
- `riotId` (string, optional)
- `opGgUrl` (URL string, optional)
- `deepLolUrl` (URL string, optional)
- `elo` (string, optional)
- `divisao` (string, optional)
- `preferencias` (array of `RoutePreference`)

Validation rules:
- Create mode requires the fields already required by the current player flow.
- Edit mode must update visible player fields without changing route rules.
- Route preferences must contain the five League roles, unique priorities 1..5 and at most one blocked route.

### RoutePreference

Uses the existing feature 003 shape:

- `rota` (enum: `Top`, `Jungle`, `Mid`, `Adc`, `Support`)
- `prioridade` (number, 1..5)
- `naoJogoNemLascando` (boolean)

Validation rules:
- Exactly five route entries in normal forms.
- Priority values must be unique.
- At most one route can be marked as blocked.

### FeedbackState

- `type` (enum: `idle`, `loading`, `empty`, `success`, `error`)
- `message` (string, optional)
- `errors` (array of strings, optional)

Validation rules:
- Loading state should use skeleton presentation for list areas.
- Empty state must include a clear next action.
- Error state must show user-friendly messages from the API or fallback service.

## State Transitions

### PlayerFormDraft

```text
idle -> editing -> validating -> saving -> success -> idle
idle -> editing -> validating -> error -> editing
```

### PlayerListItem

```text
Ativo -> Inativo
```

Deletion in the UI may remove a player from temporary fake data. When connected to the current backend, destructive action should map to the existing inactivation behavior unless a later API contract adds physical deletion.

## Relationships

- `AppShell` contains many `SidebarNavigationItem` entries.
- `AppShell` contains one `TopbarUserSummary`.
- `PlayersView` displays many `PlayerListItem` entries.
- `PlayerListItem` contains many `RoutePreference` entries.
- `PlayerFormDraft` edits one `PlayerListItem` at a time.
