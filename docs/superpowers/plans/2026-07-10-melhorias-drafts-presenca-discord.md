# Melhorias Drafts, Presenca e Discord Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Corrigir o fluxo Discord/presenca/draft e reduzir lixo operacional na tela de drafts, mantendo remocao logica e permissoes ADM+ no backend.

**Architecture:** As regras ficam no dominio `DraftMontagem`; comandos/queries no Application via MediatR; controllers so expõem endpoints. O bot apenas converte entrada do Discord e chama API. O frontend consome endpoints e so exibe acoes permitidas.

**Tech Stack:** .NET 10, ASP.NET Core Web API, EF Core, MediatR, FluentValidation, PostgreSQL, xUnit, FluentAssertions, Vue 3, TypeScript, Composition API, vue-i18n, Node.js 24, discord.js.

## Global Constraints

- Nao implementar direto em `main`.
- Nao fazer commit nesta rodada.
- Backend deve seguir DDD/CQRS e controllers finos.
- Frontend deve usar Vue 3, TypeScript e Composition API.
- Todo texto frontend novo deve usar `FrontEnd/src/i18n/locales/pt.json` e `FrontEnd/src/i18n/locales/en.json`.
- Toda mensagem backend nova deve usar resources `.resx`.
- Nao alterar segredos nem arquivos `.env`.

---

### Task 1: Corrigir horario do bot Discord

**Files:**
- Modify: `discord-bot/src/modules/drafts/draftInteractions.ts`
- Test: criar ou ajustar teste do modulo de drafts do bot.

**Interfaces:**
- Produces: `parsePresenceClosingTime(dayInput: string, timeInput: string): string | null` interpretando horario de Brasilia.

- [ ] Escrever teste para entrada `10/07` e `19:30` gerando UTC esperado para Brasilia.
- [ ] Exportar `parsePresenceClosingTime` se necessario para teste.
- [ ] Corrigir conversao sem alterar contrato do comando.
- [ ] Rodar `cd discord-bot && npm test`.
- [ ] Rodar `cd discord-bot && npm run build`.

### Task 2: Listagem padrao de drafts

**Files:**
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Repositories/IDraftMontagemRepository.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/DraftMontagemRepository.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Queries/DraftMontagens/GetDraftMontagensQuery.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/GetDraftMontagensQueryHandler.cs`
- Modify: `BackEnd/src/RinhaDasLendas.Application/Dtos/DraftMontagemResumoDto.cs`

**Interfaces:**
- Produces: listagem com `includeCancelled` opcional e `DataRinha` no resumo.

- [ ] Adicionar parametro booleano opcional na query.
- [ ] Aplicar filtro para ocultar `Cancelada` quando status nao for explicito e `includeCancelled` for falso.
- [ ] Ordenar por `HorarioEncerramentoPresenca ?? DataCadastro` descendente.
- [ ] Mapear `DataRinha` no DTO de resumo.
- [ ] Adicionar teste de query/repository quando possivel.

### Task 3: Presenca manual ADM+ no backend

**Files:**
- Modify: `BackEnd/src/RinhaDasLendas.Domain/Entities/DraftMontagem.cs`
- Add: `BackEnd/src/RinhaDasLendas.Application/Dtos/AdicionarPresencaManualDraftMontagemRequestDto.cs`
- Add: `BackEnd/src/RinhaDasLendas.Application/Dtos/RemoverPresencaManualDraftMontagemRequestDto.cs`
- Add: `BackEnd/src/RinhaDasLendas.Application/Commands/DraftMontagens/AdicionarPresencaManualDraftMontagemCommand.cs`
- Add: `BackEnd/src/RinhaDasLendas.Application/Commands/DraftMontagens/RemoverPresencaManualDraftMontagemCommand.cs`
- Add: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/AdicionarPresencaManualDraftMontagemCommandHandler.cs`
- Add: `BackEnd/src/RinhaDasLendas.Application/Handlers/DraftMontagens/RemoverPresencaManualDraftMontagemCommandHandler.cs`
- Add: validators correspondentes em `BackEnd/src/RinhaDasLendas.Application/Validators/`
- Modify: `BackEnd/src/RinhaDasLendas.Api/Controllers/DraftMontagensController.cs`

**Interfaces:**
- Produces: `POST /api/v1/draft-montagens/{id}/presencas/manual` e `DELETE /api/v1/draft-montagens/{id}/presencas/{jogadorId}` com policy `CanManageDrafts`.

- [ ] Escrever testes de dominio para adicionar, impedir duplicado e remover presenca manual.
- [ ] Implementar metodos no agregado usando origem `Manual`.
- [ ] Criar comandos/handlers/validators.
- [ ] Expor endpoints admin.
- [ ] Adicionar resources para mensagens novas.
- [ ] Rodar testes backend focados.

### Task 4: UI ADM+ e data da rinha

**Files:**
- Modify: `FrontEnd/src/services/draftMontagens.ts`
- Modify: `FrontEnd/src/types/draftMontagem.ts`
- Modify: `FrontEnd/src/views/DraftsView.vue`
- Modify: `FrontEnd/src/i18n/locales/pt.json`
- Modify: `FrontEnd/src/i18n/locales/en.json`

**Interfaces:**
- Consumes: endpoints da Task 3 e `dataRinha` da Task 2.
- Produces: controles ADM+ para adicionar/remover presenca e listagem com data da rinha.

- [ ] Atualizar tipos e chamadas de API.
- [ ] Ajustar listagem para data da rinha e filtro padrao.
- [ ] Adicionar select de jogadores ainda nao presentes para ADM+.
- [ ] Adicionar botao remover nas presencas confirmadas para ADM+.
- [ ] Adicionar todas as chaves i18n em pt/en.
- [ ] Rodar testes/build frontend.

### Task 5: Verificacao final

**Files:**
- Audit all modified files.

**Interfaces:**
- Produces: feature validada sem commits.

- [ ] Rodar `dotnet test BackEnd/RinhaDasLendas.sln`.
- [ ] Rodar `dotnet build BackEnd/RinhaDasLendas.sln --configuration Release`.
- [ ] Rodar `cd FrontEnd && npm test`.
- [ ] Rodar `cd FrontEnd && npm run build`.
- [ ] Rodar `cd discord-bot && npm test`.
- [ ] Rodar `cd discord-bot && npm run build`.
- [ ] Auditar textos hardcoded e sincronizacao de i18n.
