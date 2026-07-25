# Discord Draft CTA Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enviar mensagem publica no canal de presenca marcando o cargo configurado e convidando jogadores para abrir o draft no site.

**Architecture:** O bot monta uma mensagem localizada com `<@&DRAFT_NOTIFY_ROLE_ID>` e link `${FRONTEND_PUBLIC_URL}/drafts?draftId=<id>`. A mensagem e enviada logo apos publicar a presença, tanto no comando Discord quanto no polling de drafts criados pelo site.

**Tech Stack:** Node.js 24, TypeScript, discord.js, node:test, tsx.

## Global Constraints

- Nao fazer commit nesta rodada.
- Nao alterar segredos nem arquivos `.env` reais.
- Textos do bot devem ficar em `discord-bot/src/shared/messages/pt-BR.ts` e `discord-bot/src/shared/messages/en-US.ts`.
- Configurar cargo por `DRAFT_NOTIFY_ROLE_ID`.

---

### Task 1: Build CTA Message

**Files:**
- Modify: `discord-bot/src/discord/embeds/draftEmbeds.ts`
- Test: `discord-bot/src/discord/embeds/draftEmbeds.spec.ts`
- Modify: `discord-bot/src/shared/messages/pt-BR.ts`
- Modify: `discord-bot/src/shared/messages/en-US.ts`

**Interfaces:**
- Produces: `buildDraftPresenceCta(draftId: string, roleId: string, siteUrl: string): string`

- [ ] Write failing test requiring role mention and draft URL.
- [ ] Implement message builder using localized text.
- [ ] Run `cd discord-bot && npm test`.

### Task 2: Send CTA After Presence Publication

**Files:**
- Modify: `discord-bot/src/modules/drafts/draftInteractions.ts`
- Modify: `discord-bot/src/config/env.ts`
- Modify: `discord-bot/.env.example`
- Modify: `docker-stack.prod.yml`

**Interfaces:**
- Consumes: `buildDraftPresenceCta` from Task 1.
- Produces: second channel message after presence embed publication.

- [ ] Add `DRAFT_NOTIFY_ROLE_ID` to env schema.
- [ ] After `channel.send({ embeds: ... })`, send CTA text.
- [ ] Include `MentionRoles` in required permissions.
- [ ] Run `cd discord-bot && npm run build`.
