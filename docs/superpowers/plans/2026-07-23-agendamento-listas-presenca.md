# Agendamento Recorrente De Listas De Presença Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Permitir que Moderador+ configure agendas semanais em `/configuracoes` para criar drafts com presença aberta e publicar suas listas automaticamente no Discord, sem duplicações.

**Architecture:** O backend persiste agendas e ocorrências, calcula horários em `America/Sao_Paulo` e cria ocorrência, draft e publicação de forma atômica. Um `BackgroundService` apenas dispara o caso de uso MediatR; o bot continua consumindo o protocolo atual de polling e claims, enquanto o frontend oferece a central de automações sem conter regras de recorrência.

**Tech Stack:** .NET 10, ASP.NET Core Web API, EF Core, PostgreSQL, MediatR, FluentValidation, xUnit, FluentAssertions, Moq, Vue 3.5, TypeScript 5.9, Vue I18n, Vitest, Vue Test Utils, discord.js e Node.js/TypeScript.

## Global Constraints

- Seguir Constitution, Specify, Plan, Tasks e Implement; pausar após a Task 1 até `tasks.md` receber aprovação explícita.
- Trabalhar na branch `feature/020-agendamento-listas-presenca`; nunca implementar em `main`.
- Usar `CanManageDrafts` para listar e alterar agendas; não ampliar `CanManageUsers` nem expor configuração sensível a Moderador.
- Backend é a fonte de verdade; frontend e bot não podem implementar regra de recorrência.
- Não adicionar Quartz, Hangfire, nova dependência de timezone, banco alternativo ou cron arbitrário.
- Usar `TimeZoneInfo` com `America/Sao_Paulo`; persistir instantes em UTC e horários recorrentes como hora local.
- Publicação e encerramento ocorrem no mesmo dia, com encerramento estritamente posterior.
- Cada `agenda + data local` produz no máximo uma ocorrência, um draft e uma publicação principal.
- Bot desativado ou configuração Discord incompleta não pode criar draft invisível.
- Pausa, edição e arquivamento não alteram drafts já criados.
- Usar UUID, FKs explícitas, snake_case, modelagem relacional e migration EF Core.
- Domain não depende de EF, PostgreSQL, HTTP, DTOs, Discord SDK ou Application.
- Commands e Queries permanecem separados; controllers só recebem, enviam casos de uso e formatam HTTP.
- Todo input usa FluentValidation e regras de domínio sem duplicação desnecessária.
- Todo texto frontend usa `pt.json` e `en.json`; toda mensagem backend usa recursos `.resx` equivalentes.
- Não registrar nomes, observações, tokens, payloads Discord, IDs de mensagem ou motivos livres em logs/métricas.
- Usar TDD e observar RED antes de cada implementação.
- Commits pequenos e em português brasileiro; não alterar git config nem fazer amend.
- Preservar `docs/prompts/`, `specs/018-importacao-partidas-lcu/` e mudanças não relacionadas.

---

## File Map

### Spec Kit

- Create `specs/020-agendamento-listas-presenca/spec.md`: histórias, requisitos e critérios mensuráveis.
- Create `specs/020-agendamento-listas-presenca/plan.md`: contexto técnico, modelo e Constitution Check.
- Create `specs/020-agendamento-listas-presenca/data-model.md`: tabelas, relacionamentos, constraints e transições.
- Create `specs/020-agendamento-listas-presenca/contracts/backend-api.md`: endpoints e DTOs.
- Create `specs/020-agendamento-listas-presenca/contracts/frontend-ui.md`: estados e interações.
- Create `specs/020-agendamento-listas-presenca/contracts/discord-bot.md`: invariância do protocolo existente.
- Create `specs/020-agendamento-listas-presenca/tasks.md`: tarefas aprováveis e rastreáveis.

### Domain

- Create `BackEnd/src/RinhaDasLendas.Domain/Entities/AgendamentoPresenca.cs`: agregado e invariantes.
- Create `BackEnd/src/RinhaDasLendas.Domain/Entities/AgendamentoPresencaDiaSemana.cs`: relação de dias.
- Create `BackEnd/src/RinhaDasLendas.Domain/Entities/OcorrenciaAgendamentoPresenca.cs`: ciclo de execução.
- Create `BackEnd/src/RinhaDasLendas.Domain/Entities/HistoricoAgendamentoPresenca.cs`: auditoria.
- Create `BackEnd/src/RinhaDasLendas.Domain/Enums/AgendamentoPresencaStatus.cs`.
- Create `BackEnd/src/RinhaDasLendas.Domain/Enums/OcorrenciaAgendamentoPresencaStatus.cs`.
- Create `BackEnd/src/RinhaDasLendas.Domain/Enums/AgendamentoPresencaAcao.cs`.
- Create `BackEnd/src/RinhaDasLendas.Domain/Enums/DiaSemanaIso.cs`.
- Create `BackEnd/src/RinhaDasLendas.Domain/Models/AgendamentoPresencaOcorrenciaClaim.cs`.
- Create `BackEnd/src/RinhaDasLendas.Domain/Repositories/IAgendamentoPresencaRepository.cs`.
- Create `BackEnd/tests/RinhaDasLendas.Tests/Domain/AgendamentoPresencaTests.cs`.

### Application E API

