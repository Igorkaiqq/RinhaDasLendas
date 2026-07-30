# Corrigir Sincronização e Operação do Draft Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Garantir convergência multicliente, recuperação em até 5 segundos, entrega normal em até 2 segundos, processamento automático isolado e proteção inequívoca de layout não salvo.

**Architecture:** A Application implementa publicação pós-commit contra ports; a API adapta SignalR/telemetria e a Infrastructure mantém SQL/projeções. O HTTP personalizado permanece flat, SignalR distribui somente shared, e o frontend separa lifecycle, shared versioning, sequência personalizada, enriquecimento auxiliar e edição dirty.

**Tech Stack:** .NET 10, ASP.NET Core SignalR/Web API, EF Core, PostgreSQL, MediatR, FluentValidation, xUnit/FluentAssertions/Moq, Vue 3.5, TypeScript 5.9, SignalR 10, Vitest/Vue Test Utils e vue-i18n.

## Global Constraints

- Implementar somente após aprovação de `tasks.md`.
- Preservar regras centrais da feature 028.
- Preservar HTTP flat `{ montagem, serverNow, canCurrentUserPick }`.
- Evento SignalR compartilhado nunca contém capacidade de usuário.
- Snapshot com versão igual ou menor nunca reaplica shared.
- Publisher Application não recebe request token; timeout interno fixo de 5 segundos.
- Falha pós-commit, inclusive timeout, nunca converte sucesso funcional em erro.
- SQL visível de publicação incrementa versão/data atomicamente e retorna version stamp.
- Uma réplica; sem Redis, backplane, outbox, event sourcing ou store frontend novo.
- Todo texto visível usa `.resx` ou `pt.json`/`en.json` sincronizados.
- Backend roda pelo devcontainer com paths `/workspaces/RinhaDasLendas/.worktrees/feature-024`; frontend usa `npm --prefix FrontEnd`.
- Para cada comando backend, usar `dotnet` direto quando disponível; senão `docker compose -p rinhadaslendas_devcontainer -f .devcontainer/docker-compose.yml exec -T app`; usar os exemplos `docker.exe exec` das tarefas somente como fallback quando Linux Docker não estiver disponível, conforme `AGENTS.md`.
- Cada unidade termina compilando/testando e com commit PT-BR.

---

## Task 1: Shared Contract e Hub Autorizado

**Files:**
- Create: `BackEnd/src/RinhaDasLendas.Application/Dtos/DraftMontagemRealtimeSnapshotDto.cs`
- Preserve/Modify: `BackEnd/src/RinhaDasLendas.Application/Dtos/DraftMontagemRealtimeStateDto.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Queries/DraftMontagens/CanViewDraftMontagemQuery.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/CanViewDraftMontagemQueryHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/GetDraftMontagemRealtimeStateQueryHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Hubs/DraftMontagensHub.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.resx`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.pt-BR.resx`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.en-US.resx`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemRealtimeAccessTests.cs`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagensHubAuthorizationIntegrationTests.cs`

**Interfaces:**
- Produces `DraftMontagemRealtimeSnapshotDto(DraftMontagemResponseDto Montagem, DateTimeOffset ServerNow)`.
- Preserves `DraftMontagemRealtimeStateDto(DraftMontagemResponseDto Montagem, DateTimeOffset ServerNow, bool CanCurrentUserPick)`.
- Produces `CanViewDraftMontagemQuery(Guid Id) : IRequest<bool>`.
- Rule: human authenticated + existing non-archived; missing/archived/non-human use `DraftRealtimeUnavailable`.

- [ ] **RED:** Test GET/Join parity and identical rejection.

```csharp
allowed.Should().Be(currentUser.UserId.HasValue && !currentUser.IsBot && !draft.Arquivado);
snapshotType.GetProperty("CanCurrentUserPick").Should().BeNull();
```

- [ ] **Run RED:** `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter "FullyQualifiedName~DraftMontagemRealtimeAccessTests|FullyQualifiedName~DraftMontagensHubAuthorizationIntegrationTests"`

