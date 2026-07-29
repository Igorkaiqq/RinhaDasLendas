# Implementation Plan: Fundação Competitiva Sazonal

**Branch**: `feature/023-fundacao-competitiva-sazonal` | **Date**: 2026-07-28 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/023-fundacao-competitiva-sazonal/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Entregar a fundação manual e sazonal para operação competitiva: Seasons não sobrepostas com ativação concorrente segura, competições, rodadas e regras imutáveis publicadas; eventos de quatro Times; Séries MD3/MD5 oficiais ou amistosas; Partidas mínimas com placar, resultado, remake, picks confirmados e Fearless bilateral reconstruível; auditoria, idempotência e capabilities esportivas explícitas. A implementação preserva o monólito modular e separa as fronteiras lógicas Competição e Partidas, reutiliza `DraftMontagem`, `Time` e `Jogador` apenas por referências estáveis/snapshots e mantém Analytics, elencos temporais e integrações fora desta fatia.

## Technical Context

**Language/Version**: C# com .NET 10; TypeScript 5.9; Vue 3.5; Node.js 22 ou 24

**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 10.0.8, PostgreSQL/Npgsql 10.0.2, MediatR 12.5, FluentValidation 12.1, ASP.NET Core Identity/JWT, Swagger; Vue Composition API, Vue Router, Pinia, Vue I18n 11, Axios, Tailwind CSS 4 e Reka UI

**Storage**: PostgreSQL relacional com migration EF Core, UUIDs, FKs explícitas, nomes `snake_case`, constraints temporais/únicas, snapshots históricos, auditoria append-only e registros de idempotência; sem JSON como modelo de domínio e sem exclusão física de fatos competitivos

**Testing**: xUnit 2.9, FluentAssertions 7.2 e Moq 4.20 no backend; testes de integração com PostgreSQL; Vitest 4, Vue Test Utils e happy-dom no frontend; Chromium via `agent-browser` para validação autenticada, responsiva, acessível e bilíngue

**Target Platform**: Serviços Linux no Dev Container/Docker, PostgreSQL 17 e navegadores modernos desktop/mobile

**Project Type**: Aplicação web full-stack no monorepo existente

**Performance Goals**: Preservar paginação de 20 itens por padrão e máximo de 100; manter consultas de coleção sem carregar auditoria ou agregados completos; ativar Season, confirmar Partida e reconstruir Fearless em uma única transação por comando; permitir concluir o fluxo manual Season → competição/rodada → Série MD3 em até cinco minutos no cenário de usabilidade da spec

**Constraints**: Operação manual sem Riot/Discord; fuso `America/Sao_Paulo` e instantes UTC; no máximo uma Season ativa; períodos `[início, fim)` sem sobreposição; toda Série possui Season e toda oficial possui competição/rodada da mesma Season; Evento com quatro Times proíbe Fearless; amistoso nunca alimenta projeção oficial; `ETag/If-Match` e `Idempotency-Key`; identidade do ator somente da autenticação; PT/EN sincronizados; nenhuma credencial ou segredo em código, payload persistido ou log; nenhuma capability esportiva do VicePresidente concedida por inferência

