# Research: Cadastro de Jogadores e Preferências de Rotas

**Feature**: Cadastro de Jogadores e Preferências de Rotas
**Created**: 2026-06-06

## Decisions

1. Runtime and .NET SDK
   - Decision: Use .NET 10 SDK (project backend standards require .NET 10).
   - Rationale: Repository's backend standards mandate .NET 10; choose the
     supported SDK to ensure compatibility with existing solution files and
     downstream tooling.

2. ORM and Persistence
  - Decision: Use Entity Framework Core with PostgreSQL.
   - Rationale: EF Core integrates well with .NET, supports migrations and unique
     indices required for preference constraints.

3. Validation
  - Decision: Use FluentValidation for application-level validation.
   - Rationale: Clean separation from controllers, testable rules, expressive API.

4. Preference modeling
   - Decision: Model preferences as a separate `PreferenciaRota` entity with
     `Prioridade` integer 1..5 and unique indexes for (JogadorId, Rota) and
     (JogadorId, Prioridade).
   - Rationale: Relational model with constraints provides stronger integrity
     guarantees and simpler queries for drafting/balancing later.

5. Routes and APIs
   - Decision: Expose REST endpoints under `/api/v1/jogadores` for CRUD and
     preference management, following API_STANDARDS.md (versioning, DTOs,
     error format, pagination for lists).
   - Rationale: Aligns with API versioning and DTO requirements in project
     standards.
   - Rationale: Matches repository guidelines and frontend expectations.

6. Dev environment
  - Decision: Use Dev Container + Docker Compose for PostgreSQL.
   - Rationale: Matches constitution requirement for reproducible development
     without host installs.

## Alternatives Considered

- Storing preferences as JSON array on `Jogador` table: rejected because
  enforcing uniqueness and indexing per-priority is harder and error-prone.

- Using an external player directory (Discord-only) during MVP: rejected because
  requirement is to work without Discord integration.

## Action Items

- Implement EF Core migration for `jogadores` and `preferencias_rotas` with
  snake_case naming and UUID PKs.
- Add FluentValidation rules for player and preference DTOs.
- Add MediatR command/query handlers and wire dependency injection.
- Add unit tests covering domain rules.