Expected: FAIL because Join accepts every authenticated connection and GET has no shared access query.

- [ ] **GREEN:** Implement exact rule/query, localized common rejection and shared DTO while retaining flat HTTP.

- [ ] **Verify:** `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter "FullyQualifiedName~DraftMontagemRealtimeAccessTests|FullyQualifiedName~DraftMontagensHubAuthorizationIntegrationTests" && docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet build /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release`.

Expected: PASS and zero build errors.

- [ ] **Commit:** `git add BackEnd/src/RinhaDasLendas.Application BackEnd/src/RinhaDasLendas.Api/Hubs BackEnd/src/RinhaDasLendas.Domain/Constants BackEnd/src/RinhaDasLendas.Infrastructure/Messages BackEnd/tests/RinhaDasLendas.Tests && git commit -m "feat: separar snapshot e autorizar acesso ao draft"`

## Task 2: Publisher Core na Application

**Files:**
- Create: `BackEnd/src/RinhaDasLendas.Application/Interfaces/IDraftMontagemRealtimePublisher.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Interfaces/IDraftMontagemRealtimeNotifier.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Interfaces/IDraftMontagemRealtimeTelemetry.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Services/DraftMontagemRealtimePublisher.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Enums/DraftMontagemAvailabilityChange.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/DraftMontagemRealtimeStateFactory.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Services/DraftMontagemRealtimeNotifier.cs`
- Create: `BackEnd/src/RinhaDasLendas.Api/Observability/DraftMontagemRealtimeTelemetry.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Program.cs`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemRealtimePublisherTests.cs`
- Modify test doubles: `BackEnd/tests/RinhaDasLendas.Tests/Security/SecurityHardeningTests.cs`
- Modify test doubles: `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemBehaviorIntegrationTests.cs`

**Interfaces:**
- `Task PublishAfterCommitAsync(Guid draftId, DraftMontagemAvailabilityChange availability = DraftMontagemAvailabilityChange.None)`; no cancellation parameter.
- Availability values: `None`, `Archived`, `Restored`.
- Notifier ports aditivos nesta unidade: `SharedStateUpdatedAsync(..., CancellationToken)`, `ArchivedAsync(Guid, CancellationToken)`, `RestoredAsync(Guid, CancellationToken)`. O método personalizado anterior permanece somente até a migração atômica dos callers na Task 3.
- Internal timeout: exactly 5 seconds; archive reloads including archived.

- [ ] **RED:** Test normal publish, request canceled after commit, internal timeout, archived reload and failure absorption.

```csharp
await publisher.PublishAfterCommitAsync(draftId);
telemetry.Failures.Should().Contain(f => f.FailureType == nameof(OperationCanceledException));
```

- [ ] **Run RED:** `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter FullyQualifiedName~DraftMontagemRealtimePublisherTests`

Expected: FAIL because publisher/ports do not exist.

- [ ] **GREEN:** Implement publisher in Application with `CancellationTokenSource(TimeSpan.FromSeconds(5))`, per-send catch/telemetry, flat-independent shared factory and API adapters. Adicione `SharedStateUpdatedAsync` sem remover ainda o método anterior, mantendo esta unidade compilável.

- [ ] **Verify:** `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter FullyQualifiedName~DraftMontagemRealtimePublisherTests && docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet build /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release`.

Expected: PASS; cancellation of the former request does not prevent attempt, internal timeout is observed and absorbed.

- [ ] **Commit:** `git add BackEnd/src/RinhaDasLendas.Application BackEnd/src/RinhaDasLendas.Api BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemRealtimePublisherTests.cs BackEnd/tests/RinhaDasLendas.Tests/Security/SecurityHardeningTests.cs BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemBehaviorIntegrationTests.cs && git commit -m "feat: adicionar publisher pós-commit resiliente"`

## Task 3: Lifecycle dos Handlers de Draft

**Files:**
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/ConfirmarPresencaDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/CancelarPresencaDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/AdicionarPresencaManualDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RemoverPresencaManualDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/EncerrarPresencaDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/ReabrirPresencaDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/SelecionarModoDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/DefinirCapitaesDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/SortearCapitaesDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/DefinirOrdemEscolhaDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/IniciarDraftMontagemTempoRealCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RegistrarPickDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/AvancarTurnoDraftMontagemTimeoutCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/SubstituirReservaDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/SalvarLayoutDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Dtos/SalvarLayoutDraftMontagemRequestDto.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Validators/SalvarLayoutDraftMontagemValidator.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Interfaces/IDraftMontagemRealtimeNotifier.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Services/DraftMontagemRealtimeNotifier.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/FinalizarDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/CancelarDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/ArquivarDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RestaurarDraftMontagemCommandHandler.cs`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemRealtimeMutationCoverageTests.cs`

**Interfaces:**
- Consumes publisher from Task 2 after successful persistence only.
- Rejected, conflicted and no-op commands call publisher zero times.
- Caller request token remains for DB work but is never forwarded to publisher.
- Layout exige `VersaoEstado` da versão-base; divergência retorna `DraftStateConflict` antes de mutar, salvar ou publicar.
- Depois de migrar todos os callers, remover o método personalizado anterior de `IDraftMontagemRealtimeNotifier`; o build desta tarefa comprova zero callers restantes.

- [ ] **RED:** Parameterize every non-publication visible mutation and verify save-before-publish and no-event paths. Inclua layout com base stale, esperando `MV103/409`, zero persistência e zero publicação.

```csharp
sequence.Should().Equal("save", "publish");
publisher.Verify(x => x.PublishAfterCommitAsync(draftId, It.IsAny<DraftMontagemAvailabilityChange>()), Times.Once);
```

- [ ] **Run RED:** `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter FullyQualifiedName~DraftMontagemRealtimeMutationCoverageTests`

Expected: FAIL for handlers without notification and direct notifier callers.

- [ ] **GREEN:** Migrate the complete matrix, preserving personalized response DTOs and removing request-token coupling. Adicione `VersaoEstado` ao DTO/validator de layout, valide-o no handler e remova o método anterior do notifier somente após todos os callers compilarem com `SharedStateUpdatedAsync`.

- [ ] **Verify:** `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter FullyQualifiedName~DraftMontagemRealtimeMutationCoverageTests && docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet build /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release`.

Expected: PASS; each committed version advance has one attempt.

- [ ] **Commit:** `git add BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens BackEnd/src/RinhaDasLendas.Application/Dtos/SalvarLayoutDraftMontagemRequestDto.cs BackEnd/src/RinhaDasLendas.Application/Validators/SalvarLayoutDraftMontagemValidator.cs BackEnd/src/RinhaDasLendas.Application/Interfaces/IDraftMontagemRealtimeNotifier.cs BackEnd/src/RinhaDasLendas.Api/Services/DraftMontagemRealtimeNotifier.cs BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemRealtimeMutationCoverageTests.cs && git commit -m "feat: publicar mutações visíveis após commit"`

## Task 4: SQL de Publicação, Versão e Disponibilidade

**Files:**
- Create: `BackEnd/src/RinhaDasLendas.Domain/Models/DraftMontagemVersionStamp.cs`
- Create: `BackEnd/src/RinhaDasLendas.Domain/Models/DraftMontagemPublicacaoClaimResult.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Repositories/IDraftMontagemRepository.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/DraftMontagemRepository.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/AdquirirClaimPublicacaoDiscordDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RegistrarPublicacaoDiscordDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RegistrarFalhaPublicacaoDiscordDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RepublicarPublicacaoDiscordDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RepublicarCancelamentoDraftArquivadoCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Services/DraftMontagemPublicationReconciliationService.cs`
- Delete only after zero references: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/DraftMontagemRealtimeNotificationPublisher.cs`
- Modify: `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemPublicationRealtimeTests.cs`
- Modify: `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemRealtimeNotificationPublisherTests.cs`
- Modify: `BackEnd/tests/RinhaDasLendas.Tests/Services/DraftMontagemPublicationReconciliationServiceTests.cs`
- Create: `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemPublicationVersionIntegrationTests.cs`

