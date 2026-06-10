# Feature Specification: Standardization and Internationalization Framework

**Feature Branch**: `feature/005-standards-and-i18n`

**Created**: 2026-06-10

**Status**: Draft

**Input**: Etapa 005 - Padronização de Branches, Mensagens e Internacionalização

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Establish Documentation Standards (Priority: P1)

As a developer maintaining the RinhaDasLendas project, I need clear, centralized documentation about branching conventions, commit message standards, specification guidelines, and pull request procedures. This ensures consistency across the team and prevents confusion about project expectations.

**Why this priority**: Without clear standards documentation, the team operates with implicit knowledge, leading to inconsistencies (e.g., current branch `004-layout-jogadores` should be `feature/004-layout-jogadores`). This is foundational and must be established before other standardization efforts.

**Independent Test**: Verify that `docs/standards/` contains up-to-date documentation covering branch naming (`feature/NNN-slug`), commit message format (Portuguese, semantic prefixes), PR expectations, and spec phase requirements. Documentation should be discoverable and actionable.

**Acceptance Scenarios**:

1. **Given** a new developer joins the team, **When** they read `docs/standards/`, **Then** they understand branch naming conventions, commit message format, and the Spec Kit workflow without ambiguity
2. **Given** outdated branch naming practices in the current repo (e.g., `004-layout-jogadores`), **When** documentation is updated, **Then** it clearly specifies the correct pattern with examples
3. **Given** the AGENTS.md guidelines, **When** a developer references the standards docs, **Then** the documentation aligns with and references AGENTS.md

---

### User Story 2 - Implement Message Catalog System (Priority: P1)

As a backend developer, I need a centralized, standardized system for managing all user-facing messages with unique message codes (e.g., `MSIS001`, `ME001`). This allows consistent messaging across the API, prevents duplication, and enables internationalization.

**Why this priority**: Without centralized messages, the system will have inconsistent error handling, duplicated message text, and difficulty translating content later. This is critical infrastructure for quality user experience.

**Independent Test**: Verify that `docs/messages/` contains a complete message catalog with codes, Portuguese text, and message categories (Info, Success, Error, Validation, Confirmation, Alert). Ensure the backend can reference messages by code. Verify no hardcoded user-facing strings exist in the existing player endpoints.

**Acceptance Scenarios**:

1. **Given** a message code like `MSIS001`, **When** a developer references it in documentation, **Then** they find the Portuguese text "Operação realizada com sucesso" and understand the category
2. **Given** an API response for player creation, **When** the response includes a message code field, **Then** the response structure allows code, message text, and optional data
3. **Given** the message catalog, **When** a new feature is planned, **Then** new message codes can be registered following the same pattern

---

### User Story 3 - Create Backend Message Response Infrastructure (Priority: P1)

As a backend developer, I need a reusable response wrapper that includes message codes, Portuguese messages, and optional technical details. This ensures all APIs follow the same contract and support internationalization.

**Why this priority**: This is the backend foundation for standardized messaging. Once implemented, all endpoints can use it consistently. P1 because it enables the internationalization pipeline.

**Independent Test**: Verify that a sample response structure exists with fields: `messageCode` (string), `message` (string), `messageCategory` (string), `data` (object), and optional `details`. Confirm existing player endpoints can optionally adopt this structure without breaking current clients.

**Acceptance Scenarios**:

1. **Given** a player creation request, **When** the operation succeeds, **Then** the API response contains `messageCode: "MSIS001"`, localized message text, and player data
2. **Given** a player creation with invalid input, **When** validation fails, **Then** the response contains `messageCode: "MV001"`, validation message, and field-level error details
3. **Given** an unexpected backend error, **When** an exception occurs, **Then** the response contains `messageCode: "ME001"`, generic message, and optional technical details for logs

---

### User Story 4 - Implement Frontend Message Service Layer (Priority: P2)

