# UI Contract: Protected Layout

**Feature**: Layout Base e Gestao de Jogadores
**Audience**: Frontend implementation and validation

## Protected Shell

Every protected page must render inside the same shell:

```text
AppShell
├── SidebarNav
├── Topbar
└── main content region
```

## SidebarNav Contract

Required visible navigation items:

| Label | Route expectation | Status for this feature |
|-------|-------------------|-------------------------|
| Dashboard | `/` or named dashboard route | Available |
| Jogadores | `/jogadores` | Available |
| Times | placeholder route or placeholder content | Placeholder |
| Draft | placeholder route or placeholder content | Placeholder |
| Partidas | placeholder route or placeholder content | Placeholder |
| Estatisticas | placeholder route or placeholder content | Placeholder |
| Configuracoes | placeholder route or placeholder content | Placeholder |

Behavior:
- The current page must be visually active.
- Placeholder entries must not break navigation.
- Sidebar remains visible on desktop.
- Sidebar may collapse on tablet but must keep recognizable icons and tooltips or accessible labels.

## Topbar Contract

Required content:

- Application name: `RinhaDasLendas`
- User avatar or initials fallback
- User display name
- Basic user subtitle such as role or group identifier
- Profile menu trigger

Behavior:
- Profile menu may contain placeholder actions until authentication/profile features are specified.
- Topbar must not obscure page content at desktop or tablet widths.
- Topbar must share spacing, color and typography with the Figma design and project tokens.

## Content Region Contract

Behavior:
- Page content renders inside a constrained, scrollable central area.
- Page-level actions belong near the page heading or in the topbar action area, according to the Figma node.
- Protected pages without final functionality render a clear placeholder state instead of a broken route.

## Visual Source Contract

Before implementation tasks are executed:

1. Obtain the node-specific Figma URL for the main layout and Jogadores screen.
2. Call the Figma MCP design-context tool with the extracted `fileKey` and `nodeId`.
3. Map returned colors, typography, spacing, component hierarchy and screenshots to existing `docs/design/` tokens.
4. Document any token gap before introducing new CSS variables.

If Figma access is temporarily unavailable, implementation may continue only with explicit approval and must remain aligned with `docs/design/`.