**Interfaces:**
- Claim returns `DraftMontagemPublicacaoClaimResult(Claim, DraftMontagemVersionStamp? VersionStamp)`.
- Success/failure return `DraftMontagemVersionStamp?`; expiration returns `IReadOnlyCollection<DraftMontagemVersionStamp>`.
- SQL CTE changes publication and parent version/data atomically; no-op stamp is null.
- Archive emits existing `DraftMontagemArchived`; restore emits `DraftMontagemRestored`.

- [ ] **RED:** Test claim/success/failure/expiration/republication version increments, no-op stability, archive/restore events and all old helper callers/doubles.

```csharp
result.VersionStamp!.VersaoEstado.Should().Be(previousVersion + 1);
noOp.VersionStamp.Should().BeNull();
```

- [ ] **Run RED:** `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter "FullyQualifiedName~DraftMontagemPublicationVersionIntegrationTests|FullyQualifiedName~DraftMontagemPublicationReconciliationServiceTests"`

Expected: FAIL because current raw SQL returns bool/ID without parent version increment.

- [ ] **GREEN:** Use one transaction/CTE per visible SQL transition, publish returned IDs after commit, migrate reconciliation service/tests/doubles, then verify zero old-helper references before deletion.

- [ ] **Verify:** `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter "FullyQualifiedName~DraftMontagemPublicationVersionIntegrationTests|FullyQualifiedName~DraftMontagemPublicationReconciliationServiceTests" && ! git grep -n DraftMontagemRealtimeNotificationPublisher -- ':!docs/**' ':!specs/**' && docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet build /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release`.