- Create `BackEnd/src/RinhaDasLendas.Application/Dtos/AgendamentoPresencaDtos.cs`: requests e projections.
- Create `BackEnd/src/RinhaDasLendas.Application/Commands/AgendamentosPresenca/`: criar, editar, pausar, reativar, arquivar e processar.
- Create `BackEnd/src/RinhaDasLendas.Application/Queries/AgendamentosPresenca/`: listar, detalhar e listar ocorrências.
- Create `BackEnd/src/RinhaDasLendas.Application/Handlers/AgendamentosPresenca/`: casos de uso.
- Create `BackEnd/src/RinhaDasLendas.Application/Validators/AgendamentoPresencaRequestValidator.cs`.
- Create `BackEnd/src/RinhaDasLendas.Application/Interfaces/ISystemClock.cs`.
- Create `BackEnd/src/RinhaDasLendas.Application/Interfaces/IAgendamentoPresencaTimeZone.cs`.
- Create `BackEnd/src/RinhaDasLendas.Application/Interfaces/IAgendamentoPresencaMetrics.cs`.
- Create `BackEnd/src/RinhaDasLendas.Api/Controllers/AgendamentosPresencaController.cs`.
- Create `BackEnd/src/RinhaDasLendas.Api/Services/AgendamentoPresencaExecutionService.cs`.
- Create `BackEnd/src/RinhaDasLendas.Api/Services/SystemClock.cs`.
- Create `BackEnd/src/RinhaDasLendas.Api/Observability/AgendamentoPresencaMetrics.cs`.
- Modify `BackEnd/src/RinhaDasLendas.Api/Program.cs`: DI, hosted service e métricas.
- Modify `BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs`: códigos `MV089` a `MV100`.
- Modify `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.resx`.
- Modify `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.pt-BR.resx`.
- Modify `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.en-US.resx`.

### Infrastructure

- Create `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/AgendamentoPresencaRepository.cs`.
- Create `BackEnd/src/RinhaDasLendas.Infrastructure/Time/SaoPauloAgendamentoPresencaTimeZone.cs`.
- Modify `BackEnd/src/RinhaDasLendas.Infrastructure/Persistence/RinhaDasLendasDbContext.cs`: DbSets e mappings.
- Modify `BackEnd/src/RinhaDasLendas.Infrastructure/DependencyInjection.cs`: repositório e timezone.
- Create migration `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/20260723090000_AddAgendamentosPresenca.cs`.
- Create migration metadata `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/20260723090000_AddAgendamentosPresenca.Designer.cs`.

### Tests Backend

- Create `BackEnd/tests/RinhaDasLendas.Tests/AgendamentosPresenca/AgendamentoPresencaValidatorTests.cs`.
- Create `BackEnd/tests/RinhaDasLendas.Tests/AgendamentosPresenca/AgendamentoPresencaHandlersTests.cs`.
- Create `BackEnd/tests/RinhaDasLendas.Tests/Services/AgendamentoPresencaExecutionServiceTests.cs`.
- Create `BackEnd/tests/RinhaDasLendas.Tests/Integration/AgendamentoPresencaBehaviorIntegrationTests.cs`.
- Modify `BackEnd/tests/RinhaDasLendas.Tests/Infrastructure/SecurityApiFactory.cs`: adicionar clientes autenticados Moderador e Jogador.

### Frontend

- Create `FrontEnd/src/types/presenceSchedule.ts`.
- Create `FrontEnd/src/services/presenceSchedules.ts`.
- Create `FrontEnd/src/services/presenceSchedules.spec.ts`.
- Create `FrontEnd/src/components/settings/PresenceScheduleSection.vue`.
- Create `FrontEnd/src/components/settings/PresenceScheduleSection.spec.ts`.
- Create `FrontEnd/src/components/settings/PresenceScheduleFormDialog.vue`.
- Create `FrontEnd/src/components/settings/PresenceScheduleFormDialog.spec.ts`.
- Create `FrontEnd/src/components/settings/PresenceScheduleConfirmDialog.vue`.
- Create `FrontEnd/src/components/settings/PresenceScheduleConfirmDialog.spec.ts`.
- Modify `FrontEnd/src/views/SettingsView.vue`.
- Create `FrontEnd/src/views/SettingsView.spec.ts`.
- Modify `FrontEnd/src/i18n/locales/pt.json` e `en.json`.
- Modify `FrontEnd/src/styles/main.css`.

### Bot, Changelog E Docs

- Modify `discord-bot/src/modules/drafts/draftInteractions.spec.ts`: regressão do polling com draft agendado.
- Modify `FrontEnd/src/constants/systemUpdates.ts`: release `2026.07.2`.
- Modify testes do histórico e catálogos PT/EN.
- Modify `docs/domain/DRAFT_DISCORD_OPERATIONS.md`.
- Create `docs/domain/AGENDAMENTO_LISTAS_PRESENCA.md`.

---

### Task 1: Materializar E Aprovar Os Artefatos Spec Kit

**Files:**
- Create: `specs/020-agendamento-listas-presenca/spec.md`
- Create: `specs/020-agendamento-listas-presenca/plan.md`
- Create: `specs/020-agendamento-listas-presenca/data-model.md`
- Create: `specs/020-agendamento-listas-presenca/contracts/backend-api.md`
- Create: `specs/020-agendamento-listas-presenca/contracts/frontend-ui.md`
- Create: `specs/020-agendamento-listas-presenca/contracts/discord-bot.md`
- Create: `specs/020-agendamento-listas-presenca/tasks.md`
- Modify: `.specify/feature.json`
- Modify: `AGENTS.md` somente dentro do bloco Spec Kit gerenciado.

**Interfaces:**
- Consumes: design aprovado em `docs/superpowers/specs/2026-07-23-agendamento-listas-presenca-design.md` e este plano.
- Produces: requisitos formais, contratos e tarefas aprovadas antes do código.

- [ ] **Step 1: Criar a especificação**

Registrar cinco histórias independentes:

```markdown
1. Moderador+ cria e mantém agenda semanal.
2. Sistema cria e publica ocorrência exatamente uma vez.
3. Sistema recupera execução atrasada dentro da janela.
4. Moderador+ acompanha próxima execução e resultados recentes.
5. Usuário sem permissão não acessa agenda nem dados operacionais.
```

Fixar requisitos para `America/Sao_Paulo`, mesmo dia, times de cinco, arquivamento lógico, `CanManageDrafts`, configuração sensível separada, bloqueio sem Discord, concorrência, auditoria, i18n e release `2026.07.2`.

- [ ] **Step 2: Criar plano, data model e contratos**

Usar exatamente os nomes de entidades, enums, rotas e DTOs definidos neste plano. O contrato do bot deve declarar que nenhum endpoint novo é consumido pelo bot e que drafts agendados entram no polling existente.

- [ ] **Step 3: Gerar tarefas TDD ordenadas**

Criar fases equivalentes às Tasks 2-8 deste documento. Cada implementação deve ter teste RED anterior, e tarefas paralelas só podem tocar arquivos distintos.

