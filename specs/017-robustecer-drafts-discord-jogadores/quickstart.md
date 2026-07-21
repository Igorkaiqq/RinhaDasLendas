# Quickstart: Robustecer Drafts, Discord e Jogadores

## Prerequisites

- Devcontainer or equivalent local stack running backend, frontend, database and discord-bot dependencies.
- Test user with draft management permission.
- Test player account with linked Discord identity.
- Discord bot environment configured for a test guild/channel.

## Validation Scenarios

### 1. Discord CTA opens correct draft

1. Create a draft and publish/obtain the Discord CTA link.
2. Open the link in the browser.
3. Expected: draft page selects the draft from the link.
4. Open the page with an invalid draft identifier.
5. Expected: localized not-found/inaccessible message appears and draft list remains usable.

### 2. Bot disabled state

1. Disable Discord integration in configuration.
2. Run a bot command that would create or publish a draft.
3. Expected: no draft publication side effect occurs; bot replies with integration unavailable guidance.
4. Re-enable integration and repeat.
5. Expected: normal flow resumes.

### 3. Bot date and error handling

1. Attempt to create a draft with a past explicit date/time.
2. Expected: bot rejects it with future-date guidance.
3. Simulate a known backend business failure.
4. Expected: bot uses a specific localized message rather than generic failure.

### 4. Publication dedupe and republish

1. Publish a presence list.
2. Restart the bot.
3. Expected: no duplicate publication appears during polling.
4. Mark/remove the publication as needing republish.
5. Trigger republish as administrator.
6. Expected: new publication state is visible and recorded.

### 5. Presence consistency and realtime

1. Open the same draft in two browser sessions.
2. Confirm/cancel presence in one session or through Discord.
3. Expected: the other session updates within 5 seconds.
4. Attempt simultaneous duplicate confirmations for the same player.
5. Expected: only one confirmed presence remains.

### 6. Manual presence search

1. Open a draft as administrator.
2. Search for an active eligible player.
3. Expected: eligible players appear without loading all players into the client.
4. Add a player to presence.
5. Expected: the player disappears from eligible results and appears in confirmed presence.

### 7. Administrative audit

1. Cancel a draft with a reason.
2. Remove a presence administratively with a reason when prompted.
3. Expected: responsible user, action, timestamp and reason are visible or retrievable for audit.

### 8. Authorization and production configuration

1. Execute every mutating Discord command as a member without `ManageGuild` or configured admin role.
2. Expected: localized ephemeral denial and no backend mutation.
3. Start the backend in production with an empty, short or placeholder internal token.
4. Expected: startup fails without logging the token.

### 9. Publication claim and reconciliation

1. Request the same publication claim concurrently from two clients.
2. Expected: exactly one claim is acquired.
3. Leave the acquired attempt incomplete until it expires.
4. Expected: state becomes reconciliation required and polling does not resend.

### 10. Security and concurrency matrix

1. Exercise critical endpoints as anonymous, player, administrator, bot and wrong authentication scheme.
2. Expected: 401/403/success follows the documented policy with localized response bodies.
3. Confirm and cancel the same presence concurrently.
4. Expected: no HTTP 500 and one effective final state.
5. Saturate one rate-limit partition.
6. Expected: another user, bot or IP remains unaffected.

## Verification Commands

Run backend tests/build:

```bash
dotnet test BackEnd/RinhaDasLendas.sln --configuration Release
dotnet build BackEnd/RinhaDasLendas.sln --configuration Release
```

Run frontend tests/build:

```bash
cd FrontEnd && npm test
cd FrontEnd && npm run build
```

Run Discord bot tests/build:

```bash
cd discord-bot && npm test
cd discord-bot && npm run build
```