Expected: PASS, zero refs, exact availability events and monotonic stamps.

- [ ] **Commit:** `git add BackEnd/src BackEnd/tests/RinhaDasLendas.Tests && git commit -m "feat: versionar publicações e disponibilidade do draft"`

## Task 5: Actor Migration e Auditoria Administrativa

**Files:**
- Create: `BackEnd/src/RinhaDasLendas.Domain/Enums/DraftMontagemActorType.cs`
- Create: `BackEnd/src/RinhaDasLendas.Domain/Models/DraftMontagemActor.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagemAcaoAdministrativa.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Dtos/DraftMontagemAdminResponseDto.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Persistence/RinhaDasLendasDbContext.cs`
- Create: `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/20260730180000_AddDraftMontagemSystemActor.cs`
- Create: `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/20260730180000_AddDraftMontagemSystemActor.Designer.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/RinhaDasLendasDbContextModelSnapshot.cs`
- Modify: `FrontEnd/src/types/draftMontagem.ts`
- Modify: `FrontEnd/src/views/DraftsView.vue`
- Modify: `FrontEnd/src/views/DraftsView.spec.ts`
- Modify: `FrontEnd/src/i18n/locales/pt.json`, `FrontEnd/src/i18n/locales/en.json`, `FrontEnd/src/i18n/i18n.spec.ts`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemSystemActorMigrationTests.cs`

**Interfaces:**
- DTO/type fields: `responsavelTipo: 'User' | 'System'`, `responsavelUsuarioId: string | null`.
- Labels: `drafts.audit.actor.system = Sistema/System`.
- Constraint: User requires ID; System requires null.

- [ ] **RED:** Test migration backfill/constraint, DTO nullability and localized admin audit rendering.

- [ ] **Run RED:** `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter FullyQualifiedName~DraftMontagemSystemActorMigrationTests && npm --prefix FrontEnd test -- src/views/DraftsView.spec.ts src/i18n/i18n.spec.ts`.

Expected: FAIL because current DTO/type/UI require user ID.

- [ ] **GREEN:** Apply additive migration, actor factories, DTO/OpenAPI-compatible mapping and localized UI label.

- [ ] **Verify:** `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter FullyQualifiedName~DraftMontagemSystemActorMigrationTests && docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet build /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release && npm --prefix FrontEnd test -- src/views/DraftsView.spec.ts src/i18n/i18n.spec.ts && npm --prefix FrontEnd run build`.

Expected: PASS; no blank/fake ID is shown for system actions.

- [ ] **Commit:** `git add BackEnd/src BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemSystemActorMigrationTests.cs FrontEnd/src && git commit -m "feat: expor autoria sistêmica na auditoria do draft"`

## Task 6: Candidatos e Timer de Turno

**Files:**
- Create: `BackEnd/src/RinhaDasLendas.Domain/Models/DraftMontagemRealtimeCandidate.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Repositories/IDraftMontagemRepository.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/DraftMontagemRepository.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Commands/DraftMontagens/ProcessarTurnoDraftMontagemExpiradoCommand.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/ProcessarTurnoDraftMontagemExpiradoCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Services/DraftMontagemTurnTimerService.cs`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Infrastructure/DraftMontagemCandidateProjectionTests.cs`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Services/DraftMontagemTurnTimerServiceTests.cs`

**Interfaces:**
- Candidate is exactly `DraftMontagemRealtimeCandidate(Guid Id)`.
- Command reloads/revalidates expiration and chooses timeout/cancel maximum duration.
- Timeout continua representado pelo histórico de escolha; cancelamento por duração máxima registra ação administrativa com `DraftMontagemActor.System()`.
- One scan scope plus one command scope per ID.

- [ ] **RED:** Assert SQL projection selects only ID, first item failure does not block second, host cancellation propagates, timeout não cria auditoria administrativa extra e cancelamento por duração máxima cria exatamente uma ação com ator `System`.

- [ ] **Run RED:** `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter "FullyQualifiedName~DraftMontagemCandidateProjectionTests|FullyQualifiedName~DraftMontagemTurnTimerServiceTests"`.

Expected: FAIL because current scan loads aggregates and shares scope.

- [ ] **GREEN:** Project IDs, move decisions into command, catch/observe every non-host item exception e usar autoria sistêmica somente no ramo de cancelamento administrativo.

- [ ] **Verify:** `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter "FullyQualifiedName~DraftMontagemCandidateProjectionTests|FullyQualifiedName~DraftMontagemTurnTimerServiceTests" && docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet build /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release`.

- [ ] **Commit:** `git add BackEnd/src BackEnd/tests/RinhaDasLendas.Tests/Infrastructure/DraftMontagemCandidateProjectionTests.cs BackEnd/tests/RinhaDasLendas.Tests/Services/DraftMontagemTurnTimerServiceTests.cs && git commit -m "feat: processar turnos expirados por candidato mínimo"`

## Task 7: Worker de Presença e Logging

**Files:**
- Create: `BackEnd/src/RinhaDasLendas.Domain/Models/DraftMontagemPresenceClosureCandidate.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Repositories/IDraftMontagemRepository.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/DraftMontagemRepository.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Commands/DraftMontagens/EncerrarPresencaDraftMontagemAutomaticamenteCommand.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/EncerrarPresencaDraftMontagemAutomaticamenteCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Services/DraftMontagemPresenceClosureService.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Api/appsettings.json`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Services/DraftMontagemPresenceClosureServiceTests.cs`

