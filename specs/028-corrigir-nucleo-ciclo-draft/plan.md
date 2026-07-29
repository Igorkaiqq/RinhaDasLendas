# Corrigir Núcleo do Ciclo de Draft Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Separar montagem manual de draft em tempo real, aplicar corretamente elegibilidade e designação diária de capitães, preservar drafts legados e impedir estados terminais ou incompletos inconsistentes.

**Architecture:** `DraftMontagem` permanece como agregado e fonte das invariantes. Uma versão explícita do ciclo preserva dados legados enquanto novos drafts de presença aguardam escolha de modo; handlers consultam atividade/cargo global, coordenam persistência otimista e projetam DTOs, sem transferir regra para controllers ou Vue. O frontend apresenta as capacidades retornadas pelo backend, mas revalida somente como defesa de interface.

**Tech Stack:** C# 14, .NET 10, ASP.NET Core, EF Core 10, PostgreSQL 17, MediatR, FluentValidation, xUnit, FluentAssertions, Moq, Vue 3.5, TypeScript 5.9, Vue I18n, Vitest e SignalR.

## Global Constraints

- Preservar arquitetura `Api/Application/Domain/Infrastructure/Tests`, CQRS e Repository.
- Regras de modo, recorte titular, capitão diário, capacidade, timeout, substituição e finalização ficam no domínio.
- `Admin+` significa somente `Admin` e `SuperAdmin`; `Moderador` e bot não administram o novo ciclo.
- Cargo global `Capitão` representa elegibilidade; `DraftMontagemParticipante.Capitao` representa autoridade apenas naquele draft.
- Criação direta continua `Manual`; draft de presença novo aguarda modo após fechamento.
- Drafts persistidos antes da migration permanecem no ciclo legado sem reclassificação de estado, modo, times, participantes ou histórico.
- Tempo real só finaliza com todos os times completos; falta de jogador elegível exige intervenção Admin+.
- Toda alteração estrutural terá migration PostgreSQL; rollback após criação de draft v2 com modo nulo é somente roll-forward.
- Todo texto frontend deve estar em `pt.json` e `en.json`; toda mensagem backend deve estar nos três resources.
- Usar somente tokens e componentes do design system existente.
- Seguir TDD, controle otimista por `VersaoEstado` e commits em português por incremento.

## Technical Context

**Language/Version**: C# 14/.NET 10 e TypeScript 5.9/Vue 3.5

**Storage**: PostgreSQL; novas colunas `ciclo_versao` e modo anulável

**Testing**: xUnit/FluentAssertions/Moq; Vitest/Vue Test Utils/happy-dom

**Target Platform**: Docker/Swarm Linux e navegadores modernos desktop/mobile

**Performance Goals**: Uma persistência por transição; consultas de elegibilidade limitadas aos participantes do draft

**Scale/Scope**: Um agregado, um endpoint novo, contratos existentes, uma migration e a tela de drafts

## Constitution Check

- **Problema real do grupo**: PASS. Corrige travamentos, autoridade errada e finalização inconsistente.
- **MVP e simplicidade**: PASS. Mantém um agregado e dois modos explícitos, sem serviço paralelo.
- **Alternativa manual**: PASS. Manual independe de capitães e integrações.
- **Separação de responsabilidades**: PASS. Domínio decide; Application coordena; API/UI transportam.
- **CQRS e Repository**: PASS. Seleção de modo será command dedicado e elegibilidade será consulta do repositório.
- **Autorização**: PASS planejado. Policy dedicada `CanManageDraftCycle` limita operações a Admin+.
- **Persistência segura**: PASS planejado. Versão explícita evita inferência sobre dados legados.
- **Testes críticos**: PASS planejado. Domínio, handlers, migration, segurança, integração e frontend.
- **Internacionalização**: PASS planejado. Recursos PT-BR/EN-US e locales PT/EN sincronizados.

**Reavaliação após design**: PASS. A complexidade nova é limitada à distinção persistida necessária para compatibilidade concreta.

## Project Structure

