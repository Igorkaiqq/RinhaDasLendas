# Quickstart: Integração Usuário-Jogador

## Prerequisites

- Devcontainer image available.
- Docker available.
- Backend and frontend dependencies restorable inside containers.
- PostgreSQL available through the existing devcontainer compose setup.

## Validation Commands

### Backend

```bash
docker compose -f ".devcontainer/docker-compose.yml" run --rm --no-deps app dotnet build "BackEnd/RinhaDasLendas.sln"
docker compose -f ".devcontainer/docker-compose.yml" run --rm --no-deps app dotnet test "BackEnd/RinhaDasLendas.sln"
```

### Frontend

```bash
docker run --rm -v "$PWD:/workspaces/RinhaDasLendas" -w /workspaces/RinhaDasLendas/FrontEnd node:24 npm run lint
docker run --rm -v "$PWD:/workspaces/RinhaDasLendas" -w /workspaces/RinhaDasLendas/FrontEnd node:24 npm run build
docker run --rm -v "$PWD:/workspaces/RinhaDasLendas" -w /workspaces/RinhaDasLendas/FrontEnd node:24 npm run test
```

## Manual Scenario 1: New User Completes Player Profile

1. Register a new user through the public registration form.
2. Login with the new user.
3. Confirm the platform shows a pending player profile state or CTA.
4. Open the complete player profile flow.
5. Fill all required fields:
   - Nome de exibição
   - Discord
   - Riot ID
   - OP.GG URL
   - DeepLOL URL
   - Elo
   - Divisão
   - Five route preferences with unique priorities
   - At most one “não jogo nem lascando” marker
6. Submit.
7. Confirm the profile is completed and the authenticated user now has `jogadorId`.
8. Open the player list and confirm the new player appears as active.

Expected result: new user can become an active linked player without administrator intervention.

## Manual Scenario 2: Incomplete User Is Excluded From Draft

1. Register a new user and login.
2. Do not complete the player profile.
3. Open the player list.
4. Open draft creation or draft player selection.

Expected result: the user account does not appear as a player until the profile is completed.

## Manual Scenario 3: Invalid Route Preferences Are Rejected

1. Login as a user without player profile.
2. Open the complete player profile flow.
3. Submit route preferences with duplicated priorities or fewer than five routes.

Expected result: the system rejects the submission and shows clear validation errors.

## Manual Scenario 4: User Updates Own Player Profile

1. Login as a user with completed linked player profile.
2. Open the own player profile editor.
3. Change elo, links and route preferences.
4. Submit.
5. Reopen the player list and draft setup.

Expected result: updated values are visible and used by player/draft flows.

## Manual Scenario 5: Admin Sees Link Status

1. Login as SuperAdmin.
2. Open the user administration page.
3. Compare a user with player profile and one without player profile.

Expected result: the list/details clearly show whether each user has completed linked player profile.

## Notes

- No Discord OAuth or Riot API integration is expected for this feature.
- Existing legacy players without user accounts must remain visible and manageable.