- [ ] **Step 4: Atualizar contexto Spec Kit**

```bash
.specify/extensions/agent-context/scripts/bash/update-agent-context.sh specs/020-agendamento-listas-presenca/plan.md
.specify/scripts/bash/check-prerequisites.sh --json --require-tasks --include-tasks
```

Antes dos comandos, atualizar `.specify/feature.json` com `apply_patch` para o conteúdo exato:

```json
{
  "feature_directory": "specs/020-agendamento-listas-presenca"
}
```

Expected: `FEATURE_DIR` aponta para feature 020 e `AVAILABLE_DOCS` inclui os documentos criados.

- [ ] **Step 5: Commitar cada fase documental**

```bash
git add specs/020-agendamento-listas-presenca/spec.md
git commit -m "docs: especificar agendamento de listas de presença"

git add specs/020-agendamento-listas-presenca/plan.md specs/020-agendamento-listas-presenca/data-model.md specs/020-agendamento-listas-presenca/contracts .specify/feature.json AGENTS.md
git commit -m "docs: planejar agendamento de listas de presença"

git add specs/020-agendamento-listas-presenca/tasks.md
git commit -m "docs: organizar tarefas do agendamento de presenças"
```

- [ ] **Step 6: Parar no gate de aprovação**

Não iniciar Task 2 até o usuário aprovar explicitamente `tasks.md`.

---

### Task 2: Implementar O Domínio De Agendas E Ocorrências

**Files:**
- Create: `BackEnd/src/RinhaDasLendas.Domain/Entities/AgendamentoPresenca.cs`
- Create: `BackEnd/src/RinhaDasLendas.Domain/Entities/AgendamentoPresencaDiaSemana.cs`
- Create: `BackEnd/src/RinhaDasLendas.Domain/Entities/OcorrenciaAgendamentoPresenca.cs`
- Create: `BackEnd/src/RinhaDasLendas.Domain/Entities/HistoricoAgendamentoPresenca.cs`
- Create: `BackEnd/src/RinhaDasLendas.Domain/Enums/AgendamentoPresencaStatus.cs`
- Create: `BackEnd/src/RinhaDasLendas.Domain/Enums/OcorrenciaAgendamentoPresencaStatus.cs`
- Create: `BackEnd/src/RinhaDasLendas.Domain/Enums/AgendamentoPresencaAcao.cs`
- Create: `BackEnd/src/RinhaDasLendas.Domain/Enums/DiaSemanaIso.cs`
- Create: `BackEnd/tests/RinhaDasLendas.Tests/Domain/AgendamentoPresencaTests.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs`

**Interfaces:**
- Consumes: somente tipos BCL (`DateOnly`, `TimeOnly`, `DateTimeOffset`, `Guid`).
- Produces:

```csharp
public enum DiaSemanaIso { Segunda = 1, Terca = 2, Quarta = 3, Quinta = 4, Sexta = 5, Sabado = 6, Domingo = 7 }
public enum AgendamentoPresencaStatus { Ativo, Pausado, Arquivado }
public enum OcorrenciaAgendamentoPresencaStatus { Processando, Bloqueada, Criada, Perdida, Falha }
public enum AgendamentoPresencaAcao { Criado, Editado, Pausado, Reativado, Arquivado }

public sealed class AgendamentoPresenca
{
    public DateOnly UltimaDataAvaliada { get; private set; }
    public AgendamentoPresenca(string nome, string? observacao, TimeOnly publicacao, TimeOnly encerramento,
        IReadOnlyCollection<DiaSemanaIso> dias, DateOnly ultimaDataAvaliada,
        Guid responsavelId, DateTimeOffset agora);
    public void Editar(string nome, string? observacao, TimeOnly publicacao, TimeOnly encerramento,
        IReadOnlyCollection<DiaSemanaIso> dias, Guid responsavelId, DateTimeOffset agora);
    public void Pausar(Guid responsavelId, DateTimeOffset agora);
    public void Reativar(Guid responsavelId, DateTimeOffset agora);
    public void Arquivar(Guid responsavelId, DateTimeOffset agora);
    public void MarcarDataAvaliada(DateOnly data, DateTimeOffset agora);
    public bool OcorreEm(DateOnly data);
}

public sealed class OcorrenciaAgendamentoPresenca
{
    public static OcorrenciaAgendamentoPresenca Processando(Guid agendaId, DateOnly dataLocal,
        DateTimeOffset publicacao, DateTimeOffset encerramento, DateTimeOffset agora);
    public static OcorrenciaAgendamentoPresenca Bloqueada(Guid agendaId, DateOnly dataLocal,
        DateTimeOffset publicacao, DateTimeOffset encerramento, string codigo, DateTimeOffset agora);
    public void IniciarProcessamento(DateTimeOffset agora);
    public void MarcarCriada(Guid draftId, DateTimeOffset agora);
    public void MarcarPerdida(string codigo, DateTimeOffset agora);
    public void MarcarFalha(string codigo, DateTimeOffset agora);
}
```

- [ ] **Step 1: Escrever testes RED das invariantes**

```csharp
[Fact]
public void DeveExigirAoMenosUmDia()
{
    var act = () => new AgendamentoPresenca("Rinha", null, new TimeOnly(18, 0), new TimeOnly(20, 0), [], Guid.NewGuid(), Agora);
    act.Should().Throw<DomainException>().WithMessage(MessageCodes.PresenceScheduleDayRequired);
}

[Fact]
public void DeveExigirEncerramentoPosterior()
{
    var act = () => Criar(publicacao: new TimeOnly(20, 0), encerramento: new TimeOnly(18, 0));
    act.Should().Throw<DomainException>().WithMessage(MessageCodes.PresenceScheduleTimeRangeInvalid);
}

[Fact]
public void ArquivadaNaoPodeSerEditada()
{
    var agenda = Criar();
    agenda.Arquivar(Responsavel, Agora);
    var act = () => agenda.Editar("Outro", null, new(18, 0), new(20, 0), [DiaSemanaIso.Sexta], Responsavel, Agora.AddMinutes(1));
    act.Should().Throw<DomainException>().WithMessage(MessageCodes.PresenceScheduleArchived);
}
```