**Interfaces:**
- Candidate is exactly `DraftMontagemPresenceClosureCandidate(Guid Id)`; command recalculates count/state/deadline.
- Generic failure and non-host cancellation are logged per ID and continue; host cancellation propagates.
- EF command category is Warning in production settings.

- [ ] **RED:** Test ID-only query and three failure paths: generic, non-host cancellation, host cancellation.

- [ ] **Run RED:** `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter FullyQualifiedName~DraftMontagemPresenceClosureServiceTests`.

Expected: FAIL because current worker mutates all tracked aggregates in one scope.

- [ ] **GREEN:** Add per-ID command/scope, system actor, publisher and bounded structured logs.

- [ ] **Verify:** `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter FullyQualifiedName~DraftMontagemPresenceClosureServiceTests && docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet build /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release`.

- [ ] **Commit:** `git add BackEnd/src BackEnd/tests/RinhaDasLendas.Tests/Services/DraftMontagemPresenceClosureServiceTests.cs && git commit -m "feat: isolar encerramento automático de presenças"`

## Task 8: Lifecycle do Transporte Frontend

**Files:**
- Modify: `FrontEnd/src/types/draftMontagem.ts`
- Modify: `FrontEnd/src/services/draftMontagemRealtime.ts`
- Modify: `FrontEnd/src/services/draftMontagemRealtime.spec.ts`

