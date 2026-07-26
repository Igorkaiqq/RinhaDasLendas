# Implementation Plan: Arquivamento Administrativo de Drafts

**Branch**: `feature/022-arquivar-drafts` | **Date**: 2026-07-26 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/022-arquivar-drafts/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Adicionar arquivamento lógico reversível ao agregado `DraftMontagem`, exclusivo de Admin e SuperAdmin. Drafts ativos serão cancelados e arquivados em uma única persistência que também registra auditoria e a intenção durável de publicar o cancelamento no Discord; drafts terminais preservam o status. Listagens e comandos normais ocultarão arquivados, enquanto um filtro e uma projeção administrativa dedicada permitirão consulta e restauração com concorrência otimista, sem exclusão física.

## Technical Context

**Language/Version**: C# 14 com .NET 10; TypeScript 5.9; Vue 3.5; Node.js 22 ou 24

**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 10, PostgreSQL/Npgsql, MediatR, FluentValidation, SignalR, Vue Composition API, Vue Router, Vue I18n, Axios, Reka UI, discord.js 14 e Zod

**Storage**: PostgreSQL com migration EF Core, colunas relacionais de arquivamento, FK restritiva, constraints e índices parciais; nenhum JSON nem exclusão física

**Testing**: xUnit, FluentAssertions e Moq no backend; Vitest, Vue Test Utils e happy-dom no frontend; test runner Node no bot; Chromium via `agent-browser` para validação autenticada e responsiva

**Target Platform**: Serviços Linux em Docker/Swarm, navegadores modernos desktop/mobile e bot Discord

**Project Type**: Aplicação web full-stack e bot no monorepo existente

**Performance Goals**: Listagens normais continuam paginadas e sem carregar histórico administrativo; arquivar/restaurar exige uma única persistência; publicação de cancelamento fica disponível no próximo ciclo do bot, atualmente de até 30 segundos

**Constraints**: Admin/SuperAdmin apenas; motivo normalizado de 1 a 500 caracteres; sete estados suportados; draft ativo restaurado permanece cancelado; Discord não bloqueia arquivamento; nenhuma mensagem externa antes do commit; PT/EN sincronizados; nenhuma credencial em código/log; nenhuma dependência ou token visual novo

**Scale/Scope**: Um agregado e repositório existentes, três endpoints administrativos, um parâmetro de listagem, uma migration, um novo tipo de publicação Discord, quatro componentes Vue existentes e suítes focadas nos três projetos

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Problema real do grupo**: PASS. Drafts antigos poluem a operação e hoje não existe arquivamento administrativo reversível.
- **MVP e simplicidade**: PASS. A solução estende o agregado, repositório, tela e pipeline Discord existentes, sem novo serviço, store ou tela paralela.
- **Uso sem integração externa**: PASS. O arquivamento confirma independentemente do Discord; a publicação pendente pode ser reconciliada/republicada depois.
- **Separação de responsabilidades**: PASS. Invariantes ficam no domínio, coordenação em handlers, persistência no repositório, policy na API e apresentação no frontend.
- **Persistência relacional**: PASS. Metadados usam colunas, FK, constraints e migration; histórico existente é preservado.
- **CQRS e Repository**: PASS. Arquivar/restaurar são commands, consultas permanecem queries e todo acesso usa `IDraftMontagemRepository`.
- **Autorização**: PASS. Policy dedicada evita herdar `CanManageDrafts`, que inclui Moderador; autoria vem somente do principal autenticado.
- **Regras críticas testadas**: PASS. Estados, atomicidade, idempotência, concorrência, autorização, ocultação, restauração e Discord têm cobertura planejada.
- **Internacionalização**: PASS. Validações e erros backend usam resources; interface, bot e Atualizações possuem PT/EN equivalentes.
- **Segurança e privacidade**: PASS. Motivo, responsável e histórico de arquivamento aparecem apenas na projeção Admin+; realtime público transmite somente o ID arquivado.

**Reavaliação após o design**: PASS. Pesquisa, modelo, contratos e quickstart resolvem as fronteiras de concorrência, Discord e autorização sem violar a Constituição.

## Project Structure

### Documentation (this feature)