As a frontend developer, I need a centralized service that retrieves messages by code and applies translations. This prevents hardcoded text in components and makes the app maintainable and translatable.

**Why this priority**: Depends on backend message catalog being established first (P1). Enables unified message display across UI components.

**Independent Test**: Verify that a message service exists in the frontend with functions like `getMessage(code, locale)` returning message text. Confirm that sidebar, topbar, and player page components use this service instead of hardcoded strings for user-facing feedback.

**Acceptance Scenarios**:

1. **Given** a message code `MI001`, **When** the frontend calls `getMessage('MI001', 'pt-BR')`, **Then** it receives "Carregando informações"
2. **Given** a player form submission, **When** validation fails with code `MV001`, **Then** the error message is retrieved via the service and displayed
3. **Given** a player delete confirmation, **When** the user confirms, **Then** success message `MSIS001` is retrieved and displayed, not hardcoded

---

### User Story 5 - Implement Internationalization Base Structure (Priority: P2)

As a frontend developer, I need i18n infrastructure supporting Portuguese (pt-BR) and English (en-US). The system must separate translatable strings into locale files without extensive hardcoding in components.

**Why this priority**: Follows message service implementation (P2). Enables future language additions and makes the codebase maintainable.

**Independent Test**: Verify that translation files exist for pt-BR and en-US covering: menu labels, button text, route names, page titles, and message catalog entries. Confirm sidebar, topbar, and player page components use translation keys instead of hardcoded text.

**Acceptance Scenarios**:

1. **Given** the sidebar menu, **When** locale is set to `pt-BR`, **Then** menu items display Portuguese labels from translation files
2. **Given** the topbar, **When** locale switches to `en-US`, **Then** topbar text updates to English without page reload
3. **Given** the player page, **When** form labels and buttons are rendered, **Then** they use translation keys from i18n files, not hardcoded strings

---

### User Story 6 - Standardize Constants, Enums, and Types (Priority: P2)

As a developer (frontend or backend), I need centralized, strongly-typed definitions for closed-set values like player status, League roles, message codes, and app routes. This prevents string duplication and magic numbers throughout the codebase.

**Why this priority**: Supports both frontend and backend teams; reduces duplication and improves type safety. Can proceed in parallel with message service setup.

**Independent Test**: Verify that constants/enums exist for: `PlayerStatus`, `LeagueRank`, `LeagueRole`, `MessageCode`, `MessageType`, `AppRoutes` in frontend and equivalent backend enums. Confirm sidebar menu navigation uses centralized route constants, not hardcoded strings.

**Acceptance Scenarios**:

1. **Given** sidebar navigation, **When** a menu item is rendered, **Then** the route is referenced from `AppRoutes` constant, not a hardcoded string
2. **Given** a player status display, **When** status is shown, **Then** it uses `PlayerStatus.ACTIVE` or equivalent, not a magic string like "ativo"
3. **Given** a response with a message code, **When** the frontend processes it, **Then** it's validated against centralized `MessageCode` enum

---

### User Story 7 - Document Feature Development Process (Priority: P3)

As a feature lead, I need clear guidelines for how future features should handle messaging, translations, constants, and standards compliance. This ensures new work follows established patterns.

**Why this priority**: Governance and long-term process documentation. Important but not blocking current implementation.

**Independent Test**: Verify that `docs/standards/` includes a feature development checklist requiring: message catalog updates, translation file updates, constant definitions, documentation updates, and validation runs (lint, build, test).

**Acceptance Scenarios**:

1. **Given** a new feature planned, **When** a developer reviews development standards, **Then** they find a checklist of required steps before implementation
2. **Given** existing standards, **When** a new feature creates new messages, **Then** the process for registering them in the catalog is documented
3. **Given** the i18n system, **When** a new feature adds UI text, **Then** the process for adding translations is clear and automated where possible

---

### Edge Cases

