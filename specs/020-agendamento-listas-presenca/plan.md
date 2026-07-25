# Implementation Plan: Agendamento Recorrente de Listas de Presença

**Branch**: `feature/020-agendamento-listas-presenca` | **Date**: 2026-07-23 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/020-agendamento-listas-presenca/spec.md`, approved design from `/docs/superpowers/specs/2026-07-23-agendamento-listas-presenca-design.md` and approved implementation outline from `/docs/superpowers/plans/2026-07-23-agendamento-listas-presenca.md`

## Summary

Permitir que Moderador+ mantenha agendas semanais em `/configuracoes`, com horários no fuso `America/Sao_Paulo`, para criar atomicamente uma ocorrência, um `DraftMontagem` com times de cinco e uma publicação de presença pendente por agenda/data. O backend persiste recorrência, auditoria, `UltimaDataAvaliada` e claim expirável; um `BackgroundService` apenas dispara o caso de uso MediatR, o frontend consome os contratos sem conter regra de recorrência e o bot recebe os drafts agendados pelo polling já existente, sem endpoint novo.

## Technical Context

**Language/Version**: .NET 10 e C# no backend; Vue 3.5 e TypeScript 5.9 no frontend; Node.js e TypeScript no bot

**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core, PostgreSQL, MediatR, FluentValidation, Vue I18n, Vitest, Vue Test Utils e discord.js

**Storage**: PostgreSQL relacional com UUID, FKs explícitas, snake_case, migration EF Core, constraint única por agenda/data e claim persistido

**Testing**: xUnit, FluentAssertions, Moq, testes PostgreSQL de integração, Vitest, Vue Test Utils e testes do bot

**Target Platform**: API e scheduler no host .NET existente; navegadores desktop e mobile; processo Node.js existente do bot Discord

**Project Type**: aplicação web monolítica modular em monorepositório, com backend, frontend e adaptador Discord separados

**Performance Goals**: ciclo padrão a cada 30 segundos sem sobreposição local; aquisição concorrente determinística; recuperação de todas as datas pendentes sem horizonte arbitrário; interface sem atraso perceptível para o volume interno

**Constraints**: `America/Sao_Paulo`; publicação e encerramento no mesmo dia; encerramento posterior; exatamente uma ocorrência/draft/publicação por agenda/data; claim de cinco minutos; sem Quartz/Hangfire; sem regra de recorrência no frontend ou bot; sem draft quando Discord estiver indisponível; i18n PT/EN integral

**Scale/Scope**: uso interno, quatro novas tabelas, oito endpoints administrativos paginados quando listam recursos, uma seção em `/configuracoes`, um ciclo periódico e regressão do polling existente do bot

## Constitution Check

*GATE: Must pass before implementation and be re-checked after the design represented by this plan.*

| Gate | Status | Evidence |
|------|--------|----------|
| MVP e valor real | PASS | Automatiza uma operação recorrente já suportada manualmente, sem calendário genérico, cron arbitrário ou parâmetros extras de draft. |
| Uso interno e simplicidade | PASS | Um serviço periódico no processo existente e quatro tabelas relacionais atendem o volume interno sem microsserviço ou scheduler externo. |
| Integrações não bloqueantes | PASS | Discord indisponível gera ocorrência `Bloqueada` sem draft invisível; a configuração pode voltar dentro da janela e o fluxo manual existente permanece disponível. |
| Camadas e DDD | PASS | `AgendamentoPresenca` e ocorrências guardam invariantes no Domain; Application coordena CQRS; Infrastructure persiste; API apenas autoriza e despacha. |
| Regras críticas no backend | PASS | Timezone, recorrência, deduplicação, autorização, recuperação e auditoria não dependem do frontend ou bot. |
| Persistência íntegra | PASS | UUID, FKs, snake_case, constraints únicas, claim expirável, arquivamento lógico e migration protegem integridade mesmo sob concorrência. |
| Segurança | PASS | Todos os endpoints usam `CanManageDrafts`; `CanManageUsers` continua isolando configuração sensível; autoria vem do JWT e DTOs omitem dados operacionais. |
| Internacionalização | PASS | Frontend usa `pt.json`/`en.json`; backend usa resources PT-BR/en-US; códigos públicos substituem detalhes técnicos. |
| Testabilidade | PASS | O plano exige RED antes de cada implementação e cobre domínio, validators, handlers, PostgreSQL, concorrência, serviço, API, frontend, bot e browser. |
| Observabilidade segura | PASS | Métricas usam apenas contagens, status e códigos estáveis; logs excluem nomes, observações, usuários e dados Discord. |

**Post-design re-check**: PASS. O modelo e os contratos abaixo mantêm as fronteiras constitucionais, preservam o fluxo manual e não introduzem dependência externa ou complexidade sem justificativa.

## Design Decisions

### Domínio E Persistência

- Criar o agregado `AgendamentoPresenca`, as entidades `AgendamentoPresencaDiaSemana`, `OcorrenciaAgendamentoPresenca` e `HistoricoAgendamentoPresenca` e os enums `DiaSemanaIso`, `AgendamentoPresencaStatus`, `OcorrenciaAgendamentoPresencaStatus` e `AgendamentoPresencaAcao` exatamente como definidos em [data-model.md](./data-model.md).
- Persistir dias em linhas próprias e proteger unicidade por `agendamento_presenca_id + dia_semana`; proteger ocorrências por `agendamento_presenca_id + data_local`.
- Persistir todos os enums como `smallint`, aplicar índice `UNIQUE` parcial em `draft_montagem_id WHERE draft_montagem_id IS NOT NULL` e registrar auditoria somente em `campos_alterados varchar(200)` com nomes estáveis separados por vírgula, nunca valores.
- Adicionar `claim_id` e `claim_expires_at` à ocorrência. Após o advisory lock, `TryClaimOccurrenceAsync` usa `clock_timestamp()` do PostgreSQL para validar a janela e persistir expiração de cinco minutos a partir do relógio do banco.
- Criar draft, publicação pendente e conclusão da ocorrência na mesma transação. Crash antes do commit não confirma estado; commit impede novo draft pela ocorrência única.
- Capturar `NomeSnapshot`/`ObservacaoSnapshot` no `INSERT` atômico de qualquer ocorrência e usar exclusivamente esses snapshots em retomadas e drafts.
- Exigir snapshots explícitos nas factories de ocorrência; normalizar e validar nome entre 3-100 caracteres e observação com até 500 caracteres no Domain.
- Implementar exclusão como status `Arquivado` e FKs restritas para preservar auditoria e drafts existentes.

### Tempo E Recuperação

- Implementar `IAgendamentoPresencaTimeZone` com `TimeZoneInfo` e identificador IANA `America/Sao_Paulo`; converter somente os instantes calculados para UTC.
- `UltimaDataAvaliada` representa a última data local totalmente classificada. Para cada agenda candidata, percorrer cada data posterior ao marcador até hoje, inclusive depois de múltiplos dias indisponíveis.
- Na criação, calcular `UltimaDataAvaliada` pela hora local: dia anterior se agora local for menor ou igual à publicação; data local atual somente se for maior. Na reativação, aplicar `max(marcador atual, data calculada)` para nunca retroceder.
- Data não selecionada pode avançar o marcador. Data selecionada avança somente após ocorrência confirmada, bloqueada/perdida persistida ou classificação equivalente concluída; o dia atual antes da publicação permanece pendente.
- A fronteira é única: somente `AtivadoEm > PublicacaoPrevistaEm` impede a ocorrência do mesmo dia; igualdade permanece elegível. Agenda que permaneceu ativa pode criar atrasado antes do encerramento; depois dele registra `Perdida` sem draft.
- Em fase independente de cada ciclo, consultar `ListBlockedAsync(agora)`, sem depender da varredura posterior a `UltimaDataAvaliada`: após encerramento marcar `Perdida`; com janela aberta e configuração restaurada readquirir claim e concluir com draft; se a configuração continuar ausente manter `Bloqueada`.
- Horário local inválido ou ambíguo vira `Falha` com `MV096`, sem ajuste silencioso e sem draft.
- Persistir `Falha/MV096` idempotente antes do marcador. Como a janela é non-null, somente esse estado terminal usa a derivação determinística do PostgreSQL para registrar os instantes locais inválidos/ambíguos; ela nunca participa da elegibilidade de draft.
- Obter `ISystemClock.UtcNow` novamente antes de cada classificação, claim, conclusão e marcador; um claim que cruza o encerramento vira `Perdida` com o mesmo instante em seu CAS.
- Limitar por ciclo a quantidade de bloqueadas, agendas e datas por agenda, sem horizonte por idade. O marcador e a ordenação persistida fornecem continuação e progresso eventual.
- Listar somente agendas cuja próxima data selecionada posterior ao marcador já tenha publicação acionável. Calcular essa data em no máximo sete dias por aritmética ISO sobre as linhas de dias, sem expandir o histórico. Propagar cursor de agenda entre ciclos pelo hosted service, com ordenação circular, para que falhas persistentes no início do lote não causem starvation.
- Ao persistir `Falha/MV096`, comparar atomicamente `xmin`, dia e horários observados; configuração concorrente invalida a escrita e impede avanço do marcador.
- Após adquirir o lock da ocorrência, validar encerramento e expiração do claim com `clock_timestamp()` do PostgreSQL antes de criar draft/publicação e novamente no CAS final; o instante injetado continua sendo usado apenas nos timestamps persistidos.
- Manter limpeza do change tracker encapsulada no repositório em toda exceção de persistência antes de traduzir concorrência ou relançar; Application recarrega cada candidata rastreada isoladamente e não controla tracking.
- Exceção transitória ao consultar configuração Discord não equivale a configuração ausente: diagnosticar por porta segura, não criar `Bloqueada`, não avançar marcador e tentar novamente; datas já encerradas continuam classificáveis como `Perdida`.

### CQRS, API E Autorização

- Separar commands de criar, editar, pausar, reativar, arquivar e processar das queries de listar, detalhar e listar ocorrências.
- Listar agendas e ocorrências com `page`/`pageSize` e `PaginatedResponseDto<T>`, incluindo `TotalItems` e `TotalPages`.
- Aplicar a ordenação paginada total de agendas no query/repositório: `ProximaExecucaoEm ASC NULLS LAST, Nome ASC, Id ASC`; o frontend preserva a ordem recebida e nunca reordena páginas concatenadas.
- Usar `IAgendamentoPresencaRepository` para operações atômicas e projeções; o contrato inclui `ListAsync(bool includePaused, int page, int pageSize, CancellationToken ct)`, `CountAsync(bool includePaused, CancellationToken ct)`, `ListOccurrencesAsync(Guid agendaId, int page, int pageSize, CancellationToken ct)`, `CountOccurrencesAsync(Guid agendaId, CancellationToken ct)` e `ListBlockedAsync(DateTimeOffset now, CancellationToken ct)`; handlers coordenam relógio, timezone, configuração Discord e domínio.
- Proteger a base `/api/v1/discord/agendamentos-presenca` com JWT e `AuthPermissions.CanManageDrafts`; obter `ResponsavelUsuarioId` do claim autenticado.
- Usar somente DTOs do contrato [backend-api.md](./contracts/backend-api.md), respostas de erro padrão e resources para `MV089` a `MV100`.
- Preservar `CanManageUsers` na configuração de guild/canais/token/ativação; agendas nunca retornam esses campos.

### Serviço Periódico E Observabilidade

- `AgendamentoPresencaExecutionService` cria escopo e envia `ProcessarAgendamentosPresencaDevidosCommand(clock.UtcNow)`; não contém EF, recorrência ou regra Discord.
- Usar `PeriodicTimer` com `PresenceSchedule:IntervalSeconds`, default 30, sem iniciar ciclo antes do anterior terminar e respeitando cancelamento.
- Uma falha por agenda é isolada. Testes RED específicos devem preceder a implementação das métricas de avaliadas, criadas, bloqueadas, perdidas, falhas, conflitos e duração, aceitando apenas tags de status/código estável.
- `Avaliadas` conta cada agenda candidata uma vez; bloqueadas reavaliadas não incrementam esse contador. Diagnóstico técnico usa etapa enum fechada, tipo da exceção e código estável, sem dados de agenda/usuário/Discord.

### Frontend E Bot

- Compor `PresenceScheduleSection`, `PresenceScheduleFormDialog`, `PresenceScheduleConfirmDialog` e `PresenceScheduleOccurrenceHistoryDialog` em `SettingsView.vue` conforme [frontend-ui.md](./contracts/frontend-ui.md).
- Mostrar agendas por `CanManageDrafts` e configuração sensível apenas por `CanManageUsers`; não condicionar as agendas à permissão administrativa mais ampla.
- Consumir `PaginatedResponse<PresenceScheduleSummary>` na central, oferecer paginação/carregar mais localizado e usar `listPresenceScheduleOccurrences` no painel/modal acessível acionado por `Ver histórico`.
- Reutilizar componentes e tokens existentes, cards responsivos e formulários verticais; garantir teclado, foco, `Escape`, toque e ausência de overflow em 320px.
- Manter textos em `settings.presenceSchedules` com paridade PT/EN; datas e dias usam locale ativo.
- O bot não recebe endpoint, DTO ou regra de agenda. Conforme [discord-bot.md](./contracts/discord-bot.md), um draft agendado é apenas outro draft acionável no polling existente.

### Release E Documentação

- Adicionar `2026.07.2` como release mais recente, ID `presence-scheduling-2026-07`, data `2026-07-23`, categorias `feature`/`improvement` e áreas `drafts`/`discord`.
- Documentar operação, bloqueio, recuperação, perda, publicação e métricas sem expor locks, claims ou segredos em conteúdo de produto.

## Project Structure

### Documentation (this feature)

```text
specs/020-agendamento-listas-presenca/
├── spec.md
├── plan.md
├── data-model.md
├── contracts/
│   ├── backend-api.md
│   ├── frontend-ui.md
│   └── discord-bot.md
├── tasks.md
└── verification-report.md
```

### Source Code (repository root)

```text
BackEnd/
├── src/
│   ├── RinhaDasLendas.Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   ├── Models/
│   │   ├── Repositories/
│   │   └── Constants/MessageCodes.cs
│   ├── RinhaDasLendas.Application/
│   │   ├── Commands/AgendamentosPresenca/
│   │   ├── Queries/AgendamentosPresenca/
│   │   ├── Handlers/AgendamentosPresenca/
│   │   ├── Dtos/AgendamentoPresencaDtos.cs
│   │   ├── Validators/AgendamentoPresencaRequestValidator.cs
│   │   └── Interfaces/
│   ├── RinhaDasLendas.Infrastructure/
│   │   ├── Persistence/
│   │   ├── Repositories/
│   │   ├── Time/
│   │   ├── Messages/
│   │   └── Migrations/
│   └── RinhaDasLendas.Api/
│       ├── Controllers/AgendamentosPresencaController.cs
│       ├── Services/AgendamentoPresencaExecutionService.cs
│       └── Observability/AgendamentoPresencaMetrics.cs
└── tests/RinhaDasLendas.Tests/
    ├── Domain/
    ├── AgendamentosPresenca/
    ├── Services/
    └── Integration/

