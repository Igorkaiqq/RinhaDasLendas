# Implementation Plan: Robustecer Drafts, Discord e Jogadores

**Branch**: `feature/016-melhorias-drafts-presenca-discord` | **Date**: 2026-07-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/017-robustecer-drafts-discord-jogadores/spec.md`

## Summary

Robustecer o fluxo integrado entre site, backend e bot Discord para que convites abram o draft correto, o bot respeite configuração e erros estruturados, publicações Discord sejam rastreáveis e recuperáveis, presenças sejam idempotentes e visíveis em tempo real, e ações administrativas críticas tenham auditoria. A abordagem preserva o fluxo manual como fallback, mantém regras críticas no domínio/backend e limita o bot a adaptador operacional.

## Technical Context

**Language/Version**: .NET 10 backend, Vue 3 + TypeScript frontend, Node.js + TypeScript discord-bot

**Primary Dependencies**: ASP.NET Core Web API, EF Core, PostgreSQL, MediatR, FluentValidation, SignalR, Vue I18n, discord.js

**Storage**: PostgreSQL via EF Core migrations

**Testing**: xUnit, FluentAssertions, Moq no backend; npm test no frontend; Node test runner via tsx no discord-bot

**Target Platform**: Linux/containerized deployment with existing Docker stack and devcontainer

**Project Type**: Web application with backend API, frontend SPA and Discord bot worker

**Performance Goals**: Draft links open the correct draft without manual search; realtime presence updates arrive within 5 seconds in normal conditions; manual player search completes within 10 seconds for administrators

**Constraints**: No hardcoded user-facing text; no secrets committed; bot cannot be the only operational path; controllers stay thin; domain must not depend on HTTP, Discord, EF or DTOs

**Scale/Scope**: Internal community platform; feature spans draft presence, Discord publication, player eligibility and administrative audit

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- MVP Primeiro: PASS. Improvements strengthen existing draft/Discord flow and keep manual fallback.
- Uso Interno: PASS. Scope targets internal operational reliability, not public-scale redesign.
- Simplicidade de Uso: PASS. Main user impact is clearer links, messages, status and recovery actions.
- Regras de Jogo Claras: PASS. Presence, captain and publication states become more explicit.
- Integrações Não Devem Travar o Produto: PASS. Discord disabled/unavailable must not block site/manual flow.
- Arquitetura e Qualidade: PASS. Rules remain in backend/domain/application; bot remains adapter; tests required.
- Regras de Domínio e Evolução: PASS. Prevents duplicate player presence and records important administrative changes.

## Project Structure

### Documentation (this feature)

```text
specs/017-robustecer-drafts-discord-jogadores/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── backend-api.md
│   ├── discord-bot.md
│   └── frontend-ui.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
BackEnd/
├── src/RinhaDasLendas.Api/
│   ├── Controllers/
│   ├── Hubs/
│   ├── Services/
│   └── Resources/
├── src/RinhaDasLendas.Application/
│   ├── Commands/DraftMontagens/
│   ├── Dtos/
│   ├── Handlers/DraftMontagens/
│   ├── Queries/DraftMontagens/
│   └── Validators/
├── src/RinhaDasLendas.Domain/
│   ├── Entities/
│   ├── Enums/
│   └── Repositories/
├── src/RinhaDasLendas.Infrastructure/
│   ├── Migrations/
│   └── Repositories/
└── tests/RinhaDasLendas.Tests/

FrontEnd/
├── src/views/DraftsView.vue
├── src/services/
├── src/types/
└── src/i18n/locales/

discord-bot/
├── src/modules/drafts/
├── src/shared/api/
├── src/shared/messages/
└── src/discord/
```

**Structure Decision**: Use the existing three-part application structure. Backend owns state, rules, audit and realtime notifications; frontend owns interaction and i18n display; discord-bot consumes backend contracts and performs Discord-specific communication.

`DraftReasonDialog.vue` concentra apresentação e validação local do motivo; `DraftsView.vue` mantém somente a ação pendente e o despacho para serviços existentes.

## Complexity Tracking

No constitution violations identified.

## Phase Plan

1. P1 reliability: CTA route selection, botEnabled enforcement, structured bot errors, past-date validation and permission messaging.
2. P1 publication safety: persisted Discord publication state, dedupe after restart, publication status and republish action.
3. P2 presence consistency: idempotency/concurrency, eligible player search and realtime presence updates.
4. P2 operational audit: administrative reasons, responsible user, metrics/logs and UI visibility.
5. P2 confirmação contextual: componente único baseado em Reka UI, integração dos quatro fluxos, i18n e testes responsivos.

## Post-Design Constitution Check

- Manual fallback preserved for all Discord-dependent behavior.
- New persistent state is limited to operational publication/audit needs.
- Domain remains infrastructure-agnostic; Discord-specific fields are represented as system state, not Discord SDK objects.
- All user-facing messages require frontend/backend/bot localization.