**Interfaces:**
- Shared event type remains `{ montagem, serverNow }`; personalized HTTP remains flat.
- Status: `connected | reconnecting | fallback | disconnected` plus internal `joined` invariant.
- Retry delays `[0, 2000, 5000, 10000, 15000]`.

- [ ] **RED:** Test callbacks-before-start, Join-before-ready, failed initial Join, failed rejoin, onclose, one restart timer and idempotent teardown.

```ts
expect(statuses).not.toContain('connected')
expect(fallbackStarted).toBe(true)
```

- [ ] **Run RED:** `npm --prefix FrontEnd test -- src/services/draftMontagemRealtime.spec.ts`.

Expected: FAIL with default reconnect and no Join-failure lifecycle.

- [ ] **GREEN:** Make membership part of health; stop failed connection and schedule one controlled restart/fallback.

- [ ] **Verify:** `npm --prefix FrontEnd test -- src/services/draftMontagemRealtime.spec.ts && npm --prefix FrontEnd run build`.

Expected: PASS; connected is impossible outside group.

- [ ] **Commit:** `git add FrontEnd/src/types/draftMontagem.ts FrontEnd/src/services/draftMontagemRealtime.ts FrontEnd/src/services/draftMontagemRealtime.spec.ts && git commit -m "feat: degradar conexão quando entrada no draft falhar"`

## Task 9: Version Lanes, Conflict e Auxiliary

**Files:**
- Modify: `FrontEnd/src/services/draftMontagens.ts`
- Modify: `FrontEnd/src/services/draftMontagens.spec.ts`
- Modify: `FrontEnd/src/views/DraftsView.vue`
- Modify: `FrontEnd/src/views/DraftsView.spec.ts`

**Interfaces:**
- Shared acceptance requires version strictly greater.
- `passiveRequestId`/`mutationRequestId` order shared within lanes.
- `personalizedSequence` is global across HTTP lanes.
- `auxiliaryRequestId`/`AbortController` are independent.

- [ ] **RED:** Cover equal-version no shared reapply, cross-lane personalized ordering, stale lower metadata rejection, isolated auxiliary requests and 409 GET-before-unlock.

```ts
expect(applyShared).not.toHaveBeenCalled()
expect(lastPersonalizedSequence).toBe(newerSequence)
```

- [ ] **Run RED:** `npm --prefix FrontEnd test -- src/services/draftMontagens.spec.ts src/views/DraftsView.spec.ts`.

Expected: FAIL with one request version and same-version shared merge.

- [ ] **GREEN:** Add exact counters/gates, retain flat response parsing and make auxiliary lifecycle independent.

- [ ] **Verify:** `npm --prefix FrontEnd test -- src/services/draftMontagens.spec.ts src/views/DraftsView.spec.ts && npm --prefix FrontEnd run build`.

- [ ] **Commit:** `git add FrontEnd/src/services/draftMontagens.ts FrontEnd/src/services/draftMontagens.spec.ts FrontEnd/src/views/DraftsView.vue FrontEnd/src/views/DraftsView.spec.ts && git commit -m "feat: ordenar estado e metadados do draft"`

