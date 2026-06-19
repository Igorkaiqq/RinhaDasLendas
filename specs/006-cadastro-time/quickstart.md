# Quickstart: Cadastro de Time

## Prerequisites

- Backend dependencies restored.
- Frontend dependencies installed.
- PostgreSQL available through the existing local/devcontainer setup.
- At least one active player exists for team creation scenarios.

## Validation Commands

```bash
dotnet build BackEnd/RinhaDasLendas.sln
dotnet test BackEnd/tests/RinhaDasLendas.Tests/RinhaDasLendas.Tests.csproj
npm run lint --prefix FrontEnd
npm run build --prefix FrontEnd
```

## Backend Validation Scenarios

### Create a valid team

1. Ensure active players exist.
2. Send `POST /api/v1/times` with name, tag and 1 to 5 player ids.
3. Expect `201 Created`.
4. Confirm the response contains status `Ativo`, member count and captain when provided.

### Reject duplicate player in team

1. Send `POST /api/v1/times` with the same player id repeated.
2. Expect `400 Bad Request` with a clear validation message.

### Reject duplicate active name or tag

1. Create an active team.
2. Create another active team with same normalized name or tag.
3. Expect `409 Conflict`.

### Edit composition and captain

1. Send `PUT /api/v1/times/{id}` with a different member list and captain id.
2. Expect `200 OK`.
3. Confirm captain belongs to returned members.

### Inactivate and reactivate

1. Send `PATCH /api/v1/times/{id}/inativar`.
2. Expect status `Inativo`.
3. Send `PATCH /api/v1/times/{id}/reativar`.
4. Expect status `Ativo` when required data remains valid.

### List and filter

1. Send `GET /api/v1/times?page=1&pageSize=20`.
2. Confirm paginated response shape.
3. Send `GET /api/v1/times?search=<nome-ou-tag-ou-jogador>`.
4. Confirm only matching teams are returned.
5. Send `GET /api/v1/times?status=Ativo` and `status=Inativo`.
6. Confirm status filtering.

## Frontend Validation Scenarios

### Empty state

1. Open `/times` with no teams returned.
2. Confirm the page shows an empty state and action to create the first team.

### Create team flow

1. Open `/times`.
2. Click create team.
3. Fill name, tag and select active players.
4. Submit.
5. Confirm the new team appears in the list with active status.

### Prevent invalid local selections

1. Try selecting the same player twice.
2. Confirm the UI prevents duplication.
3. Try selecting more than five members.
4. Confirm the UI prevents the extra selection.

### Search and filter

1. Type a team name, tag or member name in the search input.
2. Confirm the list updates according to backend results.
3. Change status filter.
4. Confirm only teams with selected status are shown.

### i18n

1. Switch locale between `pt-BR` and `en-US`.
2. Confirm Teams navigation, page title, form labels, filters and validation messages use translation keys.

## Expected Result

The feature is considered implemented when all validation commands pass and the scenarios above work without relying on Discord, Riot API or manual database edits.

## Validation Notes

- 2026-06-19: Validated inside the devcontainer with `dotnet build BackEnd/RinhaDasLendas.sln`.
- 2026-06-19: Validated inside the devcontainer with `dotnet test BackEnd/tests/RinhaDasLendas.Tests/RinhaDasLendas.Tests.csproj`.
- 2026-06-19: Validated inside the devcontainer with `npm run lint --prefix FrontEnd`.
- 2026-06-19: Validated inside the devcontainer with `npm run build --prefix FrontEnd`.
- 2026-06-19: Validated inside the devcontainer with `npm run test --prefix FrontEnd`.
- 2026-06-19: Teams UI reviewed against `docs/design/DESIGN_SYSTEM.md`, `docs/design/DESIGN_TOKENS.md` and `docs/design/UI_GUIDELINES.md`: dark-first card list, status badges, skeleton loading, empty state with CTA, danger styling for inactivation and responsive card behavior follow the documented guidance and existing app patterns.