```text
specs/028-corrigir-nucleo-ciclo-draft/
├── spec.md
├── plan.md
└── tasks.md

BackEnd/src/
├── RinhaDasLendas.Domain/{Entities,Enums,Constants,Repositories}/
├── RinhaDasLendas.Application/{Commands,Handlers,Dtos,Validators}/DraftMontagens/
├── RinhaDasLendas.Infrastructure/{Persistence,Repositories,Messages,Migrations}/
└── RinhaDasLendas.Api/{Controllers,Program.cs}/

FrontEnd/src/
├── views/DraftsView.vue
├── components/drafts/{DraftPreparationPanel,DraftStateRail,DraftWorkspaceHeader}.vue
├── components/drafts/visual/{DraftVisualSetup,DraftVisualBoard,DraftSubstitutionDialog}.vue
├── services/{draftMontagens,messageService}.ts
├── types/draftMontagem.ts
└── i18n/locales/{pt,en}.json
```

## Design Decisions

### Persisted cycle version

Adicionar `DraftMontagemCicloVersao` com `Legado = 1` e `ModoPosPresenca = 2`. A migration classifica todas as linhas existentes como `Legado`; fábricas produtivas novas gravam `ModoPosPresenca`. `Modo` torna-se anulável para presença v2 e continua preenchido em criação direta e dados legados.

### Creation paths

`CriarPorPresenca` cria `PresencaAberta`, `Modo = null`, sem times. `CriarManualDireto` cria `Aberta/Manual`, times sem capitães, titulares livres e excedentes reservas. O construtor existente permanece apenas para materialização/compatibilidade até os consumidores produtivos migrarem.

### State machine v2

```text
PresencaAberta -> PresencaEncerrada -> escolher modo
Manual -> Aberta -> layout completo -> Finalizada
TempoReal -> PresencaEncerrada -> CapitaesDefinidos -> OrdemDefinida -> iniciar -> Aberta -> Finalizada
```

### Captain eligibility

O repositório retorna IDs de jogadores ativos, vinculados a usuários ativos com role `Capitão`. O domínio cruza esse conjunto com o recorte titular. Revalidar na definição, início, pick e substituição. Jogador globalmente elegível não designado permanece participante comum.

### Substitution

O request inclui `NovoCapitaoId`. A reserva nunca herda capitania. Se o capitão diário sair, Admin+ escolhe explicitamente um jogador elegível no time resultante; a troca atualiza time, flags e `TurnoAtualCapitaoId` na mesma versão.

### Authorization

Adicionar `CanManageDraftCycle` para Admin/SuperAdmin e `CanCreateDraftPresenceOrManageCycle` para Admin/SuperAdmin ou bot. O endpoint de criação usa a segunda policy, exclui Moderador e rejeita payload com jogadores quando a identidade é do bot; assim o bot preserva somente a criação de presença. Escolha de modo, capitães, ordem, início, layout, substituição, sorteio e finalização usam `CanManageDraftCycle`. Presença e consultas preservam políticas existentes; pick continua autenticado e validado pelo domínio.

## Delivery Phases

1. Persistência/versionamento e fábricas.
2. Escolha de modo e montagem manual.
3. Elegibilidade, ordem explícita e início realtime.
4. Substituição, terminalidade, pick e timeout.
5. API, policy, DTOs, resources e migration.
6. Frontend de modo, manual, realtime e substituição.
7. Segurança, integração, concorrência, compatibilidade e entrega.

## Verification Commands

Backend:

```bash
docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release
docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet build /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/RinhaDasLendas.sln --configuration Release
```

Frontend:

```bash
npm run lint:check
npm test
npm run build
```

## Complexity Tracking

| Decisão | Necessidade | Alternativa rejeitada |
|---|---|---|
| `ciclo_versao` persistido | Distinguir legado após v2 escolher modo | Inferir por modo/status quebraria drafts existentes |
| `Modo` anulável | Representar escolha ainda não realizada | Enum `Indefinido` poluiria o domínio e contratos |
| Policy dedicada | Restringir ciclo sem remover capacidades antigas de Moderador | Alterar `CanManageDrafts` quebraria presença e consultas |
| Diálogo de substituição | Escolher reserva e novo capitão explicitamente | Primeira reserva e herança automática são incorretas |
