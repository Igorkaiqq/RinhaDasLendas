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


## Validacao final da implementacao

### Backend

```bash
cd /workspaces/RinhaDasLendas
 dotnet build BackEnd/RinhaDasLendas.sln
 dotnet test BackEnd/RinhaDasLendas.sln --no-build
```

Resultados capturados em 2026-06-06:

- Build backend: aprovado.
- Testes backend: 12 aprovados, 0 falhas, 0 ignorados.
- Observacao: o build emite warning MSB3277 por conflito transitivo entre `Microsoft.EntityFrameworkCore.Relational` 10.0.4 e 10.0.8.

### Frontend

```bash
cd /workspaces/RinhaDasLendas/FrontEnd
npm install
npm run build
```

### Fluxo manual

1. Subir PostgreSQL conforme configuracao do Dev Container.
2. Aplicar migrations do projeto Infrastructure.
3. Iniciar a API e acessar Swagger em ambiente de desenvolvimento.
4. Abrir o frontend e acessar `/jogadores`.
5. Cadastrar um jogador com nome, elo, Riot ID e cinco preferencias validas.
6. Confirmar que o jogador aparece como `Ativo` na lista.
7. Editar a ordem de rotas e marcar uma unica rota como bloqueada.
8. Tentar duplicar prioridades e confirmar que a tela e a API retornam erro claro.
9. Inativar o jogador e confirmar que ele continua consultavel por ID, mas sai da listagem com `somenteAtivos=true`.

Resultados frontend capturados em 2026-06-06:

- `npm install`: aprovado, 201 pacotes instalados, 0 vulnerabilidades.
- `npm run lint`: aprovado.
- `npm run build`: aprovado com `vue-tsc -b` e `vite build`.
