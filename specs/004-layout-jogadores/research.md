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
