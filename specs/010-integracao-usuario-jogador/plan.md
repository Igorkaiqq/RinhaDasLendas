# Implementation Plan: Integração Usuário-Jogador

**Branch**: `feature/010-integracao-usuario-jogador` | **Date**: 2026-06-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/010-integracao-usuario-jogador/spec.md`

## Summary

A feature fecha o fluxo entre conta autenticada e perfil de jogo: cadastro público continua criando apenas o usuário, mas a plataforma passa a detectar usuários sem jogador vinculado e oferecer um fluxo obrigatório de completar perfil de jogador antes de incluí-los na listagem de jogadores e em drafts. A implementação deve reaproveitar as regras de validação de jogador existentes, criar comandos/consultas próprios para perfil de jogador do usuário autenticado e manter compatibilidade com jogadores legados sem usuário.

## Technical Context

**Language/Version**: .NET 10 no backend; Vue 3 + TypeScript no frontend

**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core, PostgreSQL, FluentValidation, MediatR, ASP.NET Core Identity, Vue 3 Composition API, Axios, Vue Router

**Storage**: PostgreSQL com EF Core migrations; `jogadores.usuario_id` já existe como vínculo opcional e único

**Testing**: xUnit, FluentAssertions, Moq para backend; Vitest, vue-tsc, ESLint para frontend

**Target Platform**: Aplicação web interna executada via devcontainer/containers Linux

**Project Type**: Web application com backend REST e frontend SPA

**Performance Goals**: Fluxo de completar perfil deve responder de forma perceptível ao usuário em operação normal; listagens de até 100 usuários/jogadores devem manter uso fluido

**Constraints**: Não depender de Discord OAuth, Riot API ou validações externas; manter jogadores legados sem usuário; manter regras de autorização no backend; não duplicar regra de negócio só no frontend; não criar nova estrutura fora de `BackEnd/` e `FrontEnd/`

**Scale/Scope**: Uso interno de comunidade, até centenas de usuários/jogadores; foco em MVP manual e extensível

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **MVP Primeiro**: PASS. Fluxo manual, sem Discord/Riot obrigatórios.
- **Uso Interno**: PASS. Escopo limitado à comunidade e não adiciona complexidade de escala pública.
- **Simplicidade de Uso**: PASS. Usuário cadastra, acessa e recebe CTA claro para completar perfil.
- **Regras de Jogo Claras**: PASS. Preferências de rota, elo e links ficam explícitos antes do draft.
- **Integrações Não Devem Travar**: PASS. Todos os campos são entrada manual.
- **Arquitetura e Qualidade**: PASS. Backend mantém Api/Application/Domain/Infrastructure/Tests; frontend consome API.
- **Regras de Domínio e Evolução**: PASS. Usa jogador com Discord/Riot/OP.GG/Deeplol/elo/status/rotas e valida invariantes.

## Project Structure

### Documentation (this feature)

```text
specs/010-integracao-usuario-jogador/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── perfil-jogador-api.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
BackEnd/
├── src/
│   ├── RinhaDasLendas.Api/
│   │   └── Controllers/
│   ├── RinhaDasLendas.Application/
│   │   ├── Commands/
│   │   ├── Dtos/
│   │   ├── Handlers/
│   │   ├── Queries/
│   │   └── Validators/
│   ├── RinhaDasLendas.Domain/
│   │   ├── Entities/
│   │   └── Repositories/
│   └── RinhaDasLendas.Infrastructure/
│       ├── Persistence/
│       └── Repositories/
└── tests/
    └── RinhaDasLendas.Tests/

FrontEnd/
└── src/
    ├── components/
    ├── router/
    ├── services/
    ├── types/
    └── views/
```

**Structure Decision**: Usar a estrutura existente `BackEnd/` e `FrontEnd/`, adicionando apenas comandos/queries/dtos/handlers/validators necessários ao perfil de jogador do usuário autenticado e componentes/telas para onboarding/edição no frontend.

## Phase 0: Research

See [research.md](./research.md).

## Phase 1: Design

See [data-model.md](./data-model.md), [contracts/perfil-jogador-api.md](./contracts/perfil-jogador-api.md), and [quickstart.md](./quickstart.md).

## Post-Design Constitution Check

- **MVP Primeiro**: PASS. Sem integração externa obrigatória e sem automações prematuras.
- **Uso Interno**: PASS. Solução direta para comunidade interna.
- **Simplicidade de Uso**: PASS. CTA/onboarding claro e edição própria do perfil.
- **Regras de Jogo Claras**: PASS. Jogador incompleto não entra no draft; regras de rota permanecem explícitas.
- **Integrações Não Devem Travar**: PASS. Dados de Discord/Riot/links são manuais.
- **Arquitetura e Qualidade**: PASS. Casos de uso separados, validações no backend e testes planejados.
- **Regras de Domínio e Evolução**: PASS. Mantém manual-first e compatibilidade com jogadores legados.

## Complexity Tracking

No constitution violations.
