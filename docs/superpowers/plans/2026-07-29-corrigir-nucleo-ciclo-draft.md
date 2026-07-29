# Corrigir Núcleo do Ciclo de Draft Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Entregar o ciclo v2 de draft com escolha explícita de modo, manual sem capitães, tempo real com capitães elegíveis por draft e compatibilidade integral dos drafts legados.

**Architecture:** O agregado recebe fábricas e transições explícitas, diferindo legado/v2 por uma versão persistida. Handlers consultam roles/atividade e chamam o domínio; API aplica policy Admin+; Vue apresenta a máquina de estados e envia escolhas explícitas. Migration aditiva e testes PostgreSQL protegem os dados existentes.

**Tech Stack:** .NET 10, C# 14, EF Core 10, PostgreSQL 17, ASP.NET Core, MediatR, FluentValidation, xUnit, FluentAssertions, Moq, Vue 3.5, TypeScript 5.9, Vitest e Vue I18n.

## Global Constraints

- Implementar todos os requisitos FR-001 a FR-022 de `specs/028-corrigir-nucleo-ciclo-draft/spec.md`.
- Usar `DraftMontagemCicloVersao.Legado = 1` e `ModoPosPresenca = 2`; não inferir coorte por data, modo ou status.
- Preservar todos os valores existentes durante migration; não revalidar retroativamente capitães legados.
- Restringir administração do ciclo v2 a Admin/SuperAdmin; preservar criação de presença pelo bot sem permitir criação direta com jogadores.
- Manter regras no domínio, DTOs fora do domínio, EF fora do domínio e controllers finos.
- Seguir TDD em cada task e commitar em português após GREEN.
- Todo erro backend deve usar `MessageCodes` e resources PT-BR/EN-US.
- Todo texto frontend deve usar chaves equivalentes em `pt.json` e `en.json`.
- Não criar tokens visuais ou dependências.

---

### Task 1: Versionar o ciclo e preservar dados legados

**Files:**
- Create: `BackEnd/src/RinhaDasLendas.Domain/Enums/DraftMontagemCicloVersao.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Persistence/RinhaDasLendasDbContext.cs`
- Create: `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/*_CorrigirNucleoCicloDraft.cs` por geração do EF Core
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Migrations/RinhaDasLendasDbContextModelSnapshot.cs`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemCycleMigrationTests.cs`

**Interfaces:**
- Produces: `DraftMontagemCicloVersao`, `DraftMontagem.CicloVersao`, `DraftMontagemModo? Modo`.

- [ ] **Step 1: Escrever testes PostgreSQL falhando para backfill e modo nulo**

```csharp
legacy.CicloVersao.Should().Be(DraftMontagemCicloVersao.Legado);
legacy.Modo.Should().Be(modoAntes);
novo.CicloVersao.Should().Be(DraftMontagemCicloVersao.ModoPosPresenca);
novo.Modo.Should().BeNull();
```

- [ ] **Step 2: Executar RED**

Run: `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter FullyQualifiedName~DraftMontagemCycleMigrationTests`

Expected: FAIL por enum, propriedade e migration ausentes.

- [ ] **Step 3: Implementar enum e mapeamento**

```csharp
public enum DraftMontagemCicloVersao : short
{
    Legado = 1,
    ModoPosPresenca = 2,
}

public DraftMontagemCicloVersao CicloVersao { get; private set; }
public DraftMontagemModo? Modo { get; private set; }
```

- [ ] **Step 4: Gerar migration e ajustar backfill**

Run: `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet ef migrations add CorrigirNucleoCicloDraft --project /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/src/RinhaDasLendas.Infrastructure --startup-project /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/src/RinhaDasLendas.Api`

A migration adiciona `ciclo_versao` com backfill `1`, default futuro `2` e remove `NOT NULL` de `modo`, sem alterar outros dados.

- [ ] **Step 5: Executar GREEN e commitar**

```bash
git add BackEnd/src/RinhaDasLendas.Domain BackEnd/src/RinhaDasLendas.Infrastructure BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemCycleMigrationTests.cs
git commit -m "feat: versionar ciclo de montagem de draft"
```

---

### Task 2: Criar caminhos v2 e selecionar modo