Adicionar testes para normalização, limites, dias duplicados, `OcorreEm`, histórico, pausa/reativação idempotentes e todas as transições da ocorrência.

- [ ] **Step 2: Confirmar RED**

Run: `dotnet test BackEnd/tests/RinhaDasLendas.Tests/RinhaDasLendas.Tests.csproj --filter FullyQualifiedName~AgendamentoPresencaTests`

Expected: FAIL porque os tipos ainda não existem.

- [ ] **Step 3: Adicionar códigos de domínio**

```csharp
public const string PresenceScheduleNameRequired = "MV089";
public const string PresenceScheduleNameLengthInvalid = "MV090";
public const string PresenceScheduleObservationTooLong = "MV091";
public const string PresenceScheduleDayRequired = "MV092";
public const string PresenceScheduleDayDuplicated = "MV093";
public const string PresenceScheduleTimeRangeInvalid = "MV094";
public const string PresenceScheduleArchived = "MV095";
public const string PresenceScheduleTimeZoneInvalid = "MV096";
public const string PresenceScheduleOccurrenceConflict = "MV097";
public const string PresenceScheduleDiscordUnavailable = "MV098";
public const string PresenceScheduleNotFound = "MV099";
public const string PresenceScheduleWindowExpired = "MV100";
```

- [ ] **Step 4: Implementar entidades e enums**

Implementar os contratos da seção Interfaces. Todos os setters ficam privados, coleções usam backing fields, métodos recebem `agora` explicitamente e `Touch` não usa `DateTimeOffset.UtcNow`.

- [ ] **Step 5: Confirmar GREEN**

Run: mesmo comando do Step 2.

Expected: todos os testes de domínio aprovados.

- [ ] **Step 6: Commitar domínio**

```bash
git add BackEnd/src/RinhaDasLendas.Domain BackEnd/tests/RinhaDasLendas.Tests/Domain/AgendamentoPresencaTests.cs
git commit -m "feat: modelar agendamento recorrente de presenças"
```

---

### Task 3: Persistir Agendas E Deduplicar Ocorrências

**Files:**
- Create: `BackEnd/src/RinhaDasLendas.Domain/Repositories/IAgendamentoPresencaRepository.cs`
- Create: `BackEnd/src/RinhaDasLendas.Domain/Models/AgendamentoPresencaOcorrenciaClaim.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Interfaces/IAgendamentoPresencaTimeZone.cs`
- Create: `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/AgendamentoPresencaRepository.cs`
- Create: `BackEnd/src/RinhaDasLendas.Infrastructure/Time/SaoPauloAgendamentoPresencaTimeZone.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Persistence/RinhaDasLendasDbContext.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/DependencyInjection.cs`
- Create: `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/20260723090000_AddAgendamentosPresenca.cs`
- Create: `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/20260723090000_AddAgendamentosPresenca.Designer.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/RinhaDasLendasDbContextModelSnapshot.cs`
- Create: `BackEnd/tests/RinhaDasLendas.Tests/Integration/AgendamentoPresencaBehaviorIntegrationTests.cs`

**Interfaces:**

```csharp
public interface IAgendamentoPresencaRepository
{
    Task AddAsync(AgendamentoPresenca agenda, CancellationToken ct);
    Task<AgendamentoPresenca?> GetByIdAsync(Guid id, bool tracking, CancellationToken ct);
    Task<IReadOnlyCollection<AgendamentoPresenca>> ListAsync(bool includePaused, CancellationToken ct);
    Task<IReadOnlyCollection<OcorrenciaAgendamentoPresenca>> ListOccurrencesAsync(Guid agendaId, int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyCollection<AgendamentoPresenca>> ListCandidatesAsync(DateOnly throughLocalDate, CancellationToken ct);
    Task<IReadOnlyCollection<OcorrenciaAgendamentoPresenca>> ListBlockedAsync(DateTimeOffset now, CancellationToken ct);
    Task<AgendamentoPresencaOcorrenciaClaim?> TryClaimOccurrenceAsync(Guid agendaId, DateOnly localDate,
        DateTimeOffset publicationAt, DateTimeOffset closureAt, Guid claimId, DateTimeOffset claimExpiresAt,
        DateTimeOffset now, CancellationToken ct);
    Task<bool> TryUpsertBlockedOccurrenceAsync(Guid agendaId, DateOnly localDate,
        DateTimeOffset publicationAt, DateTimeOffset closureAt, string code, DateTimeOffset now, CancellationToken ct);
    Task<bool> TryUpsertMissedOccurrenceAsync(Guid agendaId, DateOnly localDate,
        DateTimeOffset publicationAt, DateTimeOffset closureAt, string code, DateTimeOffset now, CancellationToken ct);
    Task<bool> TryCompleteWithDraftAsync(Guid occurrenceId, Guid claimId, DraftMontagem draft,
        DateTimeOffset now, CancellationToken ct);
    Task<bool> TryMarkFailedAsync(Guid occurrenceId, Guid claimId, string code, DateTimeOffset now, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public sealed record AgendamentoPresencaOcorrenciaClaim(Guid OcorrenciaId, Guid ClaimId, bool Adquirido);

public interface IAgendamentoPresencaTimeZone
{
    DateOnly GetLocalDate(DateTimeOffset instant);
    DateTimeOffset ToUtc(DateOnly date, TimeOnly time);
}
```

Campos `claim_id` e `claim_expires_at` são adicionados à ocorrência para recuperação de processador interrompido. O claim expira em cinco minutos; um novo processador pode retomá-lo enquanto a janela estiver aberta.

- [ ] **Step 1: Escrever testes PostgreSQL RED**

Cobrir:

```csharp
await Task.WhenAll(
    repositoryA.TryClaimOccurrenceAsync(schedule.Id, date, publication, closure, claimA, expiry, now, ct),
    repositoryB.TryClaimOccurrenceAsync(schedule.Id, date, publication, closure, claimB, expiry, now, ct));

var occurrences = await db.OcorrenciasAgendamentosPresenca.Where(x => x.AgendamentoPresencaId == schedule.Id && x.DataLocal == date).ToListAsync();
occurrences.Should().ContainSingle();
```