FrontEnd/src/
├── types/presenceSchedule.ts
├── services/presenceSchedules.ts
├── components/settings/
│   ├── PresenceScheduleSection.vue
│   ├── PresenceScheduleFormDialog.vue
│   ├── PresenceScheduleConfirmDialog.vue
│   └── PresenceScheduleOccurrenceHistoryDialog.vue
├── views/SettingsView.vue
├── i18n/locales/{pt,en}.json
└── styles/main.css

discord-bot/src/modules/drafts/draftInteractions.spec.ts
docs/domain/{DRAFT_DISCORD_OPERATIONS,AGENDAMENTO_LISTAS_PRESENCA}.md
```

**Structure Decision**: manter os projetos e camadas existentes. O domínio não conhece infraestrutura; Application define casos de uso e interfaces; Infrastructure implementa persistência/timezone; API hospeda controller e ciclo; frontend mantém somente interação; bot permanece adaptador do protocolo de publicação.

## Verification Strategy

1. Confirmar RED e GREEN para invariantes, transições, normalização, idempotência e histórico do domínio.
2. Validar PostgreSQL real para mappings, constraints, claim expirável, dois processadores, rollback e conclusão transacional.
3. Validar handlers, validators e matriz HTTP com anônimo, Jogador, Moderador e Admin, incluindo duas páginas com empate, pausadas, ordenação total, paginação/count e ausência de campos operacionais.
4. Simular relógio antes, exatamente no instante e depois da publicação para validar `AtivadoEm > PublicacaoPrevistaEm` e inicialização/reativação de `UltimaDataAvaliada`, além de indisponibilidade de três dias.
5. Testar a fase independente de bloqueadas com marcador já avançado, cobrindo permanência, readquisição/criação e perda após encerramento.
6. Testar serviço periódico sem sobreposição, cancelamento, isolamento de falha e métricas com contadores e tags seguras.
7. Testar frontend para paginação de agendas, histórico paginado, permissões, CRUD, estados, i18n, foco, teclado, toque, 320px e paridade PT/EN.
8. Comprovar por regressão que o bot processa draft agendado uma vez pelo polling e claim existentes, sem código de produção quando desnecessário.
9. Executar suites, builds, lint, migration, browser real, auditorias de segurança/i18n e `git diff --check` somente na fase de implementação aprovada.

O resultado consolidado, os comandos reproduzíveis e os riscos remanescentes estão em [verification-report.md](./verification-report.md).

## Complexity Tracking

Nenhuma violação constitucional ou complexidade adicional a justificar.
