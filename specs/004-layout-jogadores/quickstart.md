# Quickstart: Layout Base e Gestao de Jogadores

**Feature**: Layout Base e Gestao de Jogadores
**Created**: 2026-06-09

## Prerequisites

- Dev Container running.
- Dependencies installed in `FrontEnd/`.
- Node-specific Figma URL available before implementation begins.
- Optional: backend API running for API-backed player validation.

## Design Validation Setup

1. Extract `fileKey` and `nodeId` from the Figma URL for the main layout and Jogadores screen.
2. Use the Figma MCP design-context tool to capture layout structure, screenshot and component hints.
3. Compare the Figma output with:
   - `docs/design/DESIGN_SYSTEM.md`
   - `docs/design/DESIGN_TOKENS.md`
   - `docs/design/UI_GUIDELINES.md`
4. Record token or component gaps before implementation.

## Run Frontend

```bash
cd FrontEnd
npm install
npm run dev
```

Open the local Vite URL printed by the command.

## Validate Build

```bash
cd FrontEnd
npm run build
```

Expected outcome:

- Type checking succeeds.
- Vite production build finishes without errors.

## Validate Lint

```bash
cd FrontEnd
npm run lint
```

Expected outcome:

- ESLint completes without remaining errors after automatic fixes.

## Manual Acceptance Scenarios

### Shared Layout

1. Open Dashboard/Home.
2. Confirm sidebar, topbar and content region are visible.
3. Open Jogadores.
4. Confirm the same shell remains visible and Jogadores is active in the sidebar.
5. Click Times, Draft, Partidas, Estatisticas and Configuracoes.
6. Confirm each item either opens placeholder content or preserves safe navigation without errors.

### Topbar

1. Confirm application name is visible.
2. Confirm avatar or initials are visible.
3. Confirm user display information is visible.
4. Open the profile menu.
5. Confirm menu actions are visible and do not overlap content.

### Jogadores List

1. Open Jogadores with sample or API data.
2. Confirm each player displays name, Discord, elo, preferred routes, OP.GG link when available and status.
3. Confirm empty state appears when the service returns no players.
4. Confirm loading uses skeleton presentation.

### Player Management

1. Create a player with valid data.
2. Confirm the player appears in the list.
3. Edit the player fields and route preferences.
4. Confirm the list reflects updated data.
5. Trigger deletion/inactivation.
6. Confirm the UI asks for confirmation before applying the action.

### Responsiveness

Validate at these widths:

- 1440px desktop
- 1024px tablet landscape
- 768px tablet portrait

Expected outcome:

- Sidebar/topbar remain usable.
- Player list and form/drawer do not overlap.
- Text fits within controls, cards and navigation items.
- Primary actions remain discoverable.

## Backend Regression Check

If implementation touches player API consumption, run:

```bash
dotnet test BackEnd/tests/RinhaDasLendas.Tests/RinhaDasLendas.Tests.csproj
```

Expected outcome:

- Existing player API and domain tests pass.