Adicionar testes para dias relacionais, arquivamento, claim expirado recuperável, claim divergente, conclusão atômica com draft/publicação e rollback por conflito.

- [ ] **Step 2: Confirmar RED**

Run: `dotnet test BackEnd/tests/RinhaDasLendas.Tests/RinhaDasLendas.Tests.csproj --filter FullyQualifiedName~AgendamentoPresencaBehaviorIntegrationTests`

Expected: FAIL por ausência de mappings/repositório.

- [ ] **Step 3: Mapear tabelas e migration**

Mappings obrigatórios:

```text
agendamentos_presenca
agendamentos_presenca_dias_semana UNIQUE (agendamento_presenca_id, dia_semana)
ocorrencias_agendamentos_presenca UNIQUE (agendamento_presenca_id, data_local)
historicos_agendamentos_presenca
```

Usar `time without time zone`, `date`, `timestamp with time zone`, FKs restritas e índices parciais para agendas ativas/ocorrências bloqueadas.

- [ ] **Step 4: Implementar operações atômicas**

Usar advisory lock derivado de `agendaId + dataLocal`, `INSERT ... ON CONFLICT`, claim com expiração e transação para inserir `DraftMontagem` e atualizar ocorrência para `Criada`. O draft deve chamar:

```csharp
var draft = new DraftMontagem(generatedName, schedule.Observacao, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
draft.ConfigurarEncerramentoPresenca(closureAt);
draft.ConfigurarPublicacaoDiscord(discordGuildId, null);
```

- [ ] **Step 5: Confirmar GREEN e migration**

```bash
dotnet test BackEnd/tests/RinhaDasLendas.Tests/RinhaDasLendas.Tests.csproj --filter FullyQualifiedName~AgendamentoPresencaBehaviorIntegrationTests
dotnet ef migrations script --project BackEnd/src/RinhaDasLendas.Infrastructure --startup-project BackEnd/src/RinhaDasLendas.Api --idempotent
```

Expected: concorrência produz uma ocorrência/draft; script EF conclui.

- [ ] **Step 6: Commitar persistência**

```bash
git add BackEnd/src/RinhaDasLendas.Infrastructure BackEnd/src/RinhaDasLendas.Domain/Repositories BackEnd/tests/RinhaDasLendas.Tests/Integration/AgendamentoPresencaBehaviorIntegrationTests.cs
git commit -m "feat: persistir ocorrências agendadas sem duplicação"
```

---

### Task 4: Implementar CQRS, API, Autorização E Recursos

**Files:**
- Create: `BackEnd/src/RinhaDasLendas.Application/Dtos/AgendamentoPresencaDtos.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Commands/AgendamentosPresenca/CreateAgendamentoPresencaCommand.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Commands/AgendamentosPresenca/UpdateAgendamentoPresencaCommand.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Commands/AgendamentosPresenca/PausarAgendamentoPresencaCommand.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Commands/AgendamentosPresenca/ReativarAgendamentoPresencaCommand.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Commands/AgendamentosPresenca/ArquivarAgendamentoPresencaCommand.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Queries/AgendamentosPresenca/ListAgendamentosPresencaQuery.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Queries/AgendamentosPresenca/GetAgendamentoPresencaQuery.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Queries/AgendamentosPresenca/ListOcorrenciasAgendamentoPresencaQuery.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Handlers/AgendamentosPresenca/CreateAgendamentoPresencaCommandHandler.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Handlers/AgendamentosPresenca/UpdateAgendamentoPresencaCommandHandler.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Handlers/AgendamentosPresenca/PausarAgendamentoPresencaCommandHandler.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Handlers/AgendamentosPresenca/ReativarAgendamentoPresencaCommandHandler.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Handlers/AgendamentosPresenca/ArquivarAgendamentoPresencaCommandHandler.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Handlers/AgendamentosPresenca/ListAgendamentosPresencaQueryHandler.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Handlers/AgendamentosPresenca/GetAgendamentoPresencaQueryHandler.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Handlers/AgendamentosPresenca/ListOcorrenciasAgendamentoPresencaQueryHandler.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Validators/AgendamentoPresencaRequestValidator.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Interfaces/ISystemClock.cs`
- Create: `BackEnd/src/RinhaDasLendas.Api/Services/SystemClock.cs`
- Create: `BackEnd/src/RinhaDasLendas.Api/Controllers/AgendamentosPresencaController.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs`
- Modify: `docs/messages/message-catalog.md`
- Modify: `docs/messages/message-codes.md`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.resx`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.pt-BR.resx`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.en-US.resx`
- Create: `BackEnd/tests/RinhaDasLendas.Tests/AgendamentosPresenca/AgendamentoPresencaValidatorTests.cs`
- Create: `BackEnd/tests/RinhaDasLendas.Tests/AgendamentosPresenca/AgendamentoPresencaHandlersTests.cs`
- Modify: `BackEnd/tests/RinhaDasLendas.Tests/Integration/EndpointCoverageIntegrationTests.cs`
- Modify: `BackEnd/tests/RinhaDasLendas.Tests/Infrastructure/SecurityApiFactory.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Program.cs` para registrar `ISystemClock`.

**Interfaces:**

```csharp
public sealed record SaveAgendamentoPresencaRequestDto(
    string Nome,
    string? Observacao,
    IReadOnlyCollection<DiaSemanaIso> DiasSemana,
    TimeOnly HorarioPublicacao,
    TimeOnly HorarioEncerramento);

public sealed record AgendamentoPresencaSummaryDto(
    Guid Id, string Nome, string? Observacao, AgendamentoPresencaStatus Status,
    IReadOnlyCollection<DiaSemanaIso> DiasSemana, TimeOnly HorarioPublicacao,
    TimeOnly HorarioEncerramento, DateTimeOffset? ProximaExecucaoEm,
    OcorrenciaAgendamentoPresencaSummaryDto? UltimaOcorrencia);

public sealed record OcorrenciaAgendamentoPresencaSummaryDto(
    Guid Id, DateOnly DataLocal, DateTimeOffset PublicacaoPrevistaEm,
    DateTimeOffset EncerramentoPrevistoEm, OcorrenciaAgendamentoPresencaStatus Status,
    Guid? DraftMontagemId, string? MessageCode);
