# Quickstart: Draft em Tempo Real na Montagem de Times

## Prerequisites

- Backend and frontend dependencies installed.
- PostgreSQL available through the project environment.
- At least one admin/moderator user.
- At least two users with linked active player profiles and captain role.
- Enough active players to create a draft montagem with at least two teams and at least one reserve.

## Validation Flow

### 1. Run automated checks

From `BackEnd/`:

```bash
dotnet test
```

From `FrontEnd/`:

```bash
npm run build
npm run test
```

### 2. Create a montagem with reserves

1. Open the drafts page.
2. Create a visual draft montagem with captains and participants.
3. Confirm that at least one participant is listed as reserve.

Expected outcome:

- Montagem is open.
- Captains appear in teams.
- Free players and reserves are visually separate.

### 3. Start realtime draft

1. As organizer, start realtime mode for the selected montagem.
2. Open the same montagem in another browser/session.

Expected outcome:

- Both sessions show the same captain of the turn.
- Both sessions show a 30-second backend-controlled turn.
- Drag and drop editing is disabled for realtime draft flow.

### 4. Register a valid pick

1. As the captain of the turn, select a free player.

Expected outcome:

- Player moves to the captain's team.
- Player disappears from free pool.
- Pick appears in history.
- Other sessions update without manual refresh.
- Next captain receives the turn.

### 5. Try invalid picks

1. As a captain outside the current turn, try to choose a free player.
2. As any captain, try to choose a reserve.
3. Wait for the turn to expire, then try to submit a pick from the expired turn.

Expected outcome:

- Every invalid attempt is rejected by the backend.
- Teams, current turn and history remain unchanged for rejected attempts.
- User-facing messages are localized.

### 6. Validate timeout

1. Let a captain turn expire without choosing.

Expected outcome:

- After 30 seconds, plus small operational tolerance, the turn advances automatically.
- Timeout appears in the draft history.
- All connected sessions receive updated state.

### 7. Validate emergency reserve substitution

1. As organizer, replace a player currently in a team with a reserve.

Expected outcome:

- Reserve enters the target team.
- Replaced player is no longer active in that slot.
- Substitution appears in history/audit data.
- Connected sessions receive updated state.

### 8. Internationalization audit

Expected outcome:

- No new frontend hardcoded user-visible text exists.
- Portuguese and English locale files contain the same new keys.
- Backend messages exist in default, pt-BR and en-US resources.
- Validation messages use message codes/resources.
