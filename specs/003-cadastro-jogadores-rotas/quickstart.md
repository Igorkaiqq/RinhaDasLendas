# Quickstart: Validate Cadastro de Jogadores (Dev Container)

**Feature**: Cadastro de Jogadores e Preferências de Rotas
**Created**: 2026-06-06

## Prerequisites

- Docker and Dev Container support (configured by repository `.devcontainer`)
- Dev Container running (open workspace in container)

## Start Postgres (docker-compose)

From repository root:

```bash
# in dev container or host with Docker
docker compose -f .devcontainer/docker-compose.yml up -d postgres
```

(If repository lacks docker-compose, create a simple `docker-compose.yml` with a
PostgreSQL service exposed on default port for local development.)

## Run Backend (dev)

From repository root inside the Dev Container:

```bash
cd BackEnd/src/RinhaDasLendas.Api
# restore and run (example)
dotnet restore
dotnet ef database update --project ../RinhaDasLendas.Infrastructure --startup-project RinhaDasLendas.Api
dotnet run --project RinhaDasLendas.Api
```

## Validate End-to-end (manual)

1. Create a player via POST `/api/v1/jogadores` with 5 route preferences.
2. GET `/api/v1/jogadores` and confirm created player appears with preferences.
3. PUT `/api/v1/jogadores/{id}` to update fields and verify changes.
4. PATCH `/api/v1/jogadores/{id}/inativar` and verify player is marked inactive and
   excluded from active queries.

## Notes

- If migrations are not yet implemented, validate using a lightweight in-memory
  provider for EF Core until migrations are available.
- Keep sensitive connection strings in `.env` or container secrets and do not
  commit them.
 
## API Documentation

- Ensure Swagger/OpenAPI is available in development environment (per API
  standards) to validate request/response samples and DTO schemas.