```

Commands recebem `ResponsavelUsuarioId` no controller a partir do claim, não no body.

- [ ] **Step 1: Escrever testes RED de validator e handlers**

```csharp
[Theory]
[InlineData("", "MV089")]
public async Task DeveValidarNome(string nome, string code)
{
    var result = await validator.ValidateAsync(new(nome, null, [DiaSemanaIso.Sexta], new(18, 0), new(20, 0)));
    result.Errors.Should().Contain(x => x.ErrorCode == code);
}
```

Cobrir CRUD, autoria, idempotência, arquivado 404, paginação de ocorrências e próxima execução localizada.

- [ ] **Step 2: Escrever matriz HTTP RED**

Testar todos os endpoints com anônimo 401, Jogador 403, Moderador sucesso, payload inválido 400 e agenda inexistente 404. Confirmar que body não aceita autoria e resposta não contém claim/IDs Discord.

- [ ] **Step 3: Confirmar RED**

Run: `dotnet test BackEnd/tests/RinhaDasLendas.Tests/RinhaDasLendas.Tests.csproj --filter "FullyQualifiedName~AgendamentoPresencaValidatorTests|FullyQualifiedName~AgendamentoPresencaHandlersTests|FullyQualifiedName~EndpointCoverageIntegrationTests"`

- [ ] **Step 4: Implementar CQRS e controller**

Controller base `/api/v1/discord/agendamentos-presenca`, `[Authorize(Policy = AuthPermissions.CanManageDrafts)]`, `ISender` e `IMessageProvider`. Usar `CreatedAtAction`, `NoContent`, `ApiErrorResponse.FromCode` e `CurrentUserId()` equivalente ao padrão de AuthController.

- [ ] **Step 5: Adicionar recursos sincronizados**

Adicionar códigos `MV089`-`MV100` ao catálogo, constantes e resources PT-BR/en-US. Nenhuma mensagem visível pode existir como literal em validator/controller/handler.

- [ ] **Step 6: Confirmar GREEN**

Run: mesmo comando do Step 3.

- [ ] **Step 7: Commitar API**

```bash
git add BackEnd/src/RinhaDasLendas.Application BackEnd/src/RinhaDasLendas.Api/Controllers BackEnd/src/RinhaDasLendas.Domain/Constants BackEnd/src/RinhaDasLendas.Infrastructure/Messages BackEnd/tests/RinhaDasLendas.Tests docs/messages
git commit -m "feat: adicionar api de agendamentos de presença"
```

---

### Task 5: Executar Agendas No Background Com Recuperação

**Files:**
- Create: `BackEnd/src/RinhaDasLendas.Application/Interfaces/IAgendamentoPresencaMetrics.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Commands/AgendamentosPresenca/ProcessarAgendamentosPresencaDevidosCommand.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Handlers/AgendamentosPresenca/ProcessarAgendamentosPresencaDevidosCommandHandler.cs`
- Create: `BackEnd/src/RinhaDasLendas.Api/Services/AgendamentoPresencaExecutionService.cs`
- Create: `BackEnd/src/RinhaDasLendas.Api/Observability/AgendamentoPresencaMetrics.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Program.cs`.
- Modify: `BackEnd/src/RinhaDasLendas.Api/appsettings.json`.
- Create: `BackEnd/tests/RinhaDasLendas.Tests/Services/AgendamentoPresencaExecutionServiceTests.cs`
- Modify: `BackEnd/tests/RinhaDasLendas.Tests/Integration/AgendamentoPresencaBehaviorIntegrationTests.cs`

**Interfaces:**

```csharp
public interface ISystemClock { DateTimeOffset UtcNow { get; } }
public sealed record ProcessarAgendamentosPresencaDevidosCommand(DateTimeOffset Agora)
    : IRequest<AgendamentoPresencaCycleResult>;
public sealed record AgendamentoPresencaCycleResult(int Avaliadas, int Criadas, int Bloqueadas, int Perdidas, int Falhas);

public sealed class AgendamentoPresencaExecutionService : BackgroundService
{
    public Task<AgendamentoPresencaCycleResult> RunCycleAsync(CancellationToken cancellationToken);
}
```

- [ ] **Step 1: Escrever testes RED do timezone**

```csharp
var instant = timezone.ToUtc(new DateOnly(2026, 7, 24), new TimeOnly(18, 0));
instant.Should().Be(new DateTimeOffset(2026, 7, 24, 21, 0, 0, TimeSpan.Zero));
timezone.GetLocalDate(instant).Should().Be(new DateOnly(2026, 7, 24));
```

Testar horário inválido/ambíguo por uma implementação de timezone controlada, retornando `MV096` sem draft.

- [ ] **Step 2: Escrever testes RED do ciclo**

Cobrir: antes do horário não executa; dentro da janela cria; recovery cria atrasado; depois da janela perde; bot desativado bloqueia; configuração volta e cria; reativação tardia não cria; falha de uma agenda não interrompe outra; cancelamento encerra serviço.

Adicionar cenário de indisponibilidade por três dias: o handler percorre datas posteriores a `UltimaDataAvaliada`, registra cada dia selecionado vencido como `Perdida` e avança o marcador somente depois das gravações bem-sucedidas.

- [ ] **Step 3: Confirmar RED**

Run: `dotnet test BackEnd/tests/RinhaDasLendas.Tests/RinhaDasLendas.Tests.csproj --filter "FullyQualifiedName~AgendamentoPresencaExecutionServiceTests|FullyQualifiedName~AgendamentoPresencaBehaviorIntegrationTests"`

- [ ] **Step 4: Implementar handler e serviço**

`RunCycleAsync` cria scope e envia `ProcessarAgendamentosPresencaDevidosCommand(clock.UtcNow)`. `ExecuteAsync` usa `PeriodicTimer` com `PresenceSchedule:IntervalSeconds`, default 30, e não inicia novo ciclo antes do anterior terminar.

No handler:

```text
para cada agenda candidata
  percorrer datas após UltimaDataAvaliada até a data local atual
  datas não selecionadas apenas avançam UltimaDataAvaliada
  para cada data selecionada, calcular instantes São Paulo
    ignorar execução se AtivadoEm > publicacao, mas avançar avaliação da data
    se agora >= encerramento: upsert Perdida
    se agora < publicacao: manter a data atual ainda não avaliada
    se configuração ausente/desativada: upsert Bloqueada
    senão adquirir ocorrência e concluir atomicamente com draft
  avançar UltimaDataAvaliada após confirmar todas as ocorrências vencidas da data
