# Implementation Plan: Standardization and Internationalization Framework

**Branch**: `feature/005-standards-and-i18n` | **Date**: 2026-06-10 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/005-standards-and-i18n/spec.md`

## Summary

Establish a foundational standardization framework for RinhaDasLendas covering branch naming conventions, centralized messaging with codes, internationalization infrastructure, and constants/types centralization. The feature is decomposed into 6 implementation phases prioritized by dependency: Phase 1 (Documentation/Analysis), Phase 2 (Message Catalog), Phase 3 (Backend Infrastructure), Phase 4 (Frontend Service), Phase 5 (Frontend i18n), Phase 6 (Governance). Each phase is independently deliverable and testable.

## Technical Context

**Language/Version**: Backend: .NET 10 / ASP.NET Core; Frontend: Vue 3 + TypeScript

**Primary Dependencies**: Backend: Entity Framework Core, FluentValidation, MediatR; Frontend: Vue I18n (for Phase 5), JSON i18n files

**Storage**: PostgreSQL (no new migrations required in Phase 1-2; Phase 3 may add message catalog table)

**Testing**: Backend: xUnit, FluentAssertions, Moq; Frontend: Vitest (existing setup)

**Target Platform**: Web (browser + server)

**Project Type**: Web application (Vue 3 frontend + ASP.NET Core backend)

**Performance Goals**: Messaging lookups should be O(1) in-memory; no API performance regression

**Constraints**: i18n must not add >100KB bundle size; message catalog must be maintainable without code changes

**Scale/Scope**: Initial support for 2 locales (pt-BR, en-US); extensible to additional languages; ~50-100 initial message codes across all categories

## Constitution Check

*GATE: Must pass before Phase 1 implementation. Re-check after Phase 1 design.*

### Compliance Assessment

**MVP Principles**:
- ✅ Avoids overengineering (starts simple, supports evolution)
- ✅ Maintains manual fallbacks (message codes map to text, no external dependency required)
- ✅ Supports internal use (standards/messaging/i18n improve clarity for group)

**Architecture Principles**:
- ✅ Backend separation: message codes in domain/infrastructure, not controllers
- ✅ Frontend separation: message service layer, translation keys in i18n files
- ✅ No breaking changes: existing APIs remain functional; new structures optional

**Quality Principles**:
- ✅ Enables better testing (constants instead of magic strings)
- ✅ Improves maintainability (centralized standards)
- ✅ Supports validation (branch naming, commits, code quality)

**Governance Principles**:
- ✅ Aligns with AGENTS.md (documents workflows, branch naming, commit patterns)
- ✅ Establishes process for future features (Phases 6 creates checklist)

**Status**: PASS ✅ | No violations. Feature fully compliant with constitution.

## Project Structure

### Documentation (this feature)

```text
specs/005-standards-and-i18n/
├── spec.md                      # Feature specification
├── plan.md                       # This file
├── research.md                  # Phase 1 output (analysis findings)
├── data-model.md                # Phase 1 output (standards structure)
├── quickstart.md                # Phase 1 output (validation guide)
├── checklists/
│   └── requirements.md
├── contracts/
│   ├── message-code-structure.md     # Phase 1 output
│   ├── branch-naming-contract.md     # Phase 1 output
│   ├── backend-response-contract.md  # Phase 3 output
│   └── frontend-message-service-contract.md  # Phase 4 output
└── tasks.md                     # Phase 2 output (/speckit-tasks command)
```

### Source Code (repository root)

```text
docs/
├── standards/                   # Phase 1 output
│   ├── README.md                # Overview of all standards
│   ├── BRANCH_NAMING.md         # Branch naming convention
│   ├── COMMIT_MESSAGES.md       # Commit message format
│   ├── SPECS_AND_PLANNING.md    # Spec kit workflow
│   ├── PR_STANDARDS.md          # Pull request guidelines
│   ├── CONSTANTS_AND_ENUMS.md   # Constants/types guide
│   └── FEATURE_CHECKLIST.md     # Phase 6 output
├── messages/                    # Phase 2 output
│   ├── README.md
│   ├── message-catalog.md       # Master message list
│   └── message-codes.md         # Code reference

BackEnd/src/RinhaDasLendas.Domain/
├── Constants/                   # Phase 2 output
│   ├── MessageCodes.cs          # Message code constants (strings: "MSIS001", "ME001", etc.)
│   └── ValidationConstants.cs
├── Enums/                       # Phase 2 output
│   ├── MessageCategory.cs
│   └── [existing enums]

BackEnd/src/RinhaDasLendas.Application/
├── Dtos/
│   └── MessageResponseDto.cs    # Phase 2 output
├── Interfaces/
│   └── IMessageProvider.cs      # Phase 2 output