## Task 10: Dirty Signals, Dialog e Guards

**Files:**
- Modify: `FrontEnd/src/components/drafts/visual/DraftVisualBoard.vue`
- Modify: `FrontEnd/src/components/drafts/visual/DraftVisualBoard.spec.ts`
- Create: `FrontEnd/src/components/drafts/DraftUnsavedLayoutDialog.vue`
- Create: `FrontEnd/src/components/drafts/DraftUnsavedLayoutDialog.spec.ts`
- Modify: `FrontEnd/src/views/DraftsView.vue`
- Modify: `FrontEnd/src/views/DraftsView.spec.ts`
- Modify: `FrontEnd/src/i18n/locales/pt.json`, `FrontEnd/src/i18n/locales/en.json`, `FrontEnd/src/i18n/i18n.spec.ts`

**Interfaces:**
- Props: `canonicalResetToken: number`, `acceptedSaveVersion: number | null`.
- Event: `dirty-change(dirty: boolean, baseVersion: number)`.
- Save payload carries `versaoEstado = baseVersion`.

- [ ] **RED:** Test remote higher preserve, explicit token reset, matching accepted save, stale/equal event no-clean, 409 preserve, every navigation/archive/unload guard and dialog focus/Escape.

- [ ] **Run RED:** `npm --prefix FrontEnd test -- src/components/drafts/visual/DraftVisualBoard.spec.ts src/components/drafts/DraftUnsavedLayoutDialog.spec.ts src/views/DraftsView.spec.ts src/i18n/i18n.spec.ts`.

Expected: FAIL because current prop watcher always clones/clears.

- [ ] **GREEN:** Implement deterministic signals and one pending-intent dialog using existing tokens/i18n.

- [ ] **Verify:** `npm --prefix FrontEnd test && npm --prefix FrontEnd run lint:check && npm --prefix FrontEnd run build`.

Expected: PASS; dirty clears only by exact reset/save contracts.

- [ ] **Commit:** `git add FrontEnd/src/components/drafts FrontEnd/src/views/DraftsView.vue FrontEnd/src/views/DraftsView.spec.ts FrontEnd/src/i18n && git commit -m "feat: tornar descarte e salvamento de layout explícitos"`

## Task 11: Integração Multicliente e Observabilidade

**Files:**
- Create: `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemRealtimeMultiClientIntegrationTests.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Observability/ApiMetrics.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Observability/DraftMontagemRealtimeTelemetry.cs`
- Modify: `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemCycleIntegrationTests.cs`
- Modify: `FrontEnd/src/views/DraftsView.spec.ts`

**Interfaces:**
- Metrics use bounded event/outcome labels; draft/version stay in structured logs.
- Temporal assertions: event application `<= 2000 ms`; convergence after backend availability `<= 5000 ms`.

- [ ] **RED:** Add two-client journey, SQL publication, archive/restore, request-canceled publisher, worker isolation and timing assertions.

```csharp
eventStopwatch.Elapsed.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(2));
recoveryStopwatch.Elapsed.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(5));
```

- [ ] **Run RED:** `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter FullyQualifiedName~DraftMontagemRealtimeMultiClientIntegrationTests && npm --prefix FrontEnd test -- src/views/DraftsView.spec.ts`.

Expected: FAIL until all paths converge and temporal bounds are measured.

- [ ] **GREEN:** Complete instrumentation/seams only; do not add infrastructure or relax clocks.

- [ ] **Verify:** `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release && docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet build /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release && npm --prefix FrontEnd test && npm --prefix FrontEnd run build`.

Expected: PASS with unequivocal <=2s/<=5s assertions.

- [ ] **Commit:** `git add BackEnd/src/RinhaDasLendas.Api/Observability BackEnd/tests/RinhaDasLendas.Tests/Integration FrontEnd/src/views/DraftsView.spec.ts && git commit -m "test: validar convergência e limites temporais do draft"`

