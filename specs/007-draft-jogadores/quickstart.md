# Quickstart: Draft de Jogadores

## Prerequisites

- Dev Container aberto.
- PostgreSQL configurado conforme projeto.
- Jogadores ativos cadastrados.

## Backend Validation

1. Apply migrations:

   ```bash
   dotnet ef database update --project BackEnd/src/RinhaDasLendas.Infrastructure --startup-project BackEnd/src/RinhaDasLendas.Api
   ```

2. Run tests:

   ```bash
   dotnet test BackEnd/tests/RinhaDasLendas.Tests/RinhaDasLendas.Tests.csproj
   ```

3. Run API:

   ```bash
   dotnet run --project BackEnd/src/RinhaDasLendas.Api/RinhaDasLendas.Api.csproj
   ```

4. Validate manually:

   - Create at least four active players.
   - `POST /api/v1/drafts` with two manual captains and eligible players.
   - `POST /api/v1/drafts/{id}/picks` for the next available player.
   - Confirm duplicate pick returns conflict.
   - Continue picks until status becomes `Concluido`.

## Frontend Validation

1. Install dependencies if needed:

   ```bash
   npm install --prefix FrontEnd
   ```

2. Run tests and build:

   ```bash
   npm run test:unit --prefix FrontEnd
   npm run build --prefix FrontEnd
   ```

3. Run app:

   ```bash
   npm run dev --prefix FrontEnd
   ```

4. Open `/drafts` and validate:

   - Empty state appears when no drafts exist.
   - Create draft form lists active players.
   - Active draft board shows captains, next pick, available players and pick history.
   - Picking a player updates both teams and the timeline without page reload.
