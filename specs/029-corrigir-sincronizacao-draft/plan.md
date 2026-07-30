# Implementation Plan: Corrigir Sincronização e Operação do Draft

**Branch**: `feature/029-corrigir-sincronizacao-draft` | **Date**: 2026-07-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/029-corrigir-sincronizacao-draft/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Tornar o ciclo da feature 028 convergente e operável em múltiplos clientes sem alterar suas regras centrais. O backend publicará, após commit e em modo best-effort observável, um snapshot SignalR compartilhado sem capacidade de usuário; o endpoint HTTP continuará personalizado. O frontend coordenará conexão, geração, reconciliação e conflitos em `DraftsView`, manterá o transporte em `draftMontagemRealtime.ts` e preservará clone/dirty no board. Workers usarão projeções mínimas e um scope/comando por draft, com autoria sistêmica explícita.

## Technical Context

**Language/Version**: C# 14 / .NET 10; TypeScript 5.9; Vue 3.5 Composition API

**Primary Dependencies**: ASP.NET Core Web API e SignalR, Entity Framework Core, PostgreSQL, MediatR, FluentValidation, Vue Router, vue-i18n, Axios e `@microsoft/signalr` 10

**Storage**: PostgreSQL; `versao_estado` existente permanece token monotônico de concorrência. SQL de publicação visível incrementa versão/data atomicamente. Migration aditiva torna a autoria administrativa capaz de representar `System` sem usuário humano.

**Testing**: xUnit, FluentAssertions e Moq no backend; Vitest, Vue Test Utils e happy-dom no frontend; testes de integração com `WebApplicationFactory` e validação multicliente em navegador

**Target Platform**: backend Linux no Dev Container; frontend web responsivo nos viewports 1440+, 1280, 1024, 768 e 480

**Project Type**: aplicação web monolítica com API e SPA separadas

**Performance Goals**: mudanças normais visíveis em até 2 s; convergência após retorno do backend em até 5 s pelo pior caso de fallback de 3 s + timeout de requisição de 2 s; timer a cada 1 s sem carregar coleções dos agregados candidatos

**Constraints**: snapshot completo, versão monotônica, uma réplica, sem Redis/backplane/outbox/store novo, sem polling saudável permanente, sem alterar as regras centrais da feature 028

**Scale/Scope**: uso interno, uma réplica, um draft ativo por geração de tela, cobertura de todas as mutações exibidas e dos dois workers de draft

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pré-Design

| Gate | Status | Evidência |
|------|--------|----------|
| MVP e uso interno | PASS | Solução mantém uma réplica e não adiciona infraestrutura distribuída. |
| Integrações não travam o produto | PASS | SignalR degrada para GET autenticado; falha de publicação não reverte commit. |
| Separação API/Application/Domain/Infrastructure/Tests | PASS | Publisher e contratos ficam na Application por ports; SignalR fica na API; SQL e migrations ficam na Infrastructure. |
| Regras fora de controllers/componentes | PASS | Controller/Hub apenas encaminham; precedência por versão e invariantes permanecem nos casos de uso/domínio. |
| PostgreSQL e evolução segura | PASS | Reutiliza `versao_estado`; eventual migration é aditiva e relacional. |
| Validação, logs e testes críticos | PASS | 409 estruturado, métricas/logs sem dados sensíveis e TDD para mutações, workers e frontend. |
| Autenticação e autorização | PASS | Hub autenticado e `JoinDraftMontagem` autorizado por query. |
| Simplicidade | PASS | Um snapshot completo, um endpoint canônico existente e nenhum store/outbox/backplane novo. |
| i18n e clareza | PASS | Estados degradados, conflitos e diálogo usam recursos PT/EN e região acessível. |

### Pós-Design

Rechecagem após `research.md`, `data-model.md`, contratos e `quickstart.md`: **PASS** em todos os gates. O desenho separa payload compartilhado de estado personalizado, limita efêmeros ao frontend, usa CQRS para Join e workers, e introduz somente a alteração de banco necessária para autoria sistêmica explícita. Não há violação a justificar.

## Project Structure

### Documentation (this feature)

```text
specs/029-corrigir-sincronizacao-draft/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── realtime-sync.openapi.yaml
│   ├── realtime-events.md
│   └── ui-contracts.md
└── tasks.md
```

### Source Code (repository root)

