# Research: Layout Base e Gestao de Jogadores

**Feature**: Layout Base e Gestao de Jogadores
**Created**: 2026-06-09

## Decision: Figma remains the visual source of truth, with node access required before implementation

**Rationale**: The feature explicitly requires fidelity to Figma for sidebar, topbar and Jogadores. The Figma MCP server is available, but its design-context tools require a concrete `fileKey` and `nodeId`. No Figma URL, file key or node id exists in the repository or the feature prompt, so the plan cannot safely extract exact measurements or component metadata yet.

**Alternatives considered**:
- Guessing Figma identifiers: rejected because MCP calls require valid node identifiers and guessing would create unreliable design decisions.
- Implementing only from local docs: acceptable as a fallback for planning, but not sufficient for final visual fidelity.
- Blocking all planning: rejected because architecture, contracts and validation flows can be planned while keeping Figma extraction as a pre-implementation prerequisite.

## Decision: Use a reusable protected app shell around router views

**Rationale**: The spec requires every protected page to share sidebar, topbar and a central content area. A shell component keeps navigation, user profile summary, responsive behavior and content spacing consistent without duplicating layout code in every view.

**Alternatives considered**:
- Keeping shell directly in `App.vue`: simple initially, but makes topbar, profile menu and sidebar harder to test and reuse.
- Duplicating layout in each page: rejected because it would break the reusable structure required for future screens.

## Decision: Keep route definitions stable and add placeholders for future sections

**Rationale**: The feature must not break existing routes. Dashboard/Home and Jogadores already exist. Times, Draft, Partidas, Estatisticas and Configuracoes can route to placeholder protected content until their feature phases define real behavior.

**Alternatives considered**:
- Non-routing buttons for future sections: current temporary behavior is safe, but routing placeholders better validate the shared shell.
- Removing future menu items until implemented: rejected because the spec requires these items in the sidebar.

## Decision: Player data remains API-first with an explicit fake-service adapter

**Rationale**: Feature 003 already introduced the backend and frontend service for players. The new feature asks for temporary data while backend is absent, so the least disruptive approach is to keep the API service as primary and add a fake implementation behind the same frontend service contract for development or offline validation.

**Alternatives considered**:
- Replacing the API service with mocks: rejected because it would regress the completed player registration work.
- Hardcoding data in the view: rejected because it would mix data concerns into visual components and make tests/manual validation brittle.

## Decision: Use existing design tokens and map Figma values into them

**Rationale**: `docs/design/` defines dark-first colors, spacing, radius and typography. The plan should preserve these tokens and only add or adjust tokens after Figma comparison proves a gap and the design decision is documented.

**Alternatives considered**:
- Creating one-off CSS values from screenshots: rejected because it fragments the design system.
- Generating a new token set from scratch: rejected because the project already has design tokens.

## Decision: Desktop and tablet are required validation targets

**Rationale**: The spec requires minimum responsiveness for desktop and tablet. The design system defines desktop-first behavior with breakpoints including 1280, 1024 and 768. Validation should cover at least 1440px desktop, 1024px tablet landscape and 768px tablet portrait.

**Alternatives considered**:
- Mobile-first implementation in this phase: deferred because mobile is useful but not the required acceptance target.
- Desktop-only implementation: rejected because tablet is an explicit non-functional requirement.

## Figma Extraction: Rinha das Lendas node 1:1702

**Source**: `https://www.figma.com/design/LzgFvkseX36IRNhEIO8uyV/Rinha-das-Lendas?node-id=1-1702&t=cyFEHPHASTp1gXdF-4`

**fileKey**: `LzgFvkseX36IRNhEIO8uyV`

**nodeId**: `1:1702`

**Decision**: Map the Figma layout to the existing Vue app with a reusable shell and player card grid.

**Rationale**: The node describes a desktop player directory with fixed 240px sidebar, 64px topbar, 40px main canvas padding, filter bar, four-column player grid, and simple pagination. The implementation keeps those dimensions as the desktop baseline and adapts to tablet using two-column grids and collapsed sidebar behavior.

**Colors extracted**:
- App canvas: `#0c1320`
- Sidebar: `#070e1b`
- Surface/card/filter: `#151c29`
- Secondary surface/buttons: `#2e3543`
- Border: `#464555`
- Primary action/accent: `#5f59f7`
- Main text: `#dce2f5`
- Muted text: `#c7c4d8`
- Subtle text: `#918fa1`
- Positive/active light text: `#c2c1ff`
- Danger/unavailable: `#ffb4ab`

**Typography extracted**:
- Body and headings use Hanken Grotesk.
- Navigation labels, filters, badges and stats use JetBrains Mono.
- Page heading: 24px / 32px, bold.
- Card title: 18px / 24px, semibold.
- Body/filter input text: 14px / 20-21px.
- Navigation/filter labels: 12px / 16px with 0.96px letter spacing.

**Spacing and layout extracted**:
- Sidebar width: 240px.
- Topbar height: 64px.
- Main canvas padding: 40px.
- Main section gap: 24px.
- Filter bar gap: 16px, padding 17px, border radius 8px.
- Player grid gap: 24px, desktop four columns.
- Player cards: padding 17px, border radius 8px.
- Avatar size: 56px.
- Pagination top border spacing: 25px.

**Components extracted**:
- `AppShell`: sidebar + topbar + central scrollable content.
- `SidebarNav`: brand, navigation links, active left border, tournament CTA, support/logout footer.
- `Topbar`: global search, Discord link, notification/settings buttons, profile pill.
- `PlayersView`: hero heading/action, filter bar, player grid, pagination.
- `PlayerCard`: avatar, elo/status badges, identity, route information, external profile action.
- `PlayerFormDrawer` and `PlayerDeleteDialog`: implementation additions for feature management flows not fully represented in the static Figma card grid.

**Alternatives considered**:
- Using Figma asset URLs for icons/avatars: rejected because those URLs are short-lived MCP assets and should not become app runtime dependencies.
- Keeping the previous sidebar inside `App.vue`: rejected because the Figma layout requires a reusable app shell and a topbar shared across protected pages.
