# Mesa de Draft Redesign

## Status

Approved direction for planning. Implementation has not started.

## Context

RinhaDasLendas is an internal League of Legends platform for admins, moderators, captains and players. The product manages player profiles, teams, draft presence, Discord publication, and operational decisions around internal matches.

The current design system already defines a dark-first competitive SaaS direction with purple/blue accents, cards, sidebar navigation and an "arena de draft" concept. The redesign must make the product cleaner and more human without becoming generic, overly cute, or exaggeratedly gamer.

The user chose the full-system scope and approved the visual direction named **Mesa de Draft**.

## Subject, Audience and Single Job

Subject: an internal League of Legends draft operations desk.

Audience: admins, moderators, captains and players who need to coordinate presence, teams and match operations quickly.

Single job: help the user understand what state the rinha is in and perform the next operational action with minimal friction.

## Design Thesis

The interface should feel like a tactical table before a match: quiet, organized, confident and specific to draft operations. The memorable element is not decoration; it is a functional **draft rail** that shows where the user is in the flow and what can happen next.

The redesign must preserve product speed and clarity. The UI should not look like an ERP, a generic admin template, a Valorant clone, or a neon gamer dashboard.

## Visual Direction

### Palette

Use a dark, slightly blue-tinted base with controlled competitive accents.

```yaml
rift-void: "#070A12"
panel-smoke: "#101522"
lane-slate: "#1B2433"
spell-violet: "#7C3AED"
summoner-blue: "#38BDF8"
ban-gold: "#C8A24A"
```

`ban-gold` is the intentional aesthetic risk. It should be used sparingly for captain, pick/ban, publication attention or decisive moments. It must not become a generic button color.

Semantic state colors remain aligned with the existing design tokens for success, warning, danger and info.

### Typography

Display: `Space Grotesk`, used for page titles, hero thesis and high-level states.

Body: keep `Hanken Grotesk`. It is warmer than Inter and supports the goal of making the product cleaner and more human without losing operational clarity.

Utility/data: `JetBrains Mono`, only for compact operational facts: routes, status labels, times, IDs, role tags and counters.

Type should become part of the identity: page titles should be tighter and more deliberate, while dense operational copy should remain readable.

### Layout

The system uses a structured operations layout:

```text
┌────────────┬─────────────────────────────────────┐
│ sidebar    │ top context: page state + action     │
│ arena nav  ├─────────────────────────────────────┤
│            │ draft rail / operational state       │
│            ├──────────────┬──────────────────────┤
│            │ primary work │ contextual side panel │
│            │ surface      │ summary/actions       │
└────────────┴──────────────┴──────────────────────┘
```

Mobile collapses to:

```text
┌──────────────────────────┐
│ compact top nav           │
├──────────────────────────┤
│ current state / draft rail│
├──────────────────────────┤
│ primary content           │
├──────────────────────────┤
│ contextual actions        │
└──────────────────────────┘
```

### Signature Element: Draft Rail

The draft rail is the identity device for the system. It encodes real operational states instead of acting as decoration.

For drafts, it maps states such as:

- presença aberta;
- presença encerrada;
- capitães definidos;
- ordem definida;
- draft em andamento;
- finalizado;
- publicação Discord.

For non-draft pages, a lighter variant can show page context, filters or user progress only when it carries useful information.

## shadcn-vue Strategy

The frontend does not currently have `components.json` or installed shadcn-vue UI components. Adoption must be controlled and phased.

Required setup decisions for the implementation plan:

- initialize shadcn-vue inside `FrontEnd/`;
- map current tokens to shadcn-compatible CSS variables in `FrontEnd/src/styles/main.css`;
- keep all user-visible text in `pt.json` and `en.json`;
- prefer shadcn-vue primitives before custom markup;
- do not overwrite existing behavior while migrating visual structure.

Initial component set:

- `Button`;
- `Card`;
- `Badge`;
- `Dialog`;
- `Sheet`;
- `Input`;
- `Select`;
- `Table`;
- `Tabs`;
- `Alert`;
- `Skeleton`;
- `Separator`;
- toast via `vue-sonner`.

Rules:

- use `CardHeader`, `CardTitle`, `CardDescription`, `CardContent` and `CardFooter` composition;
- use `Badge` for statuses instead of custom spans;
- use `Alert` for system messages;
- use `Skeleton` for loading states;
- use `Sheet` for quick edit flows where it fits existing UX;
- keep destructive confirmations in dialogs;
- do not use raw hardcoded colors in Vue templates.

