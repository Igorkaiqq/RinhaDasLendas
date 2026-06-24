# Implementation Plan: Draft em Tempo Real na Montagem de Times

**Branch**: `012-draft-montagem-tempo-real` | **Date**: 2026-06-24 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/012-draft-montagem-tempo-real/spec.md`

## Summary

Evoluir a montagem visual de draft (`DraftMontagem`) para suportar um modo de draft em tempo real conduzido por capitães ativos. O backend será a fonte oficial do estado, controlará ordem de escolha e timer de 30 segundos, sincronizará atualizações em tempo real para os clientes e impedirá que reservas sejam escolhidos. Reservas continuam separados como complemento emergencial para substituições posteriores. A implementação deve preservar o modo manual existente, aplicar regras críticas no domínio/aplicação e manter todos os textos por i18n/resources.

## Technical Context

**Language/Version**: .NET 10 no backend; Vue 3 + TypeScript no frontend

**Primary Dependencies**: ASP.NET Core Web API, SignalR, Entity Framework Core, PostgreSQL, FluentValidation, MediatR, ASP.NET Core Identity/JWT, Vue 3 Composition API, Axios, Vue Router, vue-i18n, cliente SignalR no frontend

**Storage**: PostgreSQL com EF Core migrations; modelagem relacional para turnos, escolhas e substituições emergenciais

**Testing**: xUnit, FluentAssertions e Moq no backend; Vitest, vue-tsc e ESLint no frontend

**Target Platform**: Aplicação web interna executada via containers/devcontainer Linux

**Project Type**: Web application com backend REST/tempo real e frontend SPA

**Performance Goals**: Atualizações de escolha devem aparecer em outras sessões em até 2 segundos em uso normal; timeout deve avançar com tolerância operacional de até 2 segundos; tela deve continuar fluida em montagens de até 20 participantes

**Constraints**: Backend é fonte oficial; frontend não valida regra crítica; timer de 30 segundos controlado pelo backend; reservas não podem ser escolhidos; mensagens backend por resources; textos frontend por i18n; sem texto hardcoded; preservar modo manual de montagem

**Scale/Scope**: Uso interno de comunidade, dezenas de usuários simultâneos e montagens com até dezenas de participantes; primeira versão otimizada para uma instância de backend

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **MVP Primeiro**: PASS. Mantém fluxo manual existente e adiciona tempo real sem depender de Discord/Riot.
- **Uso Interno**: PASS. Escopo é de comunidade interna, sem sobreengenharia para escala pública.
- **Simplicidade de Uso**: PASS. Capitão vê vez, timer e ação principal; demais usuários visualizam estado.
- **Regras de Jogo Claras**: PASS. Ordem, capitão da vez, histórico, timeout e reservas ficam explícitos.
- **Integrações Não Devem Travar**: PASS. Não depende de integrações externas.
- **Arquitetura e Qualidade**: PASS. Regras críticas ficam no backend/domain; controllers e hub apenas orquestram casos de uso.
- **Regras de Domínio e Evolução**: PASS. Draft registra ordem de escolhas e preserva participantes/reservas.

## Project Structure

### Documentation (this feature)

```text
specs/012-draft-montagem-tempo-real/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── draft-montagem-tempo-real-api.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
BackEnd/
├── src/
│   ├── RinhaDasLendas.Api/
│   │   ├── Controllers/
│   │   ├── Hubs/
│   │   └── Program.cs
│   ├── RinhaDasLendas.Application/
│   │   ├── Commands/DraftMontagens/
│   │   ├── Dtos/
│   │   ├── Handlers/DraftMontagens/
│   │   ├── Interfaces/
│   │   ├── Queries/DraftMontagens/
│   │   └── Validators/
│   ├── RinhaDasLendas.Domain/
│   │   ├── Constants/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   └── Repositories/
│   └── RinhaDasLendas.Infrastructure/
│       ├── Messages/
│       ├── Migrations/
│       ├── Persistence/
│       └── Repositories/
└── tests/
    └── RinhaDasLendas.Tests/

FrontEnd/
└── src/
    ├── components/drafts/visual/
    ├── constants/
    ├── i18n/locales/
    ├── services/
    ├── types/
    └── views/
```

**Structure Decision**: Usar a estrutura existente `BackEnd/` e `FrontEnd/`. O backend adiciona entidades, comandos, queries, hub, serviço de timer e migrations dentro das camadas atuais. O frontend evolui a tela e os serviços de `DraftMontagem`, sem criar nova aplicação ou pasta raiz alternativa.

## Phase 0: Research

See [research.md](./research.md).

## Phase 1: Design

See [data-model.md](./data-model.md), [contracts/draft-montagem-tempo-real-api.md](./contracts/draft-montagem-tempo-real-api.md), and [quickstart.md](./quickstart.md).

## Post-Design Constitution Check

- **MVP Primeiro**: PASS. O plano mantém substituição emergencial simples e não adiciona balanceamento distribuído na primeira versão.
- **Uso Interno**: PASS. Dimensionamento condizente com uso interno.
- **Simplicidade de Uso**: PASS. Interface proposta expõe uma ação primária por turno e áreas claras para livres/reservas.
- **Regras de Jogo Claras**: PASS. Estado oficial inclui capitão da vez, tempo, histórico e reservas separadas.
- **Integrações Não Devem Travar**: PASS. Sem dependência de integrações externas.
- **Arquitetura e Qualidade**: PASS. CQRS, domínio, resources, i18n, testes de regras e DTOs preservados.
- **Regras de Domínio e Evolução**: PASS. Entidades novas permitem histórico de escolhas, timeout e substituição.

## Complexity Tracking

No constitution violations.
