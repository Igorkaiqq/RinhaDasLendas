# Implementation Plan: Montagem Visual de Draft e Times

**Branch**: `feature/008-montagem-visual-draft-times` | **Date**: 2026-06-20 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/008-montagem-visual-draft-times/spec.md`

## Summary

Implementar uma montagem visual de draft/times integrada aos jogadores cadastrados, baseada no prototipo atual `exemplo/index.html`: jogadores reais entram em um board com area de jogadores livres, reservas e times dinamicos; organizador monta times por drag and drop, consulta detalhes do jogador por clique, define capitaes, escolhe rota contextual por draft e exporta o resultado em imagem.

A abordagem recomendada e criar um agregado novo para `MontagemDraft` separado do `DraftSessao` atual. O draft atual permanece responsavel por picks alternados entre dois times; a nova montagem visual cobre o fluxo manual multi-times com persistencia do layout final.

## Technical Context

**Language/Version**: .NET 10 no backend; Vue 3 com TypeScript no frontend.

**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core, PostgreSQL, FluentValidation, MediatR, Vue Router, Vitest. Para drag and drop, priorizar APIs nativas do navegador no MVP; avaliar biblioteca apenas se acessibilidade/mobile exigir.

**Storage**: PostgreSQL relacional via EF Core migrations, UUIDs, chaves estrangeiras explicitas e snake_case.

**Testing**: xUnit, FluentAssertions e Moq no backend; Vitest e Vue Test Utils no frontend.

**Target Platform**: Web app interna executada via Dev Container, backend HTTP e frontend SPA.

**Project Type**: Aplicacao web com backend API e frontend Vue.

**Performance Goals**: Abrir criacao com ate 100 jogadores ativos sem travamento perceptivel; montar 20 jogadores em ate 3 minutos; salvar layout em operacao percebida como imediata para uso interno.

**Constraints**: Funcionar sem Discord/Riot; regras criticas de duplicidade, capacidade, capitaes e reservas no backend/domain; UI dark-first e card-first; nao assumir quatro times fixos.

**Scale/Scope**: Uso interno; dezenas de jogadores cadastrados; montagens com 1 a 8 times como alvo visual inicial, sem limite fixo de quatro times no dominio.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- MVP primeiro: PASS. Entrega fluxo manual integrado, sem depender de Discord/Riot.
- Uso interno: PASS. Escopo voltado ao grupo e montagens de dezenas de jogadores.
- Simplicidade de uso: PASS. Reaproveita interacao conhecida do prototipo: cards, drop zones e exportacao.
- Regras de jogo claras: PASS. Capitaes, reservas, capacidade e criterios ficam visiveis e validados.
- Integracoes nao travam: PASS. Usa jogadores cadastrados manualmente e preferencias existentes.
- Arquitetura e qualidade: PASS. Novo agregado/DDD/CQRS evita misturar regra em controllers ou UI.
- Regras de dominio e evolucao: PASS. Protege duplicidade e preserva caminho para balanceamento/sorteio futuro.

## Project Structure

### Documentation (this feature)

```text
specs/008-montagem-visual-draft-times/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── draft-montagens-api.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
BackEnd/
├── src/RinhaDasLendas.Api/Controllers/DraftMontagensController.cs
├── src/RinhaDasLendas.Application/Commands/DraftMontagens/
├── src/RinhaDasLendas.Application/Queries/DraftMontagens/
├── src/RinhaDasLendas.Application/Handlers/DraftMontagens/
├── src/RinhaDasLendas.Application/Validators/DraftMontagemValidator.cs
├── src/RinhaDasLendas.Application/Dtos/*DraftMontagem*.cs
├── src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs
├── src/RinhaDasLendas.Domain/Entities/DraftMontagemTime.cs
├── src/RinhaDasLendas.Domain/Entities/DraftMontagemParticipante.cs
├── src/RinhaDasLendas.Domain/Enums/DraftMontagem*.cs
├── src/RinhaDasLendas.Domain/Repositories/IDraftMontagemRepository.cs
├── src/RinhaDasLendas.Infrastructure/Repositories/DraftMontagemRepository.cs
└── tests/RinhaDasLendas.Tests/{Domain,Handlers,Validators,Integration}/

FrontEnd/
├── src/views/DraftsView.vue
├── src/components/drafts/visual/
├── src/services/draftMontagens.ts
├── src/types/draftMontagem.ts
└── src/constants/draftMontagemStatus.ts
```

**Structure Decision**: Usar a estrutura existente `BackEnd/` e `FrontEnd/`. A feature deve ser adicionada como evolucao da area de drafts, sem criar pastas raiz alternativas.

## Complexity Tracking

Nenhuma violacao constitucional identificada. A criacao de novo agregado aumenta escopo tecnico, mas reduz acoplamento e risco de quebrar o draft de picks alternados ja existente.

## Phase 0: Research

See [research.md](./research.md).

## Phase 1: Design

See [data-model.md](./data-model.md), [contracts/draft-montagens-api.md](./contracts/draft-montagens-api.md), and [quickstart.md](./quickstart.md).

## Post-Design Constitution Check

- MVP primeiro: PASS. Plano prioriza criacao, board, validacao e persistencia antes de balanceamento automatico.
- Uso interno: PASS. Sem premissas de escala publica.
- Simplicidade de uso: PASS. Fluxo mantém board visual, cards e drop zones.
- Regras de jogo claras: PASS. Capacidade, reservas e capitaes ficam calculados e retornados pela API.
- Integracoes nao travam: PASS. Nenhuma dependencia externa nova obrigatoria.
- Arquitetura e qualidade: PASS. Backend mantem regras criticas; frontend fica responsavel pela interacao.
- Regras de dominio e evolucao: PASS. Modelo suporta multi-times, reservas e rota contextual sem alterar preferencias globais do jogador.
