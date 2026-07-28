# Reabertura de Presença do Draft Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Permitir que Moderador+ reabra uma presença encerrada antes dos capitães, preserve confirmações e conclua normalmente drafts com 19 ou 20 participantes.

**Architecture:** A entidade `DraftMontagem` concentra a transição e suas invariantes; um command CQRS resolve a autoria autenticada, persiste e publica o estado atualizado. A API expõe uma alteração parcial protegida por `CanManageDrafts`, e a tela existente reutiliza o diálogo administrativo e o pipeline de mutações para confirmar e executar a ação.

**Tech Stack:** C# 14, .NET 10, ASP.NET Core, EF Core, MediatR, PostgreSQL, xUnit, FluentAssertions, Moq, Vue 3.5, TypeScript 5.9, Vue I18n, Axios e Vitest.

## Global Constraints

- Preservar a arquitetura `Api/Application/Domain/Infrastructure/Tests` e manter regras no domínio.
- Não criar migration: `DraftMontagemAcaoAdministrativa` já persiste tipo, responsável e instante.
- Autorizar somente `CanManageDrafts`; não aceitar autoria no request e não permitir autenticação do bot.
- Reabrir somente `PresencaEncerrada`; não descartar capitães, times, ordem ou escolhas.
- Preservar confirmações e limpar apenas `QuantidadeTimes`, `QuantidadeReservas`, `PresencaContinuadaManualmente` e `HorarioEncerramentoPresenca`.
- Usar somente tokens e componentes visuais existentes.
- Todo texto visível deve existir em `pt.json` e `en.json`; todo erro backend deve existir em resources PT-BR e EN-US.
- Validar a jornada 19 participantes → 3 capitães → ordem → início e 19 → reabrir → 20 → encerrar → 4 capitães.

---

## Summary

Adicionar a transição administrativa reversível `PresencaEncerrada → PresencaAberta`, sem alterar a fórmula de times e reservas. A reabertura remove o prazo automático vencido, zera a estrutura derivada do encerramento, registra `ReaberturaPresenca`, notifica observadores e reapresenta os controles de presença; a interface também explicita quantos capitães precisam ser selecionados.

## Technical Context

**Language/Version**: C# 14 com .NET 10; TypeScript 5.9; Vue 3.5

**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 10, MediatR, Vue I18n, Axios e SignalR

**Storage**: PostgreSQL existente; nenhuma alteração de esquema

**Testing**: xUnit, FluentAssertions e Moq no backend; Vitest, Vue Test Utils e happy-dom no frontend

**Target Platform**: Serviços Linux em Docker/Swarm e navegadores modernos desktop/mobile

**Project Type**: Aplicação web full-stack em monorepo

**Performance Goals**: Reabertura concluída em uma persistência e refletida no fluxo de atualização existente

**Constraints**: Correção pequena, sem dependência do Discord, sem novo prazo automático, PT/EN sincronizados e nenhum token visual novo

**Scale/Scope**: Um agregado, um command/handler, um endpoint, quatro pontos frontend e suítes focadas existentes

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Problema real do grupo**: PASS. Corrige encerramento acidental que hoje exige recriar o draft.
- **MVP e simplicidade**: PASS. Reutiliza agregado, auditoria, autorização, realtime, diálogo e tela existentes.
- **Uso sem integração externa**: PASS. Reabertura e fechamento manual independem do Discord.
- **Separação de responsabilidades**: PASS. Invariante no domínio, coordenação na Application e transporte na API/UI.
- **CQRS e Repository**: PASS. A escrita usa command e o repositório existente.
- **Autorização e autoria**: PASS. `CanManageDrafts` protege o endpoint e `ICurrentUser` fornece o responsável.
- **Regras críticas testadas**: PASS planejado. Domínio, handler, endpoint, serviço e jornadas 19/20 terão cobertura.
- **Internacionalização**: PASS planejado. Código de erro backend e interface terão pares PT/EN.
- **Segurança**: PASS. Nenhum request controla autoria e nenhuma integração externa ganha permissão.

**Reavaliação após o design**: PASS. Contratos e modelo mantêm a correção dentro das fronteiras atuais, sem nova persistência ou exceção constitucional.

## Project Structure

### Documentation (this feature)

```text
specs/024-reabrir-presenca-draft/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── reopen-presence.openapi.yaml
│   └── ui-contracts.md
├── checklists/requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
BackEnd/
├── src/
│   ├── RinhaDasLendas.Domain/{Constants/MessageCodes.cs,Entities/DraftMontagem.cs}
│   ├── RinhaDasLendas.Application/{Commands,Handlers}/DraftMontagens/ReabrirPresencaDraftMontagem*.cs
│   ├── RinhaDasLendas.Infrastructure/Messages/Messages*.resx
│   └── RinhaDasLendas.Api/Controllers/DraftMontagensController.cs
└── tests/RinhaDasLendas.Tests/{Domain,Application,Integration,Security}/

FrontEnd/src/
├── components/drafts/{DraftPreparationPanel,DraftReasonDialog}.{vue,spec.ts}
├── views/{DraftsView.vue,DraftsView.spec.ts}
├── services/{draftMontagens.ts,draftMontagens.spec.ts,messageService.ts}
├── constants/messageCode.ts
└── i18n/locales/{pt,en}.json
```