- What happens when a feature is implemented without updating the message catalog? (System should support deprecated unmanaged messages but log them for migration)
- How does the system handle locale switching while a request is in flight? (Frontend should queue updates until current request completes)
- What if a message code is referenced but doesn't exist in the catalog? (System should return a fallback message and log a warning)
- Can locale preference be stored per user or per session? (Initially session-based; user preference can be added later)
- How does the existing player API coexist with the new message-code API during transition? (Both structures supported; new code uses message codes; old clients continue working)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide centralized message catalog in `docs/messages/` with entries for each message code, Portuguese text, message category, and optional English translation
- **FR-002**: Backend API response MUST support optional structure including `messageCode` (string), `message` (string), `messageCategory` (enum), `data` (object), and optional `details`
- **FR-003**: Frontend MUST provide message service supporting `getMessage(code: string, locale?: string)` returning localized message text
- **FR-004**: Frontend MUST implement i18n infrastructure supporting `pt-BR` and `en-US` locales with separate translation files
- **FR-005**: Frontend sidebar, topbar, and player page components MUST use translation keys instead of hardcoded text
- **FR-006**: Frontend MUST centralize app routes, role constants, player status enums, and message code enums avoiding hardcoded strings
- **FR-007**: Documentation MUST specify branch naming convention as `feature/NNN-slug` and commit message format in Portuguese with semantic prefixes
- **FR-008**: Documentation MUST define message code pattern (e.g., `MSIS001`, `ME001`, `MV001`) with category prefix and 3-digit sequence number
- **FR-009**: System MUST implement Phase 1 (documentation), Phase 2 (message catalog), Phase 3 (backend infrastructure), Phase 4 (frontend service), and Phase 5 (i18n) within this feature scope
- **FR-010**: Existing player endpoints and UI MUST remain functional; no breaking changes to current API contracts

### Key Entities

- **Message**: Code (string), Portuguese text (string), English text (optional string), category (MessageCategory enum), created_at (timestamp)
- **MessageCategory**: Enum with values: Info (MI), Success (MSIS), Error (ME), Validation (MV), Confirmation (MC), Alert (MA)
- **StandardsDocument**: Name (string), version (string), last_updated (timestamp), applies_to (array of components)
- **LocaleConfiguration**: Supported locales (pt-BR, en-US), current locale (string), fallback locale (pt-BR)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Documentation in `docs/standards/` covers branching, commits, specs, and PRs; aligns with AGENTS.md; is clear enough for onboarding a new developer
- **SC-002**: Message catalog in `docs/messages/` contains at least 20 message codes covering success, error, validation, and alert scenarios; no duplicates; all codes follow NNN pattern
- **SC-003**: Sidebar, topbar, and player page use centralized translation files for 95% of user-visible text; hardcoded strings reduced by 90% compared to current state
- **SC-004**: Frontend build (npm run build) completes without errors; linting (npm run lint) passes
- **SC-005**: Backend build (dotnet build) completes without errors; all existing tests pass
- **SC-006**: No existing routes or API contracts broken; backward compatibility maintained
- **SC-007**: Checklist in `docs/standards/feature-development.md` clearly documents how future features must handle messages, translations, constants, and validation

## Assumptions

- Internationalization will start with Portuguese (pt-BR) and English (en-US); other languages can be added later
- Message codes follow pattern: 2-letter category prefix + 3-digit number (e.g., MSIS001, ME042)
- Backend response structure is optional during phase-in; existing endpoints don't need immediate refactoring but will support new structure
- Frontend will use Vue 3 composition API for state management and i18n
- The dev container is already configured with Node.js and .NET tooling; no additional tooling installation required
- Player status, roles, and ranks already exist in domain model; constants/enums need to be centralized, not created
- Translation files will be JSON or YAML format; decision documented in i18n guidelines
- Current build and test infrastructure (npm, dotnet, xUnit) can support added validation steps without modification
- Git hooks or CI/CD pipelines will validate commit messages and branch names in future phases (out of scope for spec)