## Task 12: Verification, Docs e Deploy

**Files:**
- Modify with measured evidence: `specs/029-corrigir-sincronizacao-draft/quickstart.md`
- Reconcile: `specs/029-corrigir-sincronizacao-draft/contracts/realtime-sync.openapi.yaml`
- Reconcile: `specs/029-corrigir-sincronizacao-draft/contracts/realtime-events.md`
- Reconcile: `specs/029-corrigir-sincronizacao-draft/contracts/ui-contracts.md`
- Modify: `docs/superpowers/plans/2026-07-30-corrigir-sincronizacao-draft.md`

**Interfaces:** No runtime interface; verifies exact contracts from Tasks 1-11.

- [ ] **RED gate:** Run `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release && docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet build /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release && npm --prefix FrontEnd test && npm --prefix FrontEnd run lint:check && npm --prefix FrontEnd run build`.

Expected: every command exits 0; otherwise completion is blocked.

- [ ] **Browser:** Execute quickstart with two sessions and record commit/event/backend-available/converged timestamps.

Expected: every measured event <=2000 ms and every recovery <=5000 ms.

- [ ] **Contract audit:** Validate OpenAPI, zero old-helper refs, flat HTTP, ID-only candidates, actor PT/EN, no Redis/outbox/store.

- [ ] **Hygiene:** Run explicit generated-file marker scan from quickstart and `git diff --check`.

Expected: no generated-artifact marker, whitespace error, locale drift or hardcoded user message.

- [ ] **Commit:** `git add AGENTS.md specs/029-corrigir-sincronizacao-draft docs/superpowers/plans/2026-07-30-corrigir-sincronizacao-draft.md && git commit -m "docs: registrar validação e implantação da sincronização"`

## Requirement Coverage Self-Review

| Requirement | Tasks |
|-------------|-------|
| FR-001 | 1, 2, 9 |
| FR-002 | 1, 9 |
| FR-003 | 3, 4 |
| FR-004 | 2, 11 |
| FR-005 | 2, 11 |
| FR-006 | 3, 4 |
| FR-007 | 8, 9 |
| FR-008 | 1, 8 |
| FR-009 | 8 |
| FR-010 | 8, 9 |
| FR-011 | 8, 9 |
| FR-012 | 9 |
| FR-013 | 9 |
| FR-014 | 9 |
| FR-015 | 9 |
| FR-016 | 9 |
| FR-017 | 9 |
| FR-018 | 8, 10 |
| FR-019 | 9 |
| FR-020 | 9 |
| FR-021 | 10 |
| FR-022 | 10 |
| FR-023 | 10 |
| FR-024 | 10 |
| FR-025 | 10 |
| FR-026 | 6, 7 |
| FR-027 | 5, 7 |
| FR-028 | 6, 7 |
| FR-029 | 3, 4, 6, 7, 11 |
| FR-030 | 7, 11 |
| FR-031 | 1, 5, 10, 12 |
| FR-032 | 2, 8, 12 |
| SC-001 | 11, 12 |
| SC-002 | 8, 9, 11, 12 |
| SC-003 | 9 |
| SC-004 | 3, 4 |
| SC-005 | 2, 11 |
| SC-006 | 6, 7, 11 |
| SC-007 | 10 |
| SC-008 | 6, 7 |
| SC-009 | 9 |
| SC-010 | 5, 8, 10, 12 |
| SC-011 | 11, 12 |

## Deployment Order

1. Backup e migration aditiva de actor.
2. Backend com SQL versionado, flat HTTP e eventos Archived/Restored.
3. Frontend versionado na mesma janela.
4. Uma réplica; observar falhas/timeout de publisher, versões, workers e tempos <=2s/<=5s.
5. Rollback da aplicação antes da migration; colunas aditivas permanecem compatíveis. Escala horizontal exige plano futuro separado.
