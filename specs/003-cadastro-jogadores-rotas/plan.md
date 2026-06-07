# Implementation Plan: Cadastro de Jogadores e Preferências de Rotas

**Branch**: `003-cadastro-jogadores-rotas` | **Date**: 2026-06-06 | **Spec**: `specs/003-cadastro-jogadores-rotas/spec.md`

**Input**: Feature specification from `/specs/003-cadastro-jogadores-rotas/spec.md`

## Summary

Implement the player registry and route-preference model that will serve as
the foundation for queues, drafts and matches. Backend implementation targets
.NET 10 (C#) with Entity Framework Core + PostgreSQL; frontend will be Vue 3 +
TypeScript. The plan covers domain modeling, API contracts, migrations,
tests and a minimal frontend to create/list/edit/inactivate players and manage
route preferences.

## Technical Context

**Language/Version**: C# (.NET 10)

**Primary Dependencies**: Entity Framework Core, FluentValidation, MediatR,
Moq, xUnit, FluentAssertions for backend; Vue 3 + TypeScript for frontend.

**Storage**: PostgreSQL (development via Docker Compose in Dev Container).

**Testing**: xUnit for unit tests; integration tests for API endpoints.

**Target Platform**: Dev Container (Ubuntu) for local development; Linux
servers for deployment.

**Project Type**: Web application with separate backend and frontend projects
under `BackEnd/` and `FrontEnd/`.

**Performance Goals**: MVP-level throughput with modest concurrency (no
explicit SLOs defined for this feature).

**Constraints**: Must run in Dev Container; must not depend on Discord or Riot
API for core flows.

**Scale/Scope**: Designed for a small group—hundreds of users at most initially.

## Architecture Patterns

- DDD: Domain layer with entities, value objects and domain rules; domain must
  not depend on EF Core or ASP.NET.
- CQRS: Separate Commands and Queries. Use MediatR for command/query handlers.
- Repository pattern: `IJogadorRepository` interface in Domain/Application and
  implementation in Infrastructure using EF Core.
- DTOs: API MUST use DTOs for requests/responses and not expose domain entities.

## Constitution Check

Gates (evaluated against `.specify/memory/constitution.md`):

- Backend in `BackEnd`: PASS
- Frontend in `FrontEnd`: PASS
- Dev Container requirement: PASS (plan assumes docker-compose for DB)
- Integrations optional for MVP (Discord/Riot): PASS (in-scope is manual)
- Tests for critical rules: PASS (tests defined in plan)

No constitution violations detected.

## Project Structure

### Documentation (this feature)

```text
specs/003-cadastro-jogadores-rotas/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── jogadores-api.md
└── tasks.md   # created by speckit-tasks during implementation phase
```

### Source Code (repository root)

```text
BackEnd/
├── src/
│   ├── RinhaDasLendas.Api/
│   ├── RinhaDasLendas.Application/
│   ├── RinhaDasLendas.Domain/
│   └── RinhaDasLendas.Infrastructure/
└── tests/
    └── RinhaDasLendas.Tests/

FrontEnd/
├── src/
│   ├── components/
│   ├── pages/
│   ├── services/
│   └── types/
└── public/
```

**Structure Decision**: Use existing `BackEnd` and `FrontEnd` roots. Implement
backend projects under `BackEnd/src` following repository conventions.

## Phase 0: Outline & Research

Tasks:
- T0.1: Validate .NET 10 runtime and EF Core compatibility for chosen SDK
- T0.2: Confirm PostgreSQL docker-compose setup and connection strings
- T0.3: Research recommended patterns for modeling ordered preferences and
  apply DB-level constraints (CHECK and UNIQUE indexes)

Deliverable: `research.md` (resolves any remaining technical unknowns)

## Phase 1: Design & Contracts

Prerequisite: `research.md` complete

Tasks:
- T1.1: `data-model.md` (entities: Jogador, PreferenciaRota) with DB naming
  conventions (snake_case) and UUID PKs
- T1.2: `contracts/jogadores-api.md` (API endpoints and payloads) under
  `/api/v1/` per API standards, with DTO examples and error format
- T1.3: `quickstart.md` (run instructions and validation scenarios)
- T1.4: migrations and initial schema defined (to be implemented in code)

Deliverables: `data-model.md`, `/contracts/jogadores-api.md`, `quickstart.md`

## Phase 2: Implementation (overview)

Tasks to be created by `/speckit-tasks` in `tasks.md` (examples):
- Create domain entities and invariants (methods to enforce rules)
- Implement EF Core migrations and repository layer (Infrastructure)
- Implement API controllers and DTOs (Api project)
- Implement FluentValidation rules and unit tests (Application/Tests)
- Implement MediatR command and query handlers (Application)
- Implement minimal Vue forms for create/edit/list/inactivate (FrontEnd) using dark-first
  cards, vertical forms, skeleton loading, responsive layout and empty state CTAs
  aligned with design documentation
- Add integration tests for API endpoints

## Complexity Tracking

No constitution gate violations detected; no additional complexity justifications required.
