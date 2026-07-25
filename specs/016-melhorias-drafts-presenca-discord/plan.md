# Implementation Plan: Melhorias Drafts, Presenca e Discord

**Branch**: `feature/016-melhorias-drafts-presenca-discord` | **Date**: 2026-07-10 | **Spec**: [spec.md](./spec.md)

## Summary

Aplicar ajustes incrementais no fluxo existente de `DraftMontagem`, mantendo DDD/CQRS, REST, i18n e resources. A implementacao usa regras de dominio para presenca manual, comandos MediatR para escrita, queries paginadas para listagem e pequenas alteracoes no bot Discord e na tela `DraftsView.vue`.

## Technical Context

**Backend**: .NET 10, ASP.NET Core Web API, EF Core, PostgreSQL, MediatR, FluentValidation, resources `.resx`, xUnit, FluentAssertions, Moq.

**Frontend**: Vue 3, TypeScript, Composition API, vue-i18n, Axios, Vite.

**Bot**: Node.js 24, TypeScript, discord.js.

**Storage**: PostgreSQL existente; sem nova migration obrigatoria porque cancelamento logico ja usa status `Cancelada` e presencas ja possuem cancelamento logico.

## Constitution Check

- **MVP Primeiro**: PASS. Escopo limitado a problemas atuais de presenca/draft/Discord.
- **Uso Interno**: PASS. Fluxos direcionados a admins e jogadores da comunidade.
- **Simplicidade de Uso**: PASS. Acoes ficam no painel existente de drafts.
- **Integracoes Nao Devem Travar**: PASS. Bot Discord continua cliente da API; site continua fonte principal.
- **Arquitetura e Qualidade**: PASS. Regras no Domain/Application; controllers finos.
- **Internacionalizacao**: PASS. Novos textos via i18n/resources.

## Structure Decision

Reutilizar `DraftMontagem` como agregado. Adicionar metodos de dominio para presenca manual, comandos dedicados no Application e endpoints REST no controller existente. Evitar criar novos modulos ou tabelas porque os dados ja existem no modelo atual.

## Implementation Phases

1. Bot: teste e correcao de conversao de horario Brasilia para UTC.
2. Backend listagem: filtro padrao para ocultar cancelados e ordenacao por data da rinha.
3. Backend presenca manual: regras de dominio, comandos, validators, endpoints e resources.
4. Frontend: servicos, UI ADM+, data da rinha e i18n.
5. Fluxo encerramento: garantir proximas acoes claras e estado consistente.
6. Verificacao: testes/builds e auditoria de internacionalizacao.