**Files:**
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/CreateDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/AgendamentosPresenca/ProcessarAgendamentosPresencaDevidosCommandHandler.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Dtos/SelecionarModoDraftMontagemRequestDto.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Commands/DraftMontagens/SelecionarModoDraftMontagemCommand.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Validators/SelecionarModoDraftMontagemValidator.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/SelecionarModoDraftMontagemCommandHandler.cs`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemCoreCycleTests.cs`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemCoreCycleHandlerTests.cs`

**Interfaces:**
- Produces: `CriarPorPresenca`, `CriarManualDireto`, `SelecionarModo(DraftMontagemModo, IReadOnlySet<Guid>)` e command de modo.

- [ ] **Step 1: Escrever RED para fábricas, idempotência e criação direta sem capitães**

```csharp
var presence = DraftMontagem.CriarPorPresenca("Rinha", null, 5);
presence.Modo.Should().BeNull();
var direct = DraftMontagem.CriarManualDireto("Rinha", null, 5, jogadores);
direct.Status.Should().Be(DraftMontagemStatus.Aberta);
direct.Times.Should().OnlyContain(time => time.CapitaoId is null);
```

- [ ] **Step 2: Executar RED focado**

Run: `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter "FullyQualifiedName~DraftMontagemCoreCycle"`

- [ ] **Step 3: Implementar fábricas e `SelecionarModo`**

Manual inicializa board sem capitães e muda para `Aberta`; TempoReal inicializa titulares/reservas e permanece em preparação. Repetir a mesma escolha não duplica estrutura; mudar depois do avanço falha.

- [ ] **Step 4: Implementar DTO/validator/command/handler e migrar criações produtivas**

```csharp
public sealed record SelecionarModoDraftMontagemRequestDto(string Modo);
public sealed record SelecionarModoDraftMontagemCommand(Guid Id, SelecionarModoDraftMontagemRequestDto Request)
    : IRequest<DraftMontagemResponseDto?>;
```

- [ ] **Step 5: Executar GREEN e commitar**

```bash
git add BackEnd/src BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemCoreCycleTests.cs BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemCoreCycleHandlerTests.cs
git commit -m "feat: separar modos do ciclo de draft"
```

---

### Task 3: Aplicar elegibilidade e máquina de estados realtime

**Files:**
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Repositories/IDraftMontagemRepository.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/DraftMontagemRepository.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/{DefinirCapitaesDraftMontagemCommandHandler,DefinirOrdemEscolhaDraftMontagemCommandHandler,IniciarDraftMontagemTempoRealCommandHandler,RegistrarPickDraftMontagemCommandHandler}.cs`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemCoreCycleTests.cs`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemCoreCycleHandlerTests.cs`

**Interfaces:**
- Produces: `GetCapitaesElegiveisIdsAsync`, `CapitaesElegiveisIds` na projeção admin e transição `CapitaesDefinidos -> OrdemDefinida -> Aberta/TempoReal`.

- [ ] **Step 1: Escrever RED para reserva, inativo, sem role e capitão global não designado**

```csharp
actReserva.Should().Throw<DomainException>().WithMessage(MessageCodes.DraftMontagemCaptainMustBeStarter);
montagem.Participantes.Single(p => p.JogadorId == capitaoNaoDesignado).Capitao.Should().BeFalse();
```

- [ ] **Step 2: Escrever RED para ordem sem turno e início único**

```csharp
montagem.DefinirOrdemEscolha(modo, capitaes);
montagem.Status.Should().Be(DraftMontagemStatus.OrdemDefinida);
montagem.TurnoSequencia.Should().BeNull();
montagem.IniciarTempoReal(agora, elegiveis);
montagem.TurnoSequencia.Should().Be(1);
```

- [ ] **Step 3: Implementar consulta de roles e revalidação nos handlers**

Elegível: jogador ativo, `UsuarioId` preenchido, usuário ativo e role `AuthRoles.Capitao`.

- [ ] **Step 4: Implementar transições v2 preservando ramificações legadas**

Draft v1 mantém comportamento antigo. Draft v2 exige modo TempoReal, recorte titular e estado correto.

- [ ] **Step 5: Executar GREEN e commitar**

```bash
git add BackEnd/src BackEnd/tests/RinhaDasLendas.Tests
git commit -m "feat: validar capitães do draft em tempo real"
```

---

### Task 4: Corrigir manual, substituição e guardas terminais