**Scale/Scope**: Produto interno; duas fronteiras lógicas no mesmo processo/banco; cerca de quatorze conceitos persistidos entre raízes, entidades e registros transversais, cinco controllers REST, quatro capabilities esportivas novas e reutilização de `CanManageMatches`, quatro telas/rotas operacionais, uma migration e suítes focadas de domínio, aplicação, integração, segurança e frontend

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Problema real do grupo**: PASS. Seasons, séries e partidas são pré-requisitos para registrar confrontos sem misturar períodos, contextos oficiais e amistosos.
- **MVP e simplicidade**: PASS. A solução permanece no monólito existente, usa fluxo manual e não implementa microsserviços, coleta, Score, Rating, votação ou elenco temporal.
- **Uso sem integração externa**: PASS. Todos os dados desta fatia podem ser cadastrados e corrigidos manualmente; Discord e Riot não participam do caminho crítico.
- **Regras claras e explicáveis**: PASS. Tipo, formato, modo, Season, competição/rodada, placar, Fearless, remake, elegibilidade e versões são explícitos em domínio e interface.
- **Separação de responsabilidades**: PASS. Invariantes ficam nos agregados/regras de domínio; commands/queries e handlers coordenam casos de uso; repositórios persistem; controllers e componentes não decidem regra competitiva.
- **Persistência evolutiva**: PASS. PostgreSQL relacional, UUID, FKs, constraints, migrations e snapshots preservam histórico e suportam as features 024-027 sem reescrever fatos.
- **CQRS, Repository e DI**: PASS. Escritas e leituras são separadas, cada agregado usa contrato de repositório e dependências entram por DI; padrões adicionais só aparecem onde protegem transação ou reconstrução.
- **Autenticação e autorização**: PASS. Ator vem do principal autenticado e policies por capability separam gestão de Season, competição, operação, finalização e auditoria. VicePresidente permanece sem concessão automática enquanto sua decisão fina está aberta.
- **Concorrência, idempotência e auditoria**: PASS. ETags rejeitam versão obsoleta, calendário global serializa ativação, chaves idempotentes evitam duplicação e correções preservam antes/depois, ator e justificativa.
- **Regras críticas testadas**: PASS. Plano e quickstart cobrem Season concorrente, MD3/MD5, Fearless, remake, correção, Evento, amistoso, autorização e contratos.
- **Internacionalização**: PASS. Mensagens backend usam resources e UI usa chaves PT/EN, incluindo estados, validações, confirmações, vazios e nomes acessíveis.
- **Segurança e privacidade**: PASS. Auditoria detalhada exige capability, snapshots usam dados mínimos, logs não contêm segredos e a idempotência não persiste credenciais ou payload bruto irrestrito.
- **Usabilidade e responsividade**: PASS. Fluxos usam shell/tokens atuais, cards responsivos, foco visível, estados de erro/vazio e preservam informação essencial a 320 px.

**Reavaliação após o design**: PASS. `research.md`, `data-model.md`, contratos e `quickstart.md` resolvem as decisões técnicas necessárias sem definir parâmetros de `OPEN_DECISIONS.md`. A própria Série representa o agendamento, Partidas são criadas sob demanda e usam concorrência na versão da Série, Fearless é derivado, Evento aceita quatro Times oficiais e o VicePresidente continua negado por padrão. Correção retroativa limita-se a fatos existentes e não autoriza backfill. Não há violação constitucional que exija exceção.

## Delivery Sequence

1. **P1 - Calendário e governança**: papéis/capabilities, Season, seleção sazonal, competição, rodada, regras e auditoria.
2. **P1 - Série direta operacional**: agendamento, snapshots, Partida mínima, MD3/MD5, Fearless, remake, correção, idempotência e telas.
3. **P2 - Evento de quatro Times**: agregado, associação de Séries e proibição integral de Fearless, sem bloquear a validação dos fluxos P1.

## Project Structure

### Documentation (this feature)

```text
specs/023-fundacao-competitiva-sazonal/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── competitive-foundation.openapi.yaml
│   └── ui-contracts.md
└── tasks.md
```

`tasks.md` será criado somente na fase `/speckit-tasks`, após aprovação deste plano.

### Source Code (repository root)