**Structure Decision**: Estender arquivos e padrões existentes. Criar apenas command e handler focados; não criar DTO de request vazio, store, componente de diálogo ou serviço paralelo.

## Implementation Tasks

### Task 1: Transição de domínio e mensagens

**Files:**
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Constants/MessageCodes.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.resx`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.pt-BR.resx`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Messages/Messages.en-US.resx`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Domain/DraftMontagemTests.cs`

**Interfaces:**
- Produces: `void DraftMontagem.ReabrirPresenca(Guid responsavelUsuarioId)` e `MessageCodes.DraftMontagemPresenceCannotBeReopened = "MV106"`.

- [ ] **Step 1: Escrever testes falhando para preservação, limpeza, auditoria e estados inválidos**

```csharp
montagem.ReabrirPresenca(responsavelId);
montagem.Status.Should().Be(DraftMontagemStatus.PresencaAberta);
montagem.Presencas.Count(item => item.Confirmada).Should().Be(19);
montagem.QuantidadeTimes.Should().Be(0);
montagem.QuantidadeReservas.Should().Be(0);
montagem.HorarioEncerramentoPresenca.Should().BeNull();
montagem.AcoesAdministrativas.Should().ContainSingle(item => item.Tipo == "ReaberturaPresenca" && item.ResponsavelUsuarioId == responsavelId);
```

- [ ] **Step 2: Executar o teste e confirmar falha por método inexistente**

Run: `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter FullyQualifiedName~DraftMontagemTests`

Expected: FAIL porque `ReabrirPresenca` ainda não existe.

- [ ] **Step 3: Implementar a menor transição no agregado**

```csharp
public void ReabrirPresenca(Guid responsavelUsuarioId)
{
    if (Status != DraftMontagemStatus.PresencaEncerrada || Arquivado)
        throw new DomainException(MessageCodes.DraftMontagemPresenceCannotBeReopened);

    Status = DraftMontagemStatus.PresencaAberta;
    QuantidadeTimes = 0;
    QuantidadeReservas = 0;
    PresencaContinuadaManualmente = false;
    HorarioEncerramentoPresenca = null;
    _acoesAdministrativas.Add(new DraftMontagemAcaoAdministrativa("ReaberturaPresenca", responsavelUsuarioId, null));
    Touch();
}
```

- [ ] **Step 4: Adicionar `MV106` aos três resources e executar testes de domínio**

Expected: PASS, incluindo tentativa após capitães e tentativa em arquivado sem mutação.

### Task 2: Caso de uso, API e realtime

**Files:**
- Create: `BackEnd/src/RinhaDasLendas.Application/Commands/DraftMontagens/ReabrirPresencaDraftMontagemCommand.cs`
- Create: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/ReabrirPresencaDraftMontagemCommandHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Controllers/DraftMontagensController.cs`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Application/DraftMontagemCommandHandlerTests.cs`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Integration/EndpointCoverageIntegrationTests.cs`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Security/SecurityHardeningTests.cs`

**Interfaces:**
- Consumes: `DraftMontagem.ReabrirPresenca(Guid)`.
- Produces: `ReabrirPresencaDraftMontagemCommand(Guid Id) : IRequest<DraftMontagemResponseDto?>` e `PATCH /api/v1/draft-montagens/{id}/reabrir-presenca`.

- [ ] **Step 1: Escrever testes falhando do handler e da matriz 401/403/200**

```csharp
var result = await handler.Handle(new ReabrirPresencaDraftMontagemCommand(montagem.Id), CancellationToken.None);
result!.Status.Should().Be(DraftMontagemStatus.PresencaAberta.ToString());
repository.SavedChanges.Should().Be(1);
notifier.ReceivedStates.Should().ContainSingle();
```

- [ ] **Step 2: Executar filtros dos novos testes e confirmar falhas por command/rota ausentes**

Run: `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release --filter "FullyQualifiedName~ReabrirPresenca|FullyQualifiedName~ReopenPresence"`

Expected: FAIL.

- [ ] **Step 3: Implementar command, handler e endpoint protegido**

```csharp
public sealed record ReabrirPresencaDraftMontagemCommand(Guid Id) : IRequest<DraftMontagemResponseDto?>;

var userId = DraftMontagemHandlerHelpers.ResolveRequiredCurrentUserId(currentUser);
var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
if (montagem is null) return null;
montagem.ReabrirPresenca(userId);
await repository.SaveChangesAsync(cancellationToken);
var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
await notifier.StateUpdatedAsync(command.Id, await DraftMontagemRealtimeStateFactory.CreateAsync(updated, repository, currentUser, DateTimeOffset.UtcNow, cancellationToken), cancellationToken);
return DraftMontagemResponseDto.FromEntity(updated);
```