```text
BackEnd/
├── src/
│   ├── RinhaDasLendas.Api/{Hubs,Observability,Services}/
│   ├── RinhaDasLendas.Application/{Dtos,Handlers/DraftMontagens,Interfaces,Queries/DraftMontagens}/
│   ├── RinhaDasLendas.Domain/{Entities,Enums,Models,Repositories}/
│   └── RinhaDasLendas.Infrastructure/{Migrations,Persistence,Repositories}/
└── tests/RinhaDasLendas.Tests/{Application,Integration,Services}/

FrontEnd/src/
├── components/drafts/visual/
├── i18n/locales/
├── services/
├── types/
└── views/
```

**Structure Decision**: Preservar a arquitetura web existente. Coordenação de sessão fica em `DraftsView.vue`, ciclo da conexão em `draftMontagemRealtime.ts`, estado editável em `DraftVisualBoard.vue`; Application implementa o publisher contra ports, API adapta SignalR/telemetria/workers e Infrastructure mantém SQL, projeções e migration.

## Design Decisions

- SignalR envia `DraftMontagemRealtimeSnapshotDto`; HTTP preserva o envelope flat `DraftMontagemRealtimeStateDto(Montagem, ServerNow, CanCurrentUserPick)`.
- `DraftMontagemRealtimePublisher` fica na Application e depende de notifier/telemetry/repository ports. `PublishAfterCommitAsync` não recebe request token, usa timeout interno de 5 s e absorve inclusive o cancelamento desse timeout.
- Toda mudança visível, inclusive claim/sucesso/falha/expiração/reconciliação de expirados via SQL e republicação Discord via agregado, avança atomicamente `versao_estado`/`data_atualizacao`, retorna `DraftMontagemVersionStamp` e publica cada stamp após commit.
- Snapshot com a mesma versão nunca reaplica estado compartilhado; HTTP da mesma versão pode atualizar apenas metadados personalizados/temporais aceitos por sequência global própria.
- `JoinDraftMontagem` e GET compartilham a regra: usuário humano autenticado pode ver draft existente e não arquivado; inexistente, arquivado e identidade não humana recebem a mesma rejeição localizada.
- Archive publica snapshot carregado `including archived` e evento `DraftMontagemArchived`; restore publica snapshot e `DraftMontagemRestored`, ambos best-effort.
- Workers consultam candidatos contendo somente `Id`, criam scope + comando por draft, observam qualquer falha do item e continuam; somente cancelamento solicitado pelo host propaga.
- A autoria usa `DraftMontagemActor` (`User`, `System`) e chega ao DTO/OpenAPI/types/UI como `responsavelTipo`, usuário nullable e label `Sistema`/`System`.
- O frontend abre e recupera na ordem start -> Join -> GET canônico; `connected` só é emitido após os três passos. Falha de Join/rejoin/GET mantém degradação, ativa fallback a cada 3 s com timeout de 2 s e agenda restart controlado quando aplicável.
- Estado compartilhado usa draft + generation + `versaoEstado`; metadados personalizados usam sequência global entre lanes. O detalhe administrativo que carrega o draft canônico usa a lane passiva; somente buscas de elegíveis de presença/capitães usam `auxiliaryRequestId`/`AbortController` independentes.
- A unidade 3 mantém método/adapter/doubles antigos do notifier para compilação; somente a unidade 4, após migrar publicação/reconciliação e deletar o helper antigo, remove essa superfície e comprova zero referências/build.
- O board recebe `canonicalResetToken` para descarte explícito e `acceptedSaveVersion` para limpar dirty somente no save correspondente.

## Delivery Phases

### Phase 0 - Research

Consolidar decisões e alternativas em [research.md](./research.md), sem desconhecidos pendentes.

### Phase 1 - Design and Contracts

Definir modelo em [data-model.md](./data-model.md), HTTP em [realtime-sync.openapi.yaml](./contracts/realtime-sync.openapi.yaml), SignalR em [realtime-events.md](./contracts/realtime-events.md), UI em [ui-contracts.md](./contracts/ui-contracts.md) e validação em [quickstart.md](./quickstart.md).

### Phase 2 - Executable TDD Plan

Executar as treze unidades revisáveis de [2026-07-30-corrigir-sincronizacao-draft.md](../../docs/superpowers/plans/2026-07-30-corrigir-sincronizacao-draft.md). `tasks.md` será gerado separadamente pelo fluxo Spec Kit antes de implementação.

## Complexity Tracking

Nenhuma violação constitucional. Não são introduzidos projetos, stores, filas, outbox, Redis ou padrões adicionais sem necessidade.