**Files:**
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Entities/{DraftMontagem,DraftMontagemTime,DraftMontagemParticipante}.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Dtos/SubstituirReservaDraftMontagemRequestDto.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Validators/SubstituirReservaDraftMontagemValidator.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/{SalvarLayoutDraftMontagemCommandHandler,SubstituirReservaDraftMontagemCommandHandler,FinalizarDraftMontagemCommandHandler,SortearCapitaesDraftMontagemCommandHandler}.cs`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemCoreCycleTests.cs`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemCoreCycleHandlerTests.cs`

**Interfaces:**
- Produces: substituição com `NovoCapitaoId`, manual sem capitães, finalização completa e guarda terminal comum.

- [ ] **Step 1: Escrever RED para layout/finalização manual**

```csharp
montagem.SalvarLayout(timesSemCapitao, [], reservas);
montagem.Finalizar();
montagem.Status.Should().Be(DraftMontagemStatus.Finalizada);
```

Também exigir falha com titular livre ou time incompleto.

- [ ] **Step 2: Escrever RED para substituição explícita e terminalidade**

```csharp
montagem.SubstituirPorReserva(timeId, capitaoSaiu, reserva, novoCapitao, elegiveis, null, adminId, agora);
montagem.TurnoAtualCapitaoId.Should().Be(novoCapitao);
```

Finalizada/Cancelada deve recusar layout, sorteio, capitães, picks e substituição.

- [ ] **Step 3: Implementar regras mínimas no agregado e ajustar contrato**

```csharp
public sealed record SubstituirReservaDraftMontagemRequestDto(
    Guid TimeId,
    Guid JogadorSaiuId,
    Guid ReservaEntrouId,
    Guid? NovoCapitaoId,
    string? Motivo);
```

- [ ] **Step 4: Executar GREEN e commitar**

```bash
git add BackEnd/src BackEnd/tests/RinhaDasLendas.Tests
git commit -m "fix: proteger finalização e substituições do draft"
```

---

### Task 5: Expor contratos, policy Admin+ e mensagens localizadas

**Files:**
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Constants/{AuthPermissions,MessageCodes}.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Dtos/{DraftMontagemResponseDto,DraftMontagemAdminResponseDto,DraftMontagemResumoDto}.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Api/{Program.cs,Controllers/DraftMontagensController.cs}`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages*.resx`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/{Application/DraftMontagemProjectionContractTests.cs,Security/SecurityHardeningTests.cs,Integration/EndpointCoverageIntegrationTests.cs}`

**Interfaces:**
- Produces: `PATCH /api/v1/draft-montagens/{id}/modo`, `CanManageDraftCycle`, `modo: string?`, `cicloVersao` e elegibilidade por participante/projeção admin.

- [ ] **Step 1: Escrever RED da matriz 401/403/200**

Anon/Jogador recebem 401/403; Moderador e bot recebem 403; Admin/SuperAdmin recebem sucesso em operações do ciclo.

- [ ] **Step 2: Escrever RED dos contratos e mensagens MV107+**

As projeções retornam modo nulo sem serialização quebrada e todos os novos códigos resolvem PT-BR/EN-US.

- [ ] **Step 3: Implementar policy, endpoint, DTOs e resources**

```csharp
public const string CanManageDraftCycle = nameof(CanManageDraftCycle);
public const string CanCreateDraftPresenceOrManageCycle = nameof(CanCreateDraftPresenceOrManageCycle);
options.AddPolicy(AuthPermissions.CanManageDraftCycle,
    policy => policy.RequireRole(AuthRoles.SuperAdmin, AuthRoles.Admin));
```

- [ ] **Step 4: Executar backend completo e commitar**

Run: `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release`

```bash
git add BackEnd
git commit -m "feat: expor administração segura do ciclo de draft"
```

---

### Task 6: Implementar escolha de modo e board manual no frontend

**Files:**
- Modify: `FrontEnd/src/types/draftMontagem.ts`
- Modify: `FrontEnd/src/services/{draftMontagens.ts,draftMontagens.spec.ts}`
- Modify: `FrontEnd/src/views/{DraftsView.vue,DraftsView.spec.ts}`
- Modify: `FrontEnd/src/components/drafts/{DraftPreparationPanel.vue,DraftPreparationPanel.spec.ts,DraftStateRail.vue,DraftStateRail.spec.ts,DraftWorkspaceHeader.vue,DraftWorkspaceHeader.spec.ts}`
- Modify: `FrontEnd/src/components/drafts/visual/{DraftVisualSetup.vue,DraftVisualSetup.spec.ts,DraftVisualBoard.vue,DraftVisualBoard.spec.ts}`
- Modify: `FrontEnd/src/i18n/locales/{pt,en}.json`

**Interfaces:**
- Produces: `chooseDraftMontagemMode`, modo anulável, seleção Admin+, criação direta manual e board sem capitães.