```text
specs/022-arquivar-drafts/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── draft-archiving.openapi.yaml
│   └── ui-contracts.md
└── tasks.md
```

### Source Code (repository root)

```text
BackEnd/
├── src/
│   ├── RinhaDasLendas.Domain/
│   │   ├── Constants/{AuthPermissions,MessageCodes}.cs
│   │   ├── Entities/{DraftMontagem,DraftMontagemAcaoAdministrativa}.cs
│   │   ├── Enums/DraftMontagemPublicacaoDiscordTipo.cs
│   │   └── Repositories/IDraftMontagemRepository.cs
│   ├── RinhaDasLendas.Application/
│   │   ├── Commands/DraftMontagens/{Arquivar,Restaurar}DraftMontagemCommand.cs
│   │   ├── Queries/DraftMontagens/GetDraftMontagemArquivamentoQuery.cs
│   │   ├── Handlers/DraftMontagens/{Arquivar,Restaurar,GetDraftMontagemArquivamento}*.cs
│   │   ├── Dtos/{DraftMontagemResponseDto,DraftMontagemResumoDto,DraftMontagemAdminResponseDto,DraftMontagemDiscordOperationalDto,DraftMontagemArquivamentoDtos}.cs
│   │   ├── Validators/{Arquivar,Restaurar}DraftMontagemValidator.cs
│   │   └── Interfaces/IDraftMontagemRealtimeNotifier.cs
│   ├── RinhaDasLendas.Infrastructure/
│   │   ├── Identity/AuthService.cs
│   │   ├── Messages/Messages*.resx
│   │   ├── Persistence/RinhaDasLendasDbContext.cs
│   │   ├── Migrations/*
│   │   └── Repositories/DraftMontagemRepository.cs
│   └── RinhaDasLendas.Api/
│       ├── Controllers/DraftMontagensController.cs
│       ├── Program.cs
│       └── Services/DraftMontagemRealtimeNotifier.cs
└── tests/RinhaDasLendas.Tests/
    ├── Domain/DraftMontagemTests.cs
    ├── Application/{DraftMontagemValidatorTests,DraftMontagemProjectionContractTests}.cs
    ├── Integration/{DraftMontagemArchivingIntegrationTests,DraftMontagemBehaviorIntegrationTests}.cs
    ├── Security/SecurityHardeningTests.cs
    └── Services/DraftMontagemPublicationReconciliationServiceTests.cs

discord-bot/src/
├── shared/{api/types.ts,messages/{pt-BR,en-US}.ts}
├── modules/drafts/{draftInteractions.ts,draftInteractions.spec.ts}
└── discord/embeds/{draftEmbeds.ts,draftEmbeds.spec.ts}

FrontEnd/src/
├── views/{DraftsView.vue,DraftsView.spec.ts}
├── components/drafts/
│   ├── {DraftNavigator,DraftWorkspaceHeader,DraftReasonDialog,DraftDiscordPublicationPanel}.vue
│   └── {DraftNavigator,DraftWorkspaceHeader,DraftReasonDialog,DraftDiscordPublicationPanel}.spec.ts
├── services/{draftMontagens.ts,draftMontagens.spec.ts}
├── types/draftMontagem.ts
├── constants/{permissions.ts,systemUpdates.ts,systemUpdates.spec.ts}
├── i18n/{i18n.spec.ts,locales/{pt,en}.json}
└── styles/main.css
```

**Structure Decision**: Preservar a arquitetura `Api/Application/Domain/Infrastructure/Tests`, o agregado `DraftMontagem`, o repositório e o polling existentes. Não criar status `Arquivada`, tela administrativa ou armazenamento paralelo. `DraftsView.vue` continua orquestrando serviços, seleção e permissões; filhos recebem props e emitem intenções. O bot reutiliza os endpoints de claim/conclusão/falha para o novo tipo `Cancelamento`. O controller aplica `IAuthorizationService` condicional quando `includeArchived=true`. Republicação normal permanece no endpoint protegido por `CanManageDrafts`; republicação de `Cancelamento` em arquivado usa endpoint separado protegido por `CanArchiveDrafts`, evitando autorização de framework dentro da Application.

## Complexity Tracking

Nenhuma violação constitucional requer justificativa.