## Page-Level Redesign Requirements

### App Shell and Navigation

The shell should become calmer and more spatial. Sidebar remains the primary navigation, but it should feel like an arena console, not a generic SaaS sidebar.

Requirements:

- preserve fixed sidebar on desktop;
- improve collapsed state readability;
- replace symbolic text icons where possible with consistent icons;
- keep focus states visible;
- make support/logout secondary to operational navigation.

### Home

Home should explain the product through the operations desk idea. The hero should not be a generic metrics block. It should show the characteristic workflow: presence, captains, draft, Discord publication.

Primary action remains starting/entering draft operations.

### Drafts

Drafts is the most important screen and should lead the redesign.

Requirements:

- add the draft rail as the central state device;
- separate primary board work from contextual actions;
- make presence, captains and publication states visually scannable;
- keep manual fallback flows clear;
- use reason prompts consistently for administrative actions;
- avoid hiding critical actions behind decorative UI.

### Players and Profile

Players should feel like roster management, not a generic table.

Requirements:

- cards or table rows should foreground display name, rank, route preferences and status;
- route preferences can use restrained lane markers;
- empty states should tell the admin/player what to do next.

### Teams

Teams should feel like lineups.

Requirements:

- team cards should foreground tag, captain and active state;
- destructive actions remain explicit;
- forms should be cleaner and use shadcn field patterns where possible.

### Admin and Settings

Admin/settings pages should be quieter than draft pages.

Requirements:

- use Cards, Tables, Badges and Sheets;
- prioritize clear labels and validation;
- avoid visual theatrics in configuration pages.

### Auth Screens

Login/register should introduce the identity without overwhelming users.

Requirements:

- keep logo and brand present;
- simplify copy;
- reduce visual noise;
- use a single memorable arena-table accent instead of multiple glows.

## Copy Rules

Use product language from the user's side of the screen.

Preferred vocabulary:

- "Confirmar presença";
- "Encerrar presença";
- "Definir capitães";
- "Republicar no Discord";
- "Salvar alterações";
- "Remover presença".

Avoid backend/internal language in the interface:

- webhook config;
- payload;
- endpoint;
- callback;
- mutation.

Errors should explain what happened and what can be done next. Empty states should invite action.

## Accessibility and Responsiveness

- Preserve keyboard navigation and visible focus.
- Respect reduced motion.
- Maintain contrast against dark surfaces.
- Design desktop-first but support mobile down to 320px.
- Dialog, Sheet and Drawer components must include accessible titles.
- Any icon-only control must have an accessible label.

## Motion

Use one deliberate motion idea: the draft rail can softly update when the state changes. Avoid scattered decorative animation.

Motion must be disabled or simplified under `prefers-reduced-motion`.

## Implementation Boundaries

The redesign is full-system, but implementation should be phased to reduce risk:

1. shadcn-vue setup and token bridge;
2. base shell and shared primitives;
3. Drafts screen as the identity anchor;
4. Home/auth screens;
5. Players/Teams/Profile/Admin/Settings;
6. cleanup of legacy CSS classes after screens migrate.

No backend behavior changes are required for the redesign unless a UI flow exposes a missing API contract.

## Testing Requirements

- Existing frontend tests must remain passing.
- Add or update tests for navigation, i18n parity and critical screen actions when markup changes.
- Run `npm test` and `npm run build` in `FrontEnd/` after each meaningful migration phase.
- If shadcn-vue introduces new dependencies, verify build and typecheck.

## Internationalization Requirements

- All new labels, buttons, titles, placeholders, tooltips, empty states, errors and toasts must be added to both `pt.json` and `en.json`.
- Portuguese copy must use correct accents.
- No user-visible copy may be hardcoded in Vue components.

## Implementation Decisions

- Body typography remains `Hanken Grotesk`.
- shadcn-vue should be initialized conservatively and then themed manually through the token bridge; do not apply a preset that overwrites the Mesa de Draft identity.
- Introduce a consistent icon library during shell migration, not during initial shadcn-vue setup. The first setup phase should focus on components, tokens and build stability.

## Approval Notes

User selected: **Mesa de Draft**.

Implementation should not start until the user reviews and approves this written spec.
