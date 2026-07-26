# Quickstart: Validação do Arquivamento Administrativo de Drafts

## Pré-requisitos

- Devcontainer ativo ou Docker Desktop disponível conforme `AGENTS.md`.
- Dependências de `FrontEnd/` e `discord-bot/` instaladas.
- Contas de teste SuperAdmin, Admin, Moderador e Jogador; nunca registrar credenciais ou tokens em logs/evidências.
- Drafts de validação nos sete estados operacionais.

## 1. Backend focado

Dentro do devcontainer:

```bash
dotnet test /workspaces/RinhaDasLendas/BackEnd/RinhaDasLendas.sln --configuration Release --filter "FullyQualifiedName~DraftMontagemArchivingIntegrationTests|FullyQualifiedName~DraftMontagemValidatorTests|FullyQualifiedName~SecurityHardeningTests"
```

No host:

```bash
docker compose -f .devcontainer/docker-compose.yml exec -T app dotnet test /workspaces/RinhaDasLendas/BackEnd/RinhaDasLendas.sln --configuration Release --filter "FullyQualifiedName~DraftMontagemArchivingIntegrationTests|FullyQualifiedName~DraftMontagemValidatorTests|FullyQualifiedName~SecurityHardeningTests"
```

Confirmar:

- cinco estados ativos convertem para `Cancelada` e criam cancelamento/arquivamento/publicação;
- dois estados terminais preservam status e não criam publicação de cancelamento;
- motivo 500 passa, 501 falha;
- repetição não duplica eventos;
- corrida oposta retorna conflito;
- nenhuma relação é removida;
- listagem normal e acesso direto ocultam arquivados;
- somente Admin/SuperAdmin incluem, arquivam, restauram e consultam histórico.

O comando focado só é válido depois que `DraftMontagemArchivingIntegrationTests` existir e aparecer em `dotnet test --list-tests`; a suíte completa continua obrigatória.

Verificação executável da presença da suíte:

```bash
dotnet test /workspaces/RinhaDasLendas/BackEnd/RinhaDasLendas.sln --configuration Release --list-tests --filter "FullyQualifiedName~DraftMontagemArchivingIntegrationTests" | grep -q "DraftMontagemArchivingIntegrationTests"
```

## 2. Migration e banco

Gerar e revisar a migration dentro do devcontainer:

```bash
dotnet ef migrations add AddDraftMontagemArchiving --project /workspaces/RinhaDasLendas/BackEnd/src/RinhaDasLendas.Infrastructure --startup-project /workspaces/RinhaDasLendas/BackEnd/src/RinhaDasLendas.Api
```

O comando de geração é executado uma única vez durante a implementação. Depois disso, validar a migration existente com:

```bash
dotnet ef migrations has-pending-model-changes --project /workspaces/RinhaDasLendas/BackEnd/src/RinhaDasLendas.Infrastructure --startup-project /workspaces/RinhaDasLendas/BackEnd/src/RinhaDasLendas.Api
dotnet ef migrations script --idempotent --project /workspaces/RinhaDasLendas/BackEnd/src/RinhaDasLendas.Infrastructure --startup-project /workspaces/RinhaDasLendas/BackEnd/src/RinhaDasLendas.Api --output /tmp/feature022-migration.sql
```

Validar aplicação em banco vazio e em schema atualizado da `main`, além do `Down` em banco descartável. A migration deve conter apenas as três colunas, FK restritiva, constraint e índices planejados. Não aceitar `DELETE`, `DROP` de dados existentes nem mudança de cascade.

Esses três caminhos serão automatizados em `DraftMontagemArchivingMigrationTests`; executar:

```bash
dotnet test /workspaces/RinhaDasLendas/BackEnd/RinhaDasLendas.sln --configuration Release --filter "FullyQualifiedName~DraftMontagemArchivingMigrationTests"
```

## 3. Bot Discord

```bash
npm test --prefix discord-bot
npm run build --prefix discord-bot
```

Confirmar:

- arquivado oferece somente publicação `Cancelamento`;
- claims antigos de presença/chamada/times são recusados;
- mensagem usa o canal de Drafts e conteúdo PT/EN;
- falha antes do envio registra `Falha` e permite republicação;
- resultado incerto permanece em reconciliação manual, sem duplicação automática.

## 4. Frontend focado

```bash
npm test --prefix FrontEnd -- \
  src/services/draftMontagens.spec.ts \
  src/components/drafts/DraftNavigator.spec.ts \
  src/components/drafts/DraftWorkspaceHeader.spec.ts \
  src/components/drafts/DraftReasonDialog.spec.ts \
  src/components/drafts/DraftDiscordPublicationPanel.spec.ts \
  src/views/DraftsView.spec.ts \
  src/constants/systemUpdates.spec.ts \
  src/i18n/i18n.spec.ts
```

Confirmar matriz Admin/SuperAdmin/Moderador/Jogador, seleção após arquivar, filtro, restauração, perda de permissão, `401`, `403`, `409`, realtime por ID e PT/EN.

## 5. Gates completos

```bash
npm test --prefix FrontEnd
npm run build --prefix FrontEnd
npm run lint:check --prefix FrontEnd
npm audit --prefix FrontEnd -- --audit-level=moderate
npm test --prefix discord-bot
npm run build --prefix discord-bot
```

Backend no devcontainer:

```bash
dotnet test /workspaces/RinhaDasLendas/BackEnd/RinhaDasLendas.sln --configuration Release
dotnet build /workspaces/RinhaDasLendas/BackEnd/RinhaDasLendas.sln --configuration Release
```

## 6. Validação autenticada no navegador

Iniciar API no devcontainer e frontend no host:

```bash
dotnet run --project /workspaces/RinhaDasLendas/BackEnd/src/RinhaDasLendas.Api/RinhaDasLendas.Api.csproj
npm run dev --prefix FrontEnd -- --host 0.0.0.0
```

Abrir `http://localhost:5173/drafts`. Os testes de integração criam uma fixture por estado; para inspeção visual, criar drafts descartáveis pelos fluxos públicos com contas dedicadas, registrar somente os IDs e arquivá-los ao final.

Usar `agent-browser` com uma conta dedicada e validar em 1440x900, 768x900 e 320x844:

- Moderador não vê controles de arquivo apesar de gerenciar drafts;
- Admin vê filtro e arquiva um draft ativo informando motivo;
- item desaparece da lista normal e outra seleção assume sem ações obsoletas;
- filtro revela badge arquivado junto do status cancelado;
- restauração mantém o draft cancelado e não retoma turno/presença;
- foco retorna ao contexto correto; não há overflow horizontal;
- português e inglês não exibem chaves ou textos hardcoded;
- console não contém erros.

Executar no runtime Node disponível suportado pelo repositório (22 ou 24) e registrar a versão com `node --version` nas evidências.

## 7. Auditoria de internacionalização

O relatório final deve confirmar:

- componentes frontend sem texto visível hardcoded;
- `pt.json` e `en.json` sincronizados;
- resources backend base, pt-BR e en-US atualizados;
- mensagens do bot equivalentes em PT/EN;
- acentuação portuguesa revisada;
- filtros, botões, títulos, badges, dialogs, validações, toasts e vazios revisados;
- nenhuma validação nova fora de i18n/resources;
- Atualizações publicada somente depois dos gates locais.
