# Implementation Plan: Layout Base e Gestao de Jogadores

**Branch**: `004-layout-jogadores` | **Date**: 2026-06-09 | **Spec**: `specs/004-layout-jogadores/spec.md`

**Input**: Feature specification from `/specs/004-layout-jogadores/spec.md`

## Summary

Implement the protected application shell for RinhaDasLendas and redesign the Jogadores screen to match the approved Figma direction. The work is primarily frontend: introduce reusable layout components for sidebar, topbar and content areas; align navigation with the feature menu; preserve current routes; and keep the player management flow operational through the existing API service with a temporary fake-service fallback for environments without a backend.

The Figma MCP tools are available, but no Figma `fileKey` or `nodeId` is present in the repository or prompt. The planning decision is to require a node-specific Figma URL before implementation tasks are executed, then map colors, typography, spacing and components from that node to the existing design tokens.

## Technical Context

**Language/Version**: Vue 3.5, TypeScript 5.9, CSS, Vite 7

**Primary Dependencies**: Vue Router 4, Axios, Pinia available for state if the task split needs shared UI/session state; existing project CSS tokens and design documentation under `docs/design/`

**Storage**: No new persistent storage. Player data uses the existing players API when available; temporary fake data is used only as a frontend fallback for offline or backend-unavailable development.

**Testing**: `npm run build`, `npm run lint`, manual responsive validation for desktop and tablet, plus existing backend tests if API contracts are touched.

**Target Platform**: Internal web application in the Dev Container, optimized for desktop and tablet browsers.

**Project Type**: Web application with separate backend and frontend roots.

**Performance Goals**: Protected pages render the shared shell without visible layout shift; player list interactions remain immediate for MVP-sized internal datasets (hundreds of players at most).

**Constraints**: Must follow the approved Figma design once node access is provided; must not invent colors, spacing, typography or component patterns outside `docs/design/`; must not break existing `/` and `/jogadores` routes; must keep business rules outside visual components.

**Scale/Scope**: One shared protected layout, one redesigned Jogadores screen, temporary placeholder content for future menu entries, and reusable contracts for future protected screens.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- MVP primeiro: PASS. The plan keeps Discord/Riot integrations out of scope and preserves manual player management.
- Uso interno: PASS. Scope is limited to internal navigation and player management rather than public-scale concerns.
- Simplicidade de uso: PASS. The shell emphasizes fast access to players, drafts, matches and statistics.
- Regras de jogo claras: PASS. Player route preferences remain visible and editable, and no hidden team/draft logic is introduced.
- Integracoes nao travam o produto: PASS. Fake-service fallback allows frontend validation without backend availability.
- Arquitetura e qualidade: PASS. Frontend remains in `FrontEnd/`; backend business rules and API contracts stay in the existing backend layers.
- Regras de dominio e evolucao: PASS. Player fields and route preferences continue to follow the established domain model from feature 003.

No constitution violations detected.

## Project Structure

### Documentation (this feature)

```text
specs/004-layout-jogadores/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── layout-ui.md
│   └── jogadores-ui-service.md
└── tasks.md   # created by /speckit-tasks
```

### Source Code (repository root)

```text
FrontEnd/
├── src/
│   ├── App.vue
│   ├── components/
│   │   ├── layout/
│   │   │   ├── AppShell.vue
│   │   │   ├── SidebarNav.vue
│   │   │   ├── Topbar.vue
│   │   │   └── ProfileMenu.vue
│   │   └── players/
│   │       ├── PlayerFormDrawer.vue
│   │       ├── PlayerList.vue
│   │       ├── PlayerCard.vue
│   │       └── PlayerDeleteDialog.vue
│   ├── router/
│   │   └── index.ts
│   ├── services/
│   │   ├── players.ts
│   │   └── fakePlayers.ts
│   ├── styles/
│   │   └── main.css
│   └── views/
│       ├── HomeView.vue
│       ├── PlayersView.vue
│       └── PlaceholderView.vue

BackEnd/
└── src/ and tests/   # no planned backend changes unless contracts regress
```

**Structure Decision**: Use the existing `FrontEnd/` and `BackEnd/` roots. This feature is frontend-led, with reusable shell and player components grouped under `FrontEnd/src/components/`. Existing backend contracts from feature 003 are consumed, not redefined, unless implementation finds a concrete mismatch.

## Phase 0: Outline & Research

Research tasks completed in `research.md`:

- Figma source-of-truth handling when a node URL is not yet present.
- Shared protected layout composition for Vue Router.
- Player data strategy for API-first use with fake fallback.
- Responsive behavior for desktop and tablet.
- Visual token alignment with `docs/design/`.

## Phase 1: Design & Contracts

Design artifacts generated:

- `data-model.md`: UI data shapes for shell, navigation, session summary, player cards, form draft, feedback states and service state.
- `contracts/layout-ui.md`: reusable layout contract for protected pages.
- `contracts/jogadores-ui-service.md`: frontend service contract for listing, creating, editing and excluding/inactivating players with fake fallback.
- `quickstart.md`: validation scenarios, commands and expected outcomes.

## Post-Design Constitution Check

- MVP primeiro: PASS. The design keeps a functional manual flow and avoids new integrations.
- Uso interno: PASS. The shell is sized for the internal platform and future MVP screens.
- Simplicidade de uso: PASS. Primary player actions are visible and grouped around the list/form flow.
- Regras de jogo claras: PASS. Route preferences remain explicit and editable.
- Integracoes nao travam o produto: PASS. The fake service is an adapter-style fallback, not a domain dependency.
- Arquitetura e qualidade: PASS. Reusable frontend components are planned without moving business rules into visual-only components.
- Regras de dominio e evolucao: PASS. No domain model changes are introduced.

No constitution violations detected after design.

## Complexity Tracking

No constitution gate violations detected; no additional complexity justifications required.
