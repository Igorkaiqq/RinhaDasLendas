# Implementation Plan: Cadastro de Time

**Branch**: `feature/006-cadastro-time` | **Date**: 2026-06-19 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/006-cadastro-time/spec.md`

## Summary

Implementar o cadastro e gestao de times reutilizaveis para a RinhaDasLendas, permitindo criar, editar, consultar, filtrar, inativar e reativar times compostos por jogadores ativos ja cadastrados. A solucao deve seguir DDD, CQRS, REST, PostgreSQL relacional, FluentValidation, mensagens padronizadas e frontend Vue 3 com i18n e design system existente.

## Technical Context

**Language/Version**: Backend: .NET 10 / ASP.NET Core Web API; Frontend: Vue 3 + TypeScript + Composition API

**Primary Dependencies**: Backend: Entity Framework Core, FluentValidation, MediatR, Dependency Injection; Frontend: Vue Router, Vue I18n, Vitest, services HTTP existentes

**Storage**: PostgreSQL com migrations EF Core, UUID como chave primaria, FKs explicitas, snake_case

**Testing**: Backend: xUnit, FluentAssertions, Moq; Frontend: Vitest; validacao adicional por build/lint

**Target Platform**: Web application interna executada via browser e API HTTP

**Project Type**: Web application com backend em `BackEnd/` e frontend em `FrontEnd/`

**Performance Goals**: Listagem de times deve usar paginacao e filtros no backend; busca por nome, tag ou jogador deve responder em tempo adequado para uso interno; consultas devem evitar N+1

**Constraints**: Regras criticas devem existir no dominio/backend; frontend nao deve ser unica fonte de regra; times inativos nao podem aparecer como opcao principal em fluxos futuros; nenhuma integracao externa e necessaria

**Scale/Scope**: MVP interno com dezenas de jogadores e times, preparado para evoluir para drafts, partidas e historico sem depender de Riot API ou Discord

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Compliance Assessment

**MVP Primeiro**:
- PASS: Resolve um fluxo manual e essencial sem depender de Discord ou Riot API.
- PASS: Mantem escopo simples: cadastro, composicao, consulta e inativacao logica.

**Uso Interno**:
- PASS: Otimizado para organizadores e usuarios internos, sem requisitos de escala publica.
- PASS: Mantem estrutura extensivel para drafts e partidas sem antecipar regras externas.

**Simplicidade de Uso**:
- PASS: Times devem ser criados e encontrados rapidamente com nome, tag, status, capitao e membros visiveis.
- PASS: Estados vazios, confirmacoes e erros devem ser claros.

**Regras de Jogo Claras**:
- PASS: Limite de ate cinco jogadores principais, capitao entre membros e bloqueio de duplicidade ficam explicitos.
- PASS: Inativacao preserva historico e evita uso em fluxos principais futuros.

**Integracoes Nao Devem Travar o Produto**:
- PASS: Nenhuma regra depende de Riot API, Discord ou servico externo.

**Arquitetura e Qualidade**:
- PASS: Domain concentra invariantes de time e composicao.
- PASS: Application separa comandos e consultas via CQRS.
- PASS: Infrastructure cuida de EF Core, migrations e repositorios.
- PASS: Api expõe DTOs REST sem retornar entidades diretamente.
- PASS: Frontend consome API e usa i18n/design system existentes.
- PASS: Regras criticas terao testes automatizados.

**Status**: PASS | No violations.

## Project Structure

### Documentation (this feature)

```text
specs/006-cadastro-time/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── checklists/
│   └── requirements.md
├── contracts/
│   ├── api-teams.md
│   └── frontend-teams-ui.md
└── tasks.md
```

### Source Code (repository root)

```text
BackEnd/
├── src/RinhaDasLendas.Domain/
│   ├── Entities/Time.cs
│   ├── Entities/TimeMembro.cs
│   ├── Enums/TimeStatus.cs
│   └── Repositories/ITimeRepository.cs
├── src/RinhaDasLendas.Application/
│   ├── Commands/Times/
│   ├── Queries/Times/
│   ├── Handlers/Times/
│   ├── Dtos/
│   └── Validators/
├── src/RinhaDasLendas.Infrastructure/
│   ├── Repositories/TimeRepository.cs
│   ├── Persistence/RinhaDasLendasDbContext.cs
│   └── Migrations/
├── src/RinhaDasLendas.Api/
│   └── Controllers/TimesController.cs
└── tests/RinhaDasLendas.Tests/

FrontEnd/
└── src/
    ├── views/TeamsView.vue
    ├── components/teams/
    ├── services/teams.ts
    ├── types/team.ts
    ├── constants/teamStatus.ts
    ├── router/index.ts
    └── i18n/locales/
```

**Structure Decision**: Usar a estrutura existente `BackEnd/` e `FrontEnd/`. Backend segue camadas Api, Application, Domain, Infrastructure e Tests. Frontend adiciona a tela de Times, componentes especificos, service HTTP, tipos e traducoes sem criar novas pastas raiz.

## Complexity Tracking

No constitution violations. No extra complexity accepted.

## Phase 0: Research

Research output: [research.md](research.md)

## Phase 1: Design & Contracts

Design output:
- [data-model.md](data-model.md)
- [contracts/api-teams.md](contracts/api-teams.md)
- [contracts/frontend-teams-ui.md](contracts/frontend-teams-ui.md)
- [quickstart.md](quickstart.md)

### Post-Design Constitution Check

**Status**: PASS

- Domain model keeps team invariants outside controllers and frontend components.
- API contracts use REST, DTOs, pagination, proper HTTP status codes and validation errors.
- Database model uses relational tables, UUID keys, explicit FKs, snake_case and logical deletion.
- Frontend contract follows existing Vue/i18n/design-system guidance and avoids business-rule-only validation.
- No integration dependency was introduced.