```text
BackEnd/
├── src/
│   ├── RinhaDasLendas.Domain/
│   │   ├── Constants/{AuthPermissions,AuthRoles,MessageCodes}.cs
│   │   ├── Entities/
│   │   │   ├── {CalendarioCompetitivo,Season,Competicao,Rodada,VersaoRegras}.cs
│   │   │   ├── {EventoCompetitivo,EventoTime}.cs
│   │   │   ├── {Serie,LadoSerie,ParticipanteEsperadoSerie,Partida,PickPartida}.cs
│   │   │   └── {RegistroAuditoriaCompetitiva,OperacaoIdempotente}.cs
│   │   ├── Enums/{SeasonEstado,SerieTipo,SerieEstado,SerieFormato,ModoDraft,PartidaEstado,DecisaoPicksRemake,MotivoTerminoPartida}.cs
│   │   ├── Events/Competitive/*.cs
│   │   ├── Rules/{SeasonRules,SerieRules,FearlessRules}.cs
│   │   └── Repositories/{ICalendarioCompetitivoRepository,ICompeticaoRepository,IEventoCompetitivoRepository,ISerieRepository,ICompetitiveAuditRepository,IIdempotencyRepository}.cs
│   ├── RinhaDasLendas.Application/
│   │   ├── Commands/{Seasons,Competicoes,Eventos,Series,Partidas}/*.cs
│   │   ├── Queries/{Seasons,Competicoes,Eventos,Series,Partidas}/*.cs
│   │   ├── Handlers/{Seasons,Competicoes,Eventos,Series,Partidas}/*.cs
│   │   ├── Dtos/{Season,Competicao,Evento,Serie,Partida,CompetitiveAudit}Dtos.cs
│   │   ├── Validators/{Season,Competicao,Evento,Serie,Partida}*.cs
│   │   └── Interfaces/{ICurrentActor,IIdempotencyService}.cs
│   ├── RinhaDasLendas.Infrastructure/
│   │   ├── Identity/AuthService.cs
│   │   ├── Messages/Messages*.resx
│   │   ├── Persistence/RinhaDasLendasDbContext.cs
│   │   ├── Migrations/*
│   │   └── Repositories/{CalendarioCompetitivo,Competicao,EventoCompetitivo,Serie,CompetitiveAudit,Idempotency}Repository.cs
│   └── RinhaDasLendas.Api/
│       ├── Controllers/{Temporadas,Competicoes,Eventos,Series,Partidas}Controller.cs
│       ├── Middleware/*
│       └── Program.cs
└── tests/RinhaDasLendas.Tests/
    ├── Domain/{Season,Competicao,EventoCompetitivo,Serie,Fearless}Tests.cs
    ├── Application/{CompetitiveFoundationValidator,CompetitiveFoundationHandler,SeasonSelectionContract}Tests.cs
    ├── Integration/{CompetitiveFoundationBehavior,CompetitiveFoundationMigration,CompetitiveFoundationApi}Tests.cs
    └── Security/CompetitiveFoundationAuthorizationTests.cs

FrontEnd/src/
├── views/{SeasonsView,SeriesView,SeriesDetailView,MatchDetailView}.vue
├── components/competitive/
│   ├── {SeasonScopeSelector,SeasonFormDrawer,SeasonTransitionDialog,CompetitionPanel}.vue
│   ├── {SeriesFormDrawer,SeriesCard,SeriesScoreboard,FearlessPanel}.vue
│   └── {MatchOperationPanel,CompetitiveAuditPanel}.vue
├── services/{seasons,competitions,events,series,matches}.ts
├── types/{season,competition,event,series,match}.ts
├── constants/{permissions,appRoutes}.ts
├── router/index.ts
├── i18n/{i18n.spec.ts,locales/{pt,en}.json}
└── styles/main.css
```

**Structure Decision**: Preservar os quatro projetos backend e o frontend existentes, adicionando módulos por conceito dentro das pastas atuais. `Serie` protege Partidas mínimas, placar e Fearless como um único agregado transacional; Season/Calendário, Competição e Evento permanecem raízes menores. A configuração EF pode ser extraída para classes específicas se o `DbContext` deixar de permanecer legível, mas não haverá refatoração ampla de entidades existentes. `DraftMontagem`, `Time` e `Jogador` não mudam de significado; a feature apenas consulta suas referências e captura snapshots. No frontend, views orquestram serviços e filtros, enquanto componentes recebem DTOs/capabilities e emitem intenções.

## Complexity Tracking

Nenhuma violação constitucional requer justificativa.
