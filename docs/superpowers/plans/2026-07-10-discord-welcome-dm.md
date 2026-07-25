# Discord Welcome DM Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enviar DM privada de boas-vindas com CTA para o site e mini tutorial quando uma pessoa entrar no servidor Discord.

**Architecture:** O bot escuta `guildMemberAdd` com `GuildMembers` intent, monta uma mensagem localizada e envia DM para o novo membro. Falhas de DM são logadas e não interrompem o bot.

**Tech Stack:** Node.js 24, TypeScript, discord.js, node:test, tsx.

## Global Constraints

- Nao fazer commit nesta rodada.
- Nao alterar segredos nem arquivos `.env` reais.
- Textos do bot devem ficar em `discord-bot/src/shared/messages/pt-BR.ts` e `discord-bot/src/shared/messages/en-US.ts`.
- Documentar nova variavel em `.env.example` e `docker-stack.prod.yml`.

---

### Task 1: Welcome DM Handler

**Files:**
- Create: `discord-bot/src/modules/welcome/welcomeInteractions.ts`
- Test: `discord-bot/src/modules/welcome/welcomeInteractions.spec.ts`
- Modify: `discord-bot/src/shared/messages/pt-BR.ts`
- Modify: `discord-bot/src/shared/messages/en-US.ts`

**Interfaces:**
- Produces: `buildWelcomeMessage(siteUrl: string): string`
- Produces: `handleGuildMemberAdd(member, siteUrl): Promise<void>`

- [ ] Write failing test that asserts CTA URL and tutorial steps are present.
- [ ] Implement message builder and DM sender.
- [ ] Run `cd discord-bot && npm test`.

### Task 2: Bot Wiring And Config

**Files:**
- Modify: `discord-bot/src/main.ts`
- Modify: `discord-bot/src/config/env.ts`
- Modify: `discord-bot/.env.example`
- Modify: `docker-stack.prod.yml`

**Interfaces:**
- Consumes: `handleGuildMemberAdd` from Task 1.
- Produces: bot configured with `GuildMembers` intent and `FRONTEND_PUBLIC_URL`.

- [ ] Add `FRONTEND_PUBLIC_URL` to env schema.
- [ ] Add `GatewayIntentBits.GuildMembers`.
- [ ] Register `client.on('guildMemberAdd', ...)` with safe logging.
- [ ] Run `cd discord-bot && npm run build`.