```

Gerar o nome do draft sem depender da cultura do processo:

```csharp
var generatedName = $"{agenda.Nome} - {dataLocal.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}";
```

- [ ] **Step 5: Implementar métricas seguras**

Contadores para avaliadas, criadas, bloqueadas, perdidas, falhas e conflitos; histogram de duração. Tags apenas por status/código estável, nunca nome, observação, usuário ou IDs Discord.

- [ ] **Step 6: Confirmar GREEN e suíte backend**

```bash
dotnet test BackEnd/RinhaDasLendas.sln --configuration Release
dotnet build BackEnd/RinhaDasLendas.sln --configuration Release
```

- [ ] **Step 7: Commitar scheduler**

```bash
git add BackEnd/src/RinhaDasLendas.Api BackEnd/src/RinhaDasLendas.Application BackEnd/tests/RinhaDasLendas.Tests
git commit -m "feat: executar agendas de presença com recuperação"
```

---

### Task 6: Implementar A Central De Automações No Frontend

**Files:**
- Create: `FrontEnd/src/types/presenceSchedule.ts`
- Create: `FrontEnd/src/services/presenceSchedules.ts`
- Create: `FrontEnd/src/services/presenceSchedules.spec.ts`
- Create: `FrontEnd/src/components/settings/PresenceScheduleSection.vue`
- Create: `FrontEnd/src/components/settings/PresenceScheduleSection.spec.ts`
- Create: `FrontEnd/src/components/settings/PresenceScheduleFormDialog.vue`
- Create: `FrontEnd/src/components/settings/PresenceScheduleFormDialog.spec.ts`
- Create: `FrontEnd/src/components/settings/PresenceScheduleConfirmDialog.vue`
- Create: `FrontEnd/src/components/settings/PresenceScheduleConfirmDialog.spec.ts`
- Modify: `FrontEnd/src/views/SettingsView.vue`
- Create: `FrontEnd/src/views/SettingsView.spec.ts`
- Modify: `FrontEnd/src/i18n/locales/pt.json`
- Modify: `FrontEnd/src/i18n/locales/en.json`
- Modify: `FrontEnd/src/styles/main.css`

**Interfaces:**

```ts
export type PresenceScheduleStatus = 'Ativo' | 'Pausado'
export type PresenceScheduleOccurrenceStatus = 'Processando' | 'Bloqueada' | 'Criada' | 'Perdida' | 'Falha'
export type IsoWeekday = 'Segunda' | 'Terca' | 'Quarta' | 'Quinta' | 'Sexta' | 'Sabado' | 'Domingo'

export interface SavePresenceScheduleRequest {
  nome: string
  observacao: string | null
  diasSemana: IsoWeekday[]
  horarioPublicacao: string
  horarioEncerramento: string
}

