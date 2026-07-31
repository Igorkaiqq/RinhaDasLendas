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
- Protocolo obrigatório para cada comando backend: (1) executar `dotnet --version` e usar `dotnet` direto se funcionar; (2) caso contrário, executar `docker compose -p rinhadaslendas_devcontainer -f .devcontainer/docker-compose.yml ps -a`, reutilizar/iniciar o projeto Linux e usar `exec -T app`; (3) somente se Linux Docker estiver indisponível, executar `docker.exe ps -a --filter "label=com.docker.compose.project=rinhadaslendas_devcontainer"`, iniciar os containers estáveis e usar os exemplos `docker.exe exec`. Os comandos `docker.exe` abaixo são exemplos do terceiro fallback, não o caminho padrão.
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
- Notifier ports aditivos nesta unidade: `SharedStateUpdatedAsync(..., CancellationToken)`, `ArchivedAsync(Guid, CancellationToken)`, `RestoredAsync(Guid, CancellationToken)`. O método personalizado anterior, adapter e doubles permanecem até a migração atômica dos callers de publicação na Task 4.
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
- Preserve additive seam: `BackEnd/src/RinhaDasLendas.Application/Interfaces/IDraftMontagemRealtimeNotifier.cs`
- Preserve additive adapter: `BackEnd/src/RinhaDasLendas.Api/Services/DraftMontagemRealtimeNotifier.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/FinalizarDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/CancelarDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/ArquivarDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RestaurarDraftMontagemCommandHandler.cs`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemRealtimeMutationCoverageTests.cs`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemCycleIntegrationTests.cs`

**Interfaces:**
- Consumes publisher from Task 2 after successful persistence only.
- Rejected, conflicted and no-op commands call publisher zero times.
- Caller request token remains for DB work but is never forwarded to publisher.
- Layout exige `VersaoEstado` da versão-base; divergência retorna `DraftStateConflict` antes de mutar, salvar ou publicar.
- Esta unidade migra somente handlers que não pertencem ao fluxo de publicação Discord. O método personalizado anterior, adapter e doubles permanecem aditivos para manter os callers de publicação compilando até a Task 4.

- [ ] **RED:** Parameterize every non-publication visible mutation and verify save-before-publish and no-event paths. Inclua teste HTTP via `WebApplicationFactory` para layout com base stale em `DraftMontagemCycleIntegrationTests.cs`, esperando `MV103/409`, banco inalterado e zero publicação.

```csharp
sequence.Should().Equal("save", "publish");
publisher.Verify(x => x.PublishAfterCommitAsync(draftId, It.IsAny<DraftMontagemAvailabilityChange>()), Times.Once);
```

- [ ] **Run RED:** `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter "FullyQualifiedName~DraftMontagemRealtimeMutationCoverageTests|FullyQualifiedName~DraftMontagemCycleIntegrationTests"`

Expected: FAIL for handlers without notification and direct notifier callers.

- [ ] **GREEN:** Migrate the non-publication matrix, preserving personalized response DTOs and removing request-token coupling. Adicione `VersaoEstado` ao DTO/validator de layout e valide-o no handler. Não remova o método anterior do notifier, adapter ou doubles nesta unidade.

- [ ] **Verify:** `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter "FullyQualifiedName~DraftMontagemRealtimeMutationCoverageTests|FullyQualifiedName~DraftMontagemCycleIntegrationTests" && docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet build /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release`.

Expected: PASS; each committed version advance has one attempt.

- [ ] **Commit:** `git add BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens BackEnd/src/RinhaDasLendas.Application/Dtos/SalvarLayoutDraftMontagemRequestDto.cs BackEnd/src/RinhaDasLendas.Application/Validators/SalvarLayoutDraftMontagemValidator.cs BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemRealtimeMutationCoverageTests.cs BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemCycleIntegrationTests.cs && git commit -m "feat: publicar mutações visíveis após commit"`

## Task 4: SQL de Publicação, Versão e Disponibilidade

