# Implementation Plan Phase 1: Research & Analysis

## Overview

Phase 1 focuses on documentation, analysis, and establishing standards across the project. No implementation code is written in this phase; all deliverables are documentation and design artifacts.

## Phase 1 Deliverables

### 1. research.md

**Location**: `specs/005-standards-and-i18n/research.md`

**Contents**:
- Analysis of existing branch naming practices (current vs. expected)
- Review of AGENTS.md and existing docs for standards already defined
- Findings on current message handling (where messages are hardcoded, where centralized)
- Frontend i18n approach review (existing translations if any)
- Backend response structure review (current patterns, compatibility concerns)
- Technology choices for i18n (Vue I18n vs. lightweight alternatives)
- Constants/enums audit (what's centralized, what's duplicated)

**Output**: 15-25 findings, each with decision, rationale, and alternatives considered

### 2. data-model.md

**Location**: `specs/005-standards-and-i18n/data-model.md`

**Contents**:
- **Message Entity**: Fields (code, category, pt-BR text, en-US text, context, severity)
- **Message Category Enum**: Info, Success, Error, Validation, Confirmation, Alert
- **Constants Structure**: How constants/enums should be organized in frontend and backend
- **i18n Configuration**: Locale structure, file format, storage location
- **Branch/Commit Standards**: Documentation structure, not entities but data flows

**Diagrams**: Relationships between message codes, locales, components

### 3. docs/standards/ (Complete Documentation Set)

**Location**: `docs/standards/`

**Files to Create**:
- `README.md` – Overview, navigation to other docs
- `BRANCH_NAMING.md` – Branch convention with examples
- `COMMIT_MESSAGES.md` – Commit message format (Portuguese, semantic prefixes)
- `SPECS_AND_PLANNING.md` – Spec Kit workflow summary
- `PR_STANDARDS.md` – Pull request expectations and review criteria
- `CONSTANTS_AND_ENUMS.md` – When to use enums, constants, types; naming conventions
- `I18N_GUIDELINES.md` – i18n structure, adding translations, locale switching

**Content**: Each doc aligns with AGENTS.md; references existing project conventions; provides actionable examples

### 4. docs/messages/ (Message Catalog Structure)

**Location**: `docs/messages/`

**Files to Create**:
- `README.md` – Introduction to message system
- `message-catalog.md` – Master list of all message codes (50+ initial codes across categories)
- `message-codes.md` – Quick reference by code number and category

**Content**: Each message entry includes code, category, Portuguese text, English text, use context

**Note**: No backend implementation yet; this is the source of truth for Phase 2

### 5. Contracts (Interface Documentation)

**Location**: `specs/005-standards-and-i18n/contracts/`

**Files Created**:
- `message-code-structure.md` – Message code format, ranges, backend response structure
- `branch-naming-contract.md` – Branch naming pattern and examples

**Future Contracts** (Phase 3+):
- `backend-response-contract.md` – Response structure with message codes
- `frontend-message-service-contract.md` – Message service API and behavior

---

## Analysis Focus Areas

### Branch Naming Analysis
- Current practice: `004-layout-jogadores` (should be `feature/004-layout-jogadores`)
- Issue: No `feature/` prefix, inconsistent with AGENTS.md definition
- Decision: Standardize all to `feature/NNN-slug` going forward
- Affected: Update AGENTS.md to clarify; document in standards

### Message Handling Analysis
- Current state: User-facing messages likely hardcoded in components/controllers
- Pain points: Inconsistent messaging, difficult to translate, no message codes
- Solution: Centralize in catalog, assign codes, create service layer
- Phases: Catalog (Phase 1) → Backend (Phase 2) → Frontend (Phase 4)

### Constants Audit Analysis
- Current state: Review where magic strings, hardcoded roles, status values exist
- Examples: App routes, player status, elo ranks, message types
- Locations: Likely in components, services, controllers
- Action: Identify all, document in audit, prioritize for Phase 6

### i18n Readiness Analysis
- Current state: No translation files (UI all Portuguese)
- Options: Vue I18n, custom lightweight solution
- Decision rationale: Vue I18n if minimal setup; custom if lower complexity preferred
- Plan: Decision made in research; implementation in Phase 5

### Backend Response Pattern Analysis
- Current API structure: Standard responses (assuming HTTP 200/400/500)
- Question: Where/how to add message codes? How to store backend message text?
- Options for message text storage: Hardcoded in classes, database, resource files
- Decision: Use native .NET Resource Files (.resx) for backend i18n; codes in MessageCodes.cs constants; text resolved by ResourceMessageProvider
- Implementation: IMessageProvider interface, ResourceMessageProvider using System.Resources, .resx files for each culture
- Benefit: Native .NET i18n, text changes don't require code recompilation, culture-aware lookups

---

## Phase 1 Validation Checklist

- [ ] research.md completed with all clarifications resolved
- [ ] data-model.md documents all entities and structures
- [ ] docs/standards/ fully populated (7 documents minimum)
- [ ] docs/messages/ catalog has 50+ codes across all categories
- [ ] contracts define message codes, branches, and (preliminary) backend/frontend APIs
- [ ] All documentation references AGENTS.md and existing project conventions
- [ ] No implementation code written (no .cs files, no .ts files for features)
- [ ] Documentation is clear enough for a new developer to understand standards
- [ ] Identified inconsistencies (e.g., branch naming, hardcoded strings) documented with solutions

---

## Next Steps After Phase 1

1. **Review & Approval**: Team reviews Phase 1 deliverables
2. **Feedback Loop**: Clarifications addressed, documentation updated if needed
3. **Proceed to Phase 2**: Generate tasks for backend infrastructure (MessageCodes.cs, .resx files, IMessageProvider, ResourceMessageProvider)
4. **Iterative Phases**: Each phase similarly documented, reviewed, and executed in sequence