export function listPresenceSchedules(): Promise<PresenceScheduleSummary[]>
export function createPresenceSchedule(payload: SavePresenceScheduleRequest): Promise<PresenceScheduleSummary>
export function updatePresenceSchedule(id: string, payload: SavePresenceScheduleRequest): Promise<PresenceScheduleSummary>
export function pausePresenceSchedule(id: string): Promise<PresenceScheduleSummary>
export function reactivatePresenceSchedule(id: string): Promise<PresenceScheduleSummary>
export function archivePresenceSchedule(id: string): Promise<void>
```

- [ ] **Step 1: Escrever testes RED do serviço**

Mockar `api` e verificar métodos/URLs, propagação de `messageCode`, serialização de horários `HH:mm` e nenhum fallback que esconda 403/500 como lista vazia.

- [ ] **Step 2: Escrever testes RED do formulário**

```ts
expect(wrapper.get('button[data-weekday="Sexta"]').attributes('aria-pressed')).toBe('false')
await wrapper.get('button[data-weekday="Sexta"]').trigger('click')
expect(wrapper.get('button[data-weekday="Sexta"]').attributes('aria-pressed')).toBe('true')
```

Cobrir nome, observação, ao menos um dia, encerramento posterior, loading, foco, Escape, cancelamento e payload normalizado.

- [ ] **Step 3: Escrever testes RED da seção e Settings**

Cobrir cards, próxima execução, status, estado vazio, bloqueado/perdido/falha, CRUD, confirmação, usuário comum oculto, Moderador visível sem configuração sensível e Admin com ambas.

- [ ] **Step 4: Confirmar RED**

Run: `npm test --prefix FrontEnd -- src/services/presenceSchedules.spec.ts src/components/settings/PresenceScheduleFormDialog.spec.ts src/components/settings/PresenceScheduleSection.spec.ts src/views/SettingsView.spec.ts`

- [ ] **Step 5: Implementar serviço, tipos e i18n**

Adicionar raiz `settings.presenceSchedules` completa nos dois catálogos: títulos, campos, dias, statuses, ações, confirmações, erros, loading, estado vazio, fuso e resumo. Atualizar teste de paridade.

- [ ] **Step 6: Implementar modal e confirmações**

Reutilizar Dialog/Button/Input/Textarea existentes. O modal não recebe regras backend como fonte de verdade; validações locais apenas evitam submissão obviamente inválida e usam i18n.

- [ ] **Step 7: Implementar central e autorização**

Em `SettingsView.vue`:

```vue
<DiscordAdminConfigurationSection v-if="auth.hasPermission(Permissions.CanManageUsers)" />
<PresenceScheduleSection v-if="auth.hasPermission(Permissions.CanManageDrafts)" />
```

Não condicionar agendas a `CanManageUsers`.

- [ ] **Step 8: Aplicar layout aprovado**

Adicionar estilos `presence-schedule-*` em `main.css` usando somente tokens existentes. Desktop com resumo/cards; mobile em uma coluna, ações confortáveis, chips quebráveis/roláveis, modal sem overflow em 320px e `prefers-reduced-motion` respeitado.

- [ ] **Step 9: Confirmar GREEN**

```bash
npm test --prefix FrontEnd
npm run build --prefix FrontEnd
npm run lint --prefix FrontEnd -- --no-fix
```

- [ ] **Step 10: Commitar frontend**

```bash
git add FrontEnd/src
git commit -m "feat: adicionar central de agendamentos de presença"
```

---

### Task 7: Comprovar Compatibilidade Do Bot E Publicar Changelog

**Files:**
- Modify: `discord-bot/src/modules/drafts/draftInteractions.spec.ts`
- Modify: `FrontEnd/src/constants/systemUpdates.ts` e testes.
- Modify: locales PT/EN.
- Modify/Create: documentação do File Map.

**Interfaces:**
- Consumes: draft agendado com publicação `Presenca` pendente, mesmo contrato operacional existente.
- Produces: prova de regressão e release `2026.07.2` como latest.

- [ ] **Step 1: Escrever teste RED do bot**

Criar fixture operacional de draft agendado sem adicionar campo novo ao contrato do bot. Executar `runDraftPollingCycle` e comprovar claim, embed, conclusão e CTA uma única vez em dois ciclos.

- [ ] **Step 2: Confirmar RED/necessidade**

Se o teste passar sem código do bot, registrar que a compatibilidade é comprovada e não alterar produção. Se falhar, corrigir apenas adaptação do contrato, sem mover scheduler ao bot.

- [ ] **Step 3: Adicionar release `2026.07.2`**

```ts
{
  id: 'presence-scheduling-2026-07',
  version: '2026.07.2',
  publishedAt: '2026-07-23',
  featured: true,
  categories: ['feature', 'improvement'],
  areas: ['drafts', 'discord'],
}
```

Remover `featured` de `2026.07.1`, inserir no topo e incluir detalhes localizados: agendamento semanal, horários de publicação/encerramento, gestão Moderador+, recuperação antes da janela e proteção contra duplicidade em linguagem de produto.

- [ ] **Step 4: Atualizar documentação operacional**

Documentar criação, bloqueio, recuperação, ocorrência perdida, falha de envio, pausa/reativação, métricas e runbook sem segredos.

- [ ] **Step 5: Verificar bot, histórico e docs**

```bash
npm test --prefix discord-bot
npm run build --prefix discord-bot
npm test --prefix FrontEnd -- src/constants/systemUpdates.spec.ts src/services/systemUpdates.spec.ts src/i18n/i18n.spec.ts
git diff --check
```

- [ ] **Step 6: Commitar integração e documentação**

```bash
git add discord-bot/src/modules/drafts/draftInteractions.spec.ts FrontEnd/src/constants/systemUpdates.ts FrontEnd/src/constants/systemUpdates.spec.ts FrontEnd/src/i18n docs/domain
git commit -m "docs: registrar agendamento de presenças no produto"
```

---

### Task 8: Verificação Integrada, Segurança E Browser Real

**Files:**
- Modify: `specs/020-agendamento-listas-presenca/tasks.md` apenas para marcar tarefas comprovadas.
- Modify: documentação somente se a verificação encontrar divergência real.

**Interfaces:**
- Consumes: feature completa.
- Produces: evidência final reproduzível.

- [ ] **Step 1: Executar suites e builds completos**

```bash
dotnet test BackEnd/RinhaDasLendas.sln --configuration Release
dotnet build BackEnd/RinhaDasLendas.sln --configuration Release
npm test --prefix FrontEnd
npm run build --prefix FrontEnd
npm run lint --prefix FrontEnd -- --no-fix
npm test --prefix discord-bot
npm run build --prefix discord-bot
```

Expected: zero falhas e zero erros de lint/build.

- [ ] **Step 2: Validar migration PostgreSQL**

Aplicar migration em banco descartável, executar rollback/reaplicação quando suportado e comprovar constraints de agenda/data e dias.

- [ ] **Step 3: Executar matriz HTTP real**

Com clientes anônimo, Jogador, Moderador e Admin: validar 401, 403, CRUD, autoria, payload inválido, arquivado, paginação e ausência de dados operacionais.

- [ ] **Step 4: Executar concorrência real**

Disparar dois ciclos simultâneos para mesma agenda/data e consultar banco/Discord fake: exatamente uma ocorrência, um draft e um claim vencedor.

- [ ] **Step 5: Validar browser real**

Usar `agent-browser` após `agent-browser skills get core`. Login real como Moderador e validar `/configuracoes` em 1440x900, 768x1024, 390x844 e 320px: seção visível, config sensível oculta, criar/editar/pausar/reativar/excluir, teclado, foco, erros, estados e overflow. Repetir permissão com Jogador e Admin.

- [ ] **Step 6: Auditoria de internacionalização**

Registrar obrigatoriamente:

```text
Frontend hardcoded texts: Não encontrados.
Backend hardcoded messages: Não encontrados.
pt.json e en.json sincronizados: Sim.
Backend resources PT/EN sincronizados: Sim.
Bot messages PT/EN sincronizadas: Sim ou não aplicável sem textos novos.
Acentuação portuguesa revisada: Sim.
Placeholders, botões, títulos, badges, toasts, confirmações e estados vazios revisados: Sim.
Validações frontend/backend usam i18n/resource: Sim.
Novos arquivos respeitam o padrão: Sim.
```

Qualquer `Não` deixa a tarefa incompleta.

- [ ] **Step 7: Auditoria de segurança e observabilidade**

Confirmar: somente `CanManageDrafts`; Moderador não vê canais; nenhum body controla autoria; logs sem nome/observação/segredos; DTOs sem claim/IDs Discord; rate limiting e auth existentes preservados.

- [ ] **Step 8: Fechar tarefas e commit final**

```bash
git diff --check
git status --short
git log --oneline -12
git add specs/020-agendamento-listas-presenca/tasks.md
git commit -m "docs: concluir tarefas do agendamento de presenças"
```

Somente marcar tarefas após evidência correspondente.
