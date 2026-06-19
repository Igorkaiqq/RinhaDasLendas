# Implementation Plan: Draft de Jogadores

**Branch**: `feature/007-draft-jogadores` | **Date**: 2026-06-19 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/007-draft-jogadores/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Implementar o fluxo completo de draft de jogadores: criacao de sessoes, definicao manual ou sorteada de capitaes, picks alternados, conclusao automatica, cancelamento, consulta de historico e tela frontend para operar o draft. A abordagem segue DDD/CQRS: regras criticas ficam na entidade de dominio `DraftSessao`, comandos e queries ficam separados na Application, persistencia relacional com EF Core/PostgreSQL na Infrastructure, e a UI Vue consome endpoints REST versionados.

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: .NET 10 no backend; Vue 3 com TypeScript no frontend.

**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core, PostgreSQL, FluentValidation, MediatR, Vue Router, Vitest.

**Storage**: PostgreSQL relacional via migrations EF Core, com UUIDs, chaves estrangeiras explicitas e snake_case.

**Testing**: xUnit, FluentAssertions e Moq no backend; Vitest e Vue Test Utils no frontend.

**Target Platform**: Web app interna executada via Dev Container, backend HTTP e frontend SPA.

**Project Type**: Aplicacao web com backend API e frontend Vue.

**Performance Goals**: Fluxo principal de criacao e primeiro pick em menos de 2 minutos para o usuario; listagens paginadas; operacoes de draft perceptivelmente imediatas para grupo interno.

**Constraints**: Funcionar sem Discord/Riot; regras de duplicidade, status e ordem de picks no backend/domain; controllers sem regra de negocio; frontend seguindo tokens e componentes card-first.

**Scale/Scope**: Uso interno; sessoes com dezenas de jogadores cadastrados, dois times por draft e ate cinco jogadores por time no fluxo padrao.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- MVP primeiro: PASS. Fluxo manual completo, sem depender de integracoes externas.
- Uso interno: PASS. Escopo limitado para grupos pequenos e operacao por organizador confiavel.
- Simplicidade de uso: PASS. Tela unica de drafts com CTA principal, cards de times e lista de jogadores disponiveis.
- Regras de jogo claras: PASS. Capitaes, criterio manual/sorteio, proximo pick e historico ficam visiveis.
- Integracoes nao travam: PASS. Discord/Riot fora do escopo; uso baseado nos jogadores cadastrados.
- Arquitetura e qualidade: PASS. Backend em camadas, DDD/CQRS, validadores e testes.
- Regras de dominio e evolucao: PASS. Registra capitaes, ordem de escolhas, impede duplicidade e preserva historico.

## Project Structure

### Documentation (this feature)

```text
specs/007-draft-jogadores/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
BackEnd/
├── src/RinhaDasLendas.Api/Controllers/DraftsController.cs
├── src/RinhaDasLendas.Application/Commands/Drafts/
├── src/RinhaDasLendas.Application/Queries/Drafts/
├── src/RinhaDasLendas.Application/Handlers/Drafts/
├── src/RinhaDasLendas.Application/Validators/DraftValidator.cs
├── src/RinhaDasLendas.Application/Dtos/*Draft*.cs
├── src/RinhaDasLendas.Domain/Entities/DraftSessao.cs
├── src/RinhaDasLendas.Domain/Entities/DraftParticipante.cs
├── src/RinhaDasLendas.Domain/Entities/DraftEscolha.cs
├── src/RinhaDasLendas.Domain/Enums/Draft*.cs
├── src/RinhaDasLendas.Domain/Repositories/IDraftRepository.cs
├── src/RinhaDasLendas.Infrastructure/Repositories/DraftRepository.cs
└── tests/RinhaDasLendas.Tests/{Domain,Handlers,Validators,Integration}/

FrontEnd/
├── src/views/DraftsView.vue
├── src/components/drafts/
├── src/services/drafts.ts
├── src/types/draft.ts
└── src/constants/draftStatus.ts
```

**Structure Decision**: Usar a estrutura existente `BackEnd/` e `FrontEnd/`, preservando camadas DDD/CQRS e Vue Composition API. Nenhuma pasta raiz alternativa sera criada.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

Nenhuma violacao constitucional identificada.

## Post-Design Constitution Check

- MVP primeiro: PASS. Contratos e UI cobrem operacao manual end-to-end.
- Uso interno: PASS. Escopo nao inclui permissoes avancadas ou integracoes.
- Simplicidade de uso: PASS. Quickstart valida criacao, pick, conclusao e cancelamento.
- Regras de jogo claras: PASS. Modelo guarda criterio de capitaes, criterio de primeiro pick e sequencia de escolhas.
- Integracoes nao travam: PASS. Todos os cenarios funcionam com dados internos.
- Arquitetura e qualidade: PASS. Plano inclui testes de dominio, validators, handlers, integracao e frontend.