BackEnd/src/RinhaDasLendas.Infrastructure/
├── Messages/                    # Phase 2 output
│   ├── Messages.resx            # Default (pt-BR) resource file
│   ├── Messages.pt-BR.resx      # Portuguese (Brazilian) resource file
│   ├── Messages.en-US.resx      # English (US) resource file
│   └── ResourceMessageProvider.cs  # Phase 2 output

FrontEnd/src/
├── constants/                   # Phase 6 output
│   ├── appRoutes.ts
│   ├── playerStatus.ts
│   ├── leagueRoles.ts
│   └── messageCode.ts
├── services/
│   └── messageService.ts        # Phase 4 output
├── i18n/                        # Phase 5 output
│   ├── index.ts
│   ├── locales/
│   │   ├── pt-BR.json
│   │   └── en-US.json
├── components/
│   └── [updated to use i18n keys]
└── views/
    └── [updated to use i18n keys]

tests/
├── backend/
│   └── MessageProviderTests.cs  # Phase 3 output
└── frontend/
    └── messageService.spec.ts   # Phase 4 output
```

**Structure Decision**: Distributed documentation in `docs/standards/` and `docs/messages/` for clarity; constants/enums in domain and frontend services respectively; i18n files co-located with Vue components for maintainability.

---

## Phase Breakdown

### Phase 1: Documentation, Analysis & Standards Definition

**Goal**: Establish clear documentation of standards, identify inconsistencies, and design message/constant structures without implementation.

**Deliverables**:
- `research.md`: Analysis findings
- `data-model.md`: Standards reference architecture
- `docs/standards/`: Complete standards documentation (branch naming, commits, specs, PRs, constants, i18n process)
- `docs/messages/message-catalog.md`: Initial message code structure (no backend storage yet)
- `contracts/`: Interface documentation

**Testing**: Manual review; no code changes; no build validation.

**Estimated Scope**: 8-10 tasks (analysis, documentation creation, alignment review)

---

### Phase 2: Message Catalog & Backend Response Infrastructure

**Goal**: Create backend foundation for message codes, Resource Files for localization, and standardized response structure.

**Depends on**: Phase 1 (documentation defines codes)

**Deliverables**:
- Backend `MessageCodes.cs` (code constants only: `MSIS001`, `ME001`, etc.)
- Backend `MessageCategory.cs` enum
- Backend `Messages.resx`, `Messages.pt-BR.resx`, `Messages.en-US.resx` (resource files with message text)
- Backend `IMessageProvider` interface (abstraction for message retrieval)
- Backend `ResourceMessageProvider` implementation (uses .resx files based on current culture)
- Backend `MessageResponseDto` structure
- Backend infrastructure for message responses (no API endpoint refactoring yet)
- Tests for ResourceMessageProvider (culture-aware lookups)

**Key Design Points**:
- Message codes in `MessageCodes.cs` are immutable constants (e.g., `public const string OperationSuccess = "MSIS001"`)
- Message text resides in `.resx` files, not hardcoded in classes
- `ResourceMessageProvider` resolves codes to localized text using `System.Resources` and `CultureInfo`
- No large classes containing message text constants
- Supports backend i18n natively; text changes don't require code recompilation

**Testing**: xUnit tests for message lookups with different cultures; ResourceMessageProvider behavior validation; no API integration changes.

**Estimated Scope**: 8-10 tasks (MessageCodes class, .resx files, IMessageProvider interface, ResourceMessageProvider implementation, tests)

---

### Phase 3: Frontend Message Service & Constants

**Goal**: Create frontend layer for message retrieval and centralize constants/types.

**Depends on**: Phase 2 (backend message codes defined)

**Deliverables**:
- Frontend `messageService.ts`: `getMessage(code, locale?)` function
- Frontend constants: `AppRoutes`, `PlayerStatus`, `LeagueRoles`, `MessageCode`, `MessageType`
- Frontend types: `LeagueRank` union type, `LocaleCode` type
- Services updated to use constants instead of magic strings
- Tests for message service

**Testing**: Vitest unit tests; sidebar/topbar manual validation (no i18n yet, just constants).

**Estimated Scope**: 7-9 tasks (service layer, constants, types, tests, component updates)

---

### Phase 4: Internationalization Infrastructure (Frontend)

**Goal**: Implement i18n for pt-BR and en-US with translation files.

**Depends on**: Phase 3 (message service and constants in place)

**Deliverables**:
- i18n configuration and setup (Vue I18n or lightweight alternative)
- Translation files: `locales/pt-BR.json`, `locales/en-US.json`
- Sidebar translated (menu labels, items)
- Topbar translated (header text, user menu)
- Player page translated (form labels, buttons, messages)
- Locale switcher component (optional visual indicator)

**Testing**: Manual verification of translations; lint and build validation.

**Estimated Scope**: 8-10 tasks (i18n setup, translation files, component updates, locale switcher)

---

### Phase 5: Backend Message Response Adoption (Player Endpoints)

**Goal**: Integrate message codes into existing player endpoints as optional response structure.

**Depends on**: Phase 2 (backend infrastructure), Phase 1 (docs)

**Deliverables**:
- Player API endpoints return optional `messageCode` field in response headers or response envelope
- Example endpoints: Get Player, Create Player, Update Player (select critical endpoints)
- Backward compatibility: existing response format preserved; message codes added alongside
- Documentation updated in `docs/messages/` linking codes to endpoints

**Testing**: xUnit integration tests; no breaking changes to existing clients.

**Estimated Scope**: 6-8 tasks (endpoint updates, integration tests, docs)

---

### Phase 6: Governance & Feature Development Checklist

**Goal**: Document required standards for future features to ensure compliance.

**Depends on**: Phases 1-5 (all standards established)

**Deliverables**:
- `docs/standards/FEATURE_CHECKLIST.md`: Pre-implementation checklist for new features
- `docs/standards/` updated with examples from Phases 1-5
- Optional: CI/CD hook documentation (branch validation, commit message validation)

**Testing**: Checklist review; no code changes.

**Estimated Scope**: 3-4 tasks (checklist creation, documentation finalization)

---

## Complexity Tracking

| Aspect | Justification | Simpler Alternative Rejected Because |
|--------|---------------|-------------------------------------|
| Phased approach (6 phases) | Each phase is independently deliverable; reduces risk; allows feedback; separates concerns (backend infrastructure ≠ frontend i18n) | Monolithic implementation would be harder to review, test incrementally, and parallelize work across team |
| Separate frontend/backend phases | Frontend i18n (Phase 4) depends on message service (Phase 3), which depends on backend infrastructure (Phase 2) | Mixing would create circular dependencies and make testing harder |
| Message catalog in docs (Phase 1) | Allows design review before backend storage; enables collaborative editing; decouples documentation from code | Storing directly in code (constants) skips design discussion and makes catalog less discoverable |
| Optional message codes in responses (Phase 5) | Maintains backward compatibility; non-breaking for existing clients; allows gradual adoption | Forced refactoring of all endpoints would introduce breaking changes and risk |

---

## Phase Execution Flow

```
Phase 1 (Documentation)
    ↓
    ├─→ research.md
    ├─→ data-model.md
    ├─→ docs/standards/*
    ├─→ docs/messages/
    └─→ contracts/*
    
Phase 2 (Backend Messages)
    ↓ (depends on Phase 1)
    ├─→ MessageCodes.cs (code constants)
    ├─→ MessageCategory enum
    ├─→ Messages.resx files (pt-BR, en-US)
    ├─→ IMessageProvider interface
    ├─→ ResourceMessageProvider implementation
    ├─→ MessageResponseDto
    └─→ Tests (culture-aware lookups)
    
Phase 3 (Frontend Services & Constants)
    ↓ (depends on Phase 2)
    ├─→ messageService.ts
    ├─→ Frontend constants
    ├─→ Frontend types
    └─→ Component updates (no i18n yet)
    
Phase 4 (Frontend i18n)
    ↓ (depends on Phase 3)
    ├─→ i18n setup
    ├─→ Translation files
    ├─→ Sidebar/topbar/player page translated
    └─→ Build validation
    
Phase 5 (Backend Adoption)
    ↓ (depends on Phase 2)
    ├─→ Player endpoint updates
    ├─→ Integration tests
    └─→ Documentation
    
Phase 6 (Governance)
    ↓ (depends on Phases 1-5)
    ├─→ Feature checklist
    └─→ Final documentation
```

**Parallel Opportunities**: Phase 5 (backend) can proceed in parallel with Phase 4 (frontend i18n) after Phase 3 completes, since they have separate concerns.

---

## Validation Criteria

By end of all phases:
- ✅ `docs/standards/` contains complete, aligned documentation
- ✅ Message catalog has 50+ codes covering Info, Success, Error, Validation, Confirmation, Alert
- ✅ Frontend sidebar/topbar/player page use i18n keys (no hardcoded text)
- ✅ Backend message provider supports lookup by code
- ✅ Frontend message service supports lookup by code and locale
- ✅ No breaking changes to existing APIs
- ✅ `npm run lint` passes
- ✅ `npm run build` passes
- ✅ `dotnet build` passes
- ✅ All tests pass

---

## Next Steps

1. **Proceed to Phase 1 Implementation**: After approval of this plan, run `/speckit-tasks` to generate granular tasks for Phase 1
2. **Phase 1 Tasks**: Focus on analysis, documentation, and design (no code changes)
3. **Iterative Phases**: Upon Phase 1 completion, request new task generation for Phase 2, and so on
4. **Feedback Loop**: Each phase deliverable is reviewable; adjustments can be made before proceeding to next phase