- [ ] **Step 1: Escrever RED de serviço, role Admin+ e escolha de modo**

```ts
await chooseDraftMontagemMode('draft-1', 'Manual')
expect(api.patch).toHaveBeenCalledWith('/api/v1/draft-montagens/draft-1/modo', { modo: 'Manual' })
```

- [ ] **Step 2: Escrever RED do manual sem capitães e criação direta**

O setup não renderiza seleção de capitães; o board manual envia `capitaoId: null`, esconde ordem/realtime e só finaliza visualmente quando completo.

- [ ] **Step 3: Implementar tipos, serviço, painel, rail, setup e board**

`PresencaEncerrada + modo null` mostra duas ações localizadas. Manual abre board; TempoReal habilita capitães elegíveis. Legacy não volta para escolha de modo.

- [ ] **Step 4: Adicionar PT/EN e executar GREEN**

Run: `npm test -- src/services/draftMontagens.spec.ts src/components/drafts/DraftPreparationPanel.spec.ts src/components/drafts/visual/DraftVisualSetup.spec.ts src/components/drafts/visual/DraftVisualBoard.spec.ts src/views/DraftsView.spec.ts`

- [ ] **Step 5: Commitar**

```bash
git add FrontEnd
git commit -m "feat: separar montagem manual e tempo real"
```

---

### Task 7: Implementar substituição explícita e concluir integração

**Files:**
- Create: `FrontEnd/src/components/drafts/visual/DraftSubstitutionDialog.vue`
- Create: `FrontEnd/src/components/drafts/visual/DraftSubstitutionDialog.spec.ts`
- Modify: `FrontEnd/src/components/drafts/visual/DraftVisualBoard.vue`
- Modify: `FrontEnd/src/views/DraftsView.vue`
- Modify: `FrontEnd/src/constants/messageCode.ts`
- Modify: `FrontEnd/src/services/messageService.ts`
- Modify: `FrontEnd/src/i18n/locales/{pt,en}.json`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Integration/{DraftMontagemCycleIntegrationTests,DraftMontagemCycleAuthorizationIntegrationTests,DraftMontagemLegacyCompatibilityIntegrationTests}.cs`
- Test: `FrontEnd/src/i18n/i18n.spec.ts`

**Interfaces:**
- Produces: diálogo explícito, payload com `NovoCapitaoId`, jornadas manual/realtime e evidência final.

- [ ] **Step 1: Escrever RED do diálogo**

Reserva é obrigatória; novo capitão é obrigatório apenas quando sai o capitão; reserva elegível não é promovida automaticamente; foco e Escape seguem o Dialog existente.

- [ ] **Step 2: Implementar diálogo e integração**

```ts
export interface DraftMontagemSubstitutionPayload {
  timeId: string
  jogadorSaiuId: string
  reservaEntrouId: string
  novoCapitaoId?: string | null
  motivo?: string | null
}
```

- [ ] **Step 3: Escrever e executar jornadas integradas**

```text
presença v2 -> Manual -> layout completo -> Finalizada
presença v2 -> TempoReal -> capitães elegíveis -> OrdemDefinida -> início -> timeout/picks -> substituição -> Finalizada
draft legado -> transições antigas preservadas
```

- [ ] **Step 4: Executar verificação completa**

Backend: `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release && docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet build /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release`

Frontend: `npm run lint:check && npm test && npm run build`

- [ ] **Step 5: Auditar i18n e revisar diff**

Confirmar PT/EN sincronizados, resources backend equivalentes, acentuação correta, nenhum texto visível hardcoded, migration limitada ao escopo e `git diff --check` limpo.

- [ ] **Step 6: Commitar integração**

```bash
git add BackEnd FrontEnd specs/028-corrigir-nucleo-ciclo-draft
git commit -m "test: validar ciclo completo de montagem de draft"
```

## Requirement Coverage

| Requisitos | Tasks |
|---|---|
| FR-001, FR-003, FR-018, FR-022 | 1, 2, 6 |
| FR-002 | 5, 6, 7 |
| FR-004, FR-005 | 2, 4, 6 |
| FR-006 a FR-012 | 3, 5, 6 |
| FR-013, FR-017 | 3, 4, 7 |
| FR-014, FR-015 | 4, 7 |
| FR-016 | 4, 6, 7 |
| FR-019, FR-020 | 5, 6, 7 |
| FR-021 | 1, 2, 3, 4, 7 |