- [ ] **Step 4: Executar os testes focados e a suíte backend completa**

Run: `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release`

Expected: 0 falhas.

### Task 3: Serviço, confirmação e clareza da interface

**Files:**
- Modify: `FrontEnd/src/services/draftMontagens.ts`
- Modify: `FrontEnd/src/services/draftMontagens.spec.ts`
- Modify: `FrontEnd/src/components/drafts/DraftPreparationPanel.vue`
- Modify: `FrontEnd/src/components/drafts/DraftPreparationPanel.spec.ts`
- Modify: `FrontEnd/src/components/drafts/DraftReasonDialog.vue`
- Modify: `FrontEnd/src/components/drafts/DraftReasonDialog.spec.ts`
- Modify: `FrontEnd/src/views/DraftsView.vue`
- Modify: `FrontEnd/src/views/DraftsView.spec.ts`

**Interfaces:**
- Produces: `reopenDraftMontagemPresence(id: string): Promise<DraftMontagem>`, prop `canReopenPresence`, evento `reopen-presence` e ação `{ type: 'reopenPresence'; draftName: string }`.

- [ ] **Step 1: Escrever testes falhando do PATCH, capability, diálogo e fluxo 19/20**

```ts
await reopenDraftMontagemPresence('draft/id')
expect(api.patch).toHaveBeenCalledWith('/api/v1/draft-montagens/draft%2Fid/reabrir-presenca')
expect(panel.props('canReopenPresence')).toBe(true)
expect(screen.getByText('3 / 3 capitães')).toBeTruthy()
```

- [ ] **Step 2: Executar os quatro arquivos de teste e confirmar falha**

Run: `npm test -- src/services/draftMontagens.spec.ts src/components/drafts/DraftPreparationPanel.spec.ts src/components/drafts/DraftReasonDialog.spec.ts src/views/DraftsView.spec.ts`

Expected: FAIL por serviço, prop, ação e traduções ausentes.

- [ ] **Step 3: Implementar serviço e fluxo confirmado na view**

```ts
export async function reopenDraftMontagemPresence(id: string): Promise<DraftMontagem> {
  try {
    const response = await api.patch<DraftMontagem>(`/api/v1/draft-montagens/${encodeURIComponent(id)}/reabrir-presenca`)
    return response.data
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}
```

- [ ] **Step 4: Reutilizar `DraftReasonDialog` como confirmação sem motivo e mostrar contagem de capitães**

O painel mostra `drafts.presence.captainsCount` enquanto `canSelectCaptains`, oferece `drafts.presence.reopen` como ação secundária enquanto `canReopenPresence`, e emite somente intenções. A view revalida estado/permissão no pedido e na confirmação, aplica `applyMutationProjection` e exibe `drafts.presence.reopened`.

- [ ] **Step 5: Executar testes frontend focados**

Expected: PASS e nenhum envio duplicado durante `saving`.

### Task 4: Internacionalização, regressão e entrega

**Files:**
- Modify: `FrontEnd/src/constants/messageCode.ts`
- Modify: `FrontEnd/src/services/messageService.ts`
- Modify: `FrontEnd/src/i18n/locales/pt.json`
- Modify: `FrontEnd/src/i18n/locales/en.json`
- Test: `FrontEnd/src/i18n/i18n.spec.ts`
- Test: `BackEnd/tests/RinhaDasLendas.Tests/Integration/DraftMontagemBehaviorIntegrationTests.cs`

**Interfaces:**
- Consumes: `MV106`, `reopenPresence`, `captainsCount`, `reopen` e `reopened`.

- [ ] **Step 1: Adicionar pares PT/EN e teste integrado da jornada de 19 participantes**

```text
19 confirmações → encerrar → 3 times/4 reservas → 3 capitães → ordem → início
19 confirmações → encerrar → reabrir → 20 confirmações → encerrar → 4 times/0 reservas
```

- [ ] **Step 2: Executar auditorias e suítes completas**

Run: `npm run lint:check && npm test && npm run build` em `FrontEnd/`.

Run: `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release`.

Expected: lint, build e todas as suítes com 0 falhas; chaves PT/EN sincronizadas e resources backend equivalentes.

- [ ] **Step 3: Validar manualmente a jornada de produção**

Seguir `quickstart.md` em desktop e mobile, confirmando foco, diálogo, reabertura, inclusão e avanço.

- [ ] **Step 4: Revisar diff, commitar em português e enviar a branch**

```bash
git status --short
git diff --check
git push -u origin feature/024-reabrir-presenca-draft
```

## Complexity Tracking

Nenhuma violação constitucional requer justificativa.
