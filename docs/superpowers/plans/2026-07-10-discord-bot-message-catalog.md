# Discord Bot Message Catalog Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add all approved Discord bot success/error/user-interaction messages from parts 1 and 2.

**Architecture:** Keep user-visible text in `discord-bot/src/shared/messages/*.ts`. Add a small error classification helper in the drafts module so command/button handlers can map API/Discord/configuration failures to localized messages without exposing raw API payloads.

**Tech Stack:** TypeScript, discord.js, Node test runner/Vitest setup already used by `discord-bot`, existing message module.

## Global Constraints

- Do not commit changes.
- All user-visible bot messages must come from `pt-BR.ts` and `en-US.ts`.
- Keep implementation minimal and scoped to `discord-bot`.
- Write failing tests before production code.

---

### Task 1: Error Classification Tests

**Files:**
- Modify: `discord-bot/src/modules/drafts/draftInteractions.spec.ts`
- Modify: `discord-bot/src/modules/drafts/draftInteractions.ts`

**Interfaces:**
- Produces: `getDraftInteractionErrorMessage(error: unknown, context: DraftInteractionErrorContext): string`

- [ ] Add tests for API unavailable, unauthorized API, missing channel permissions, not found, closed presence, linked account without player profile, invalid captain IDs, invalid pick order, and fallback generic messages.
- [ ] Run `cd discord-bot && npm test -- draftInteractions` and verify the new tests fail because the helper does not exist.
- [ ] Implement the helper with exact localized messages.
- [ ] Re-run `cd discord-bot && npm test -- draftInteractions` and verify pass.

### Task 2: Command/Button Message Integration

**Files:**
- Modify: `discord-bot/src/modules/drafts/draftInteractions.ts`
- Modify: `discord-bot/src/shared/messages/pt-BR.ts`
- Modify: `discord-bot/src/shared/messages/en-US.ts`

**Interfaces:**
- Consumes: `getDraftInteractionErrorMessage(error, context)`.
- Produces: command/button handlers that respond with specific localized messages.

- [ ] Add/adjust tests for create/list/cancel/close/captains/pick-order/button responses.
- [ ] Run targeted bot tests and verify failures.
- [ ] Wrap each interaction branch with localized error handling where needed.
- [ ] Re-run targeted tests.

### Task 3: Verification and i18n Audit

**Files:**
- Modify only if verification finds missing parity.

- [ ] Run `cd discord-bot && npm test`.
- [ ] Run `cd discord-bot && npm run build`.
- [ ] Confirm `pt-BR.ts` and `en-US.ts` contain matching keys.
- [ ] Report i18n audit in final response.