**Files:**
- Create: `BackEnd/src/RinhaDasLendas.Domain/Models/DraftMontagemVersionStamp.cs`
- Create: `BackEnd/src/RinhaDasLendas.Domain/Models/DraftMontagemPublicacaoClaimResult.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Repositories/IDraftMontagemRepository.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/DraftMontagemRepository.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/AdquirirClaimPublicacaoDiscordDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RegistrarPublicacaoDiscordDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RegistrarFalhaPublicacaoDiscordDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RepublicarPublicacaoDiscordDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RepublicarCancelamentoDraftArquivadoCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Services/DraftMontagemPublicationReconciliationService.cs`
- Delete only after zero references: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/DraftMontagemRealtimeNotificationPublisher.cs`
- Remove old method after helper migration: `BackEnd/src/RinhaDasLendas.Application/Interfaces/IDraftMontagemRealtimeNotifier.cs`
- Remove old adapter member after helper migration: `BackEnd/src/RinhaDasLendas.Api/Services/DraftMontagemRealtimeNotifier.cs`
- Modify: `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemPublicationRealtimeTests.cs`
- Modify: `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemRealtimeNotificationPublisherTests.cs`
- Modify: `BackEnd/tests/RinhaDasLendas.Tests/Services/DraftMontagemPublicationReconciliationServiceTests.cs`
- Create: `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemPublicationVersionIntegrationTests.cs`
- Modify/remove obsolete doubles: `BackEnd/tests/RinhaDasLendas.Tests/Security/SecurityHardeningTests.cs`
- Modify/remove obsolete doubles: `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemBehaviorIntegrationTests.cs`
- Migrate old notifier mocks: `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemCommandHandlerTests.cs`
- Migrate old notifier mocks: `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemCancellationMetricsTests.cs`
- Migrate old notifier mocks: `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemArchivingHandlerTests.cs`
- Migrate old notifier mocks: `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemCoreCycleHandlerTests.cs`

**Interfaces:**
- Claim returns `DraftMontagemPublicacaoClaimResult(Claim, DraftMontagemVersionStamp? VersionStamp)`.
- Success/failure/republication return `DraftMontagemVersionStamp?`; expiration and expiry reconciliation return exact stamp collections.
- SQL CTE or aggregate method changes publication and parent version/data atomically for claim/success/failure/expiration/republication/expiry reconciliation; no-op stamp is null.
- Archive emits existing `DraftMontagemArchived`; restore emits `DraftMontagemRestored`.

- [ ] **RED:** Test claim/success/failure/expiration/republication/expiry-reconciliation version and date increments, exact returned stamp, no-op stability, archive/restore events and all old helper/notifier callers/doubles.

```csharp
result.VersionStamp!.VersaoEstado.Should().Be(previousVersion + 1);
noOp.VersionStamp.Should().BeNull();
```

- [ ] **Run RED:** `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter "FullyQualifiedName~DraftMontagemPublicationVersionIntegrationTests|FullyQualifiedName~DraftMontagemPublicationReconciliationServiceTests"`

Expected: FAIL because current raw SQL returns bool/ID without parent version increment.

- [ ] **GREEN:** Use one transaction/CTE or aggregate operation per visible transition, publish each returned stamp after commit including republication and expiry reconciliation, migrate reconciliation service/tests/doubles, delete the old helper, then remove the old notifier method/adapter/doubles only after zero references.

- [ ] **Verify:** `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter "FullyQualifiedName~DraftMontagemPublicationVersionIntegrationTests|FullyQualifiedName~DraftMontagemPublicationReconciliationServiceTests" && ! git grep -n -E 'DraftMontagemRealtimeNotificationPublisher|(^|[^[:alnum:]_])StateUpdatedAsync\(' -- ':!docs/**' ':!specs/**' && docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet build /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release`.

Expected: PASS, zero old-helper/notifier-surface refs, exact availability events and monotonic stamps for republication and expiry reconciliation.

- [ ] **Commit:** `git add BackEnd/src BackEnd/tests/RinhaDasLendas.Tests && git commit -m "feat: versionar publicações e disponibilidade do draft"`

## Task 5: Actor Migration e Auditoria Administrativa

**Files:**
- Create: `BackEnd/src/RinhaDasLendas.Domain/Enums/DraftMontagemActorType.cs`
- Create: `BackEnd/src/RinhaDasLendas.Domain/Models/DraftMontagemActor.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagemAcaoAdministrativa.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Dtos/DraftMontagemAdminResponseDto.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Persistence/RinhaDasLendasDbContext.cs`
- Create: `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/20260731012844_AddDraftMontagemSystemActor.cs`
- Create: `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/20260731012844_AddDraftMontagemSystemActor.Designer.cs`
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
- Status: `connected | reconnecting | fallback | disconnected`; transport reports successful start/Join as readiness, but `DraftsView` emits `connected` only after the following canonical GET succeeds.
- Retry delays `[0, 2000, 5000, 10000, 15000]`.
- `DraftsView` owns the fallback GET after transport readiness/degradation; its interval is 3000 ms and request timeout is 2000 ms. Publisher timeout remains independently fixed at 5 seconds.

- [ ] **RED:** Test callbacks-before-start, exact start -> Join readiness handoff initially/after recovery, no direct final `connected` emission, failed initial Join, failed rejoin, onclose, one restart timer and idempotent teardown.

```ts
expect(statuses).not.toContain('connected')
expect(fallbackStarted).toBe(true)
```

- [ ] **Run RED:** `npm --prefix FrontEnd test -- src/services/draftMontagemRealtime.spec.ts`.

Expected: FAIL with default reconnect and no Join-failure lifecycle.

- [ ] **GREEN:** Make membership a prerequisite rather than final health; hand readiness/degradation to `DraftsView`, stop failed Join connections and schedule one controlled transport restart.

- [ ] **Verify:** `npm --prefix FrontEnd test -- src/services/draftMontagemRealtime.spec.ts && npm --prefix FrontEnd run build`.

Expected: PASS; `connected` is impossible before start + Join + canonical GET success.

- [ ] **Commit:** `git add FrontEnd/src/types/draftMontagem.ts FrontEnd/src/services/draftMontagemRealtime.ts FrontEnd/src/services/draftMontagemRealtime.spec.ts && git commit -m "feat: degradar conexão quando entrada no draft falhar"`

## Task 9: Version Lanes, Conflict e Sequência Personalizada

**Files:**
- Modify: `FrontEnd/src/services/draftMontagens.ts`
- Modify: `FrontEnd/src/services/draftMontagens.spec.ts`
- Modify: `FrontEnd/src/views/DraftsView.vue`
- Modify: `FrontEnd/src/views/DraftsView.spec.ts`

**Interfaces:**
- Shared acceptance requires version strictly greater.
- `passiveRequestId`/`mutationRequestId` order shared within lanes.
- `personalizedSequence` is global across HTTP lanes.
- Administrative detail that carries the canonical draft uses the passive lane.
- Initial and recovery health sequence is exactly start -> Join -> canonical GET -> `connected`; GET failure remains fallback/degraded.
- View-owned fallback starts every 3000 ms, times out each GET at 2000 ms and forbids overlap.

- [ ] **RED:** Cover equal-version no shared reapply, cross-lane personalized ordering, stale lower metadata rejection, passive administrative detail, exact initial/recovery health ordering, failed canonical GET degradation, fixed 3000 ms cadence, 2000 ms timeout/no overlap and 409 GET-before-unlock.

```ts
expect(applyShared).not.toHaveBeenCalled()
expect(lastPersonalizedSequence).toBe(newerSequence)
```

- [ ] **Run RED:** `npm --prefix FrontEnd test -- src/services/draftMontagens.spec.ts src/views/DraftsView.spec.ts`.

Expected: FAIL with one request version and same-version shared merge.

- [ ] **GREEN:** Add exact counters/gates, retain flat response parsing, route canonical administrative detail through passive, run fallback at fixed 3000 ms with 2000 ms timeout/no overlap and emit `connected` only after the generation's canonical GET succeeds.

- [ ] **Verify:** `npm --prefix FrontEnd test -- src/services/draftMontagens.spec.ts src/views/DraftsView.spec.ts && npm --prefix FrontEnd run build`.

- [ ] **Commit:** `git add FrontEnd/src/services/draftMontagens.ts FrontEnd/src/services/draftMontagens.spec.ts FrontEnd/src/views/DraftsView.vue FrontEnd/src/views/DraftsView.spec.ts && git commit -m "feat: ordenar estado e metadados do draft"`

## Task 10: Enriquecimento Auxiliar Opcional

**Files:**
- Modify: `FrontEnd/src/views/DraftsView.vue`
- Modify: `FrontEnd/src/views/DraftsView.spec.ts`
- Modify: `FrontEnd/src/i18n/locales/pt.json`
- Modify: `FrontEnd/src/i18n/locales/en.json`
- Modify: `FrontEnd/src/i18n/i18n.spec.ts`

**Interfaces:**
- Only eligible-player/presence/captain searches use `auxiliaryRequestId` and their own `AbortController`.
- Auxiliary requests never increment passive/mutation request IDs or consume `personalizedSequence`.
- Administrative detail carrying the canonical draft remains passive, not auxiliary.

- [ ] **RED:** Cover auxiliary draft/generation/request-ID rejection, abort on context change, stale completion, localized dependent-control retry and proof that canonical detail/realtime/actions remain available.

- [ ] **Run RED:** `npm --prefix FrontEnd test -- src/views/DraftsView.spec.ts src/i18n/i18n.spec.ts`.

Expected: FAIL while eligible-player/captain enrichment shares canonical request lifecycle or blocks the main opening.

- [ ] **GREEN:** Add the isolated auxiliary lifecycle and localized control-scoped failure/retry without changing canonical lanes or global personalized sequence.

- [ ] **Verify:** `npm --prefix FrontEnd test -- src/views/DraftsView.spec.ts src/i18n/i18n.spec.ts && npm --prefix FrontEnd run build`.

Expected: PASS; auxiliary failure cannot invalidate canonical state and administrative detail still uses passive.

- [ ] **Commit:** `git add FrontEnd/src/views/DraftsView.vue FrontEnd/src/views/DraftsView.spec.ts FrontEnd/src/i18n && git commit -m "feat: isolar enriquecimentos auxiliares do draft"`

## Task 11: Dirty Signals, Dialog e Guards

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

## Task 12: Integração Multicliente e Observabilidade

**Files:**
- Create: `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemRealtimeMultiClientIntegrationTests.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Observability/ApiMetrics.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Observability/DraftMontagemRealtimeTelemetry.cs`
- Modify: `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemCycleIntegrationTests.cs`
- Modify: `FrontEnd/src/views/DraftsView.spec.ts`

**Interfaces:**
- Metrics use bounded event/outcome labels; draft/version stay in structured logs.
- Temporal assertions: event application `<= 2000 ms`; convergence after backend availability `<= 5000 ms`, backed by 3000 ms fallback cadence plus 2000 ms request timeout.

- [ ] **RED:** Add two-client journey, SQL publication/republication/expiry reconciliation, archive/restore, request-canceled publisher, worker isolation, exact start -> Join -> GET health sequence and timing assertions.

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

## Task 13: Verification, Docs e Deploy

**Files:**
- Modify with measured evidence: `specs/029-corrigir-sincronizacao-draft/quickstart.md`
- Reconcile: `specs/029-corrigir-sincronizacao-draft/contracts/realtime-sync.openapi.yaml`
- Reconcile: `specs/029-corrigir-sincronizacao-draft/contracts/realtime-events.md`
- Reconcile: `specs/029-corrigir-sincronizacao-draft/contracts/ui-contracts.md`
- Modify: `docs/superpowers/plans/2026-07-30-corrigir-sincronizacao-draft.md`

**Interfaces:** No runtime interface; verifies exact contracts from Tasks 1-12.

- [ ] **RED gate:** Run `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release && docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet build /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release && npm --prefix FrontEnd test && npm --prefix FrontEnd run lint:check && npm --prefix FrontEnd run build`.

Expected: every command exits 0; otherwise completion is blocked.

- [ ] **Browser:** Execute quickstart with two sessions and record commit/event/backend-available/converged timestamps.

Expected: every measured event <=2000 ms and every recovery <=5000 ms.

- [ ] **Contract audit:** Validate OpenAPI, zero old-helper/old-notifier-method/adapter/double refs, flat HTTP, ID-only candidates, actor PT/EN, passive administrative detail, isolated auxiliary searches, no Redis/outbox/store.

- [ ] **Hygiene:** Run explicit generated-file marker scan from quickstart and `git diff --check`.

Expected: no generated-artifact marker, whitespace error, locale drift or hardcoded user message.

- [ ] **Commit:** `git add AGENTS.md specs/029-corrigir-sincronizacao-draft docs/superpowers/plans/2026-07-30-corrigir-sincronizacao-draft.md && git commit -m "docs: registrar validação e implantação da sincronização"`

## Requirement Coverage Self-Review

| Requirement | Tasks |
|-------------|-------|
| FR-001 | 1, 2, 9 |
| FR-002 | 1, 9 |
| FR-003 | 3, 4 |
| FR-004 | 2, 12 |
| FR-005 | 2, 12 |
| FR-006 | 3, 4 |
| FR-007 | 8, 9 |
| FR-008 | 1, 8 |
| FR-009 | 8 |
| FR-010 | 8, 9 |
| FR-011 | 8, 9, 10 |
| FR-012 | 9 |
| FR-013 | 9 |
| FR-014 | 9 |
| FR-015 | 9 |
| FR-016 | 9 |
| FR-017 | 9 |
| FR-018 | 8, 11 |
| FR-019 | 10 |
| FR-020 | 10 |
| FR-021 | 11 |
| FR-022 | 11 |
| FR-023 | 11 |
| FR-024 | 11 |
| FR-025 | 11 |
| FR-026 | 6, 7 |
| FR-027 | 5, 7 |
| FR-028 | 6, 7 |
| FR-029 | 3, 4, 6, 7, 12 |
| FR-030 | 7, 12 |
| FR-031 | 1, 5, 10, 11, 13 |
| FR-032 | 2, 8, 13 |
| SC-001 | 12, 13 |
| SC-002 | 8, 9, 12, 13 |
| SC-003 | 9 |
| SC-004 | 3, 4 |
| SC-005 | 2, 12 |
| SC-006 | 6, 7, 12 |
| SC-007 | 11 |
| SC-008 | 6, 7 |
| SC-009 | 10 |
| SC-010 | 5, 8, 10, 11, 13 |
| SC-011 | 12, 13 |

## Deployment Order

1. Backup e migration aditiva de actor.
2. Backend com SQL versionado, flat HTTP e eventos Archived/Restored.
3. Frontend versionado na mesma janela.
4. Uma réplica; observar falhas/timeout de publisher, versões, workers, fallback 3000 ms/timeout 2000 ms e tempos <=2s/<=5s.
5. Rollback da aplicação antes da migration; colunas aditivas permanecem compatíveis. Escala horizontal exige plano futuro separado.
