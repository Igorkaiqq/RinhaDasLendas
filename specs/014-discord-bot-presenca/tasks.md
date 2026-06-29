# Tasks: Discord Bot e Lista de Presença no DraftMontagem

## Backend

- [ ] Ajustar enum de status do DraftMontagem para incluir presença, capitães e ordem.
- [ ] Alterar entidade DraftMontagem para permitir criação sem capitães.
- [ ] Adicionar campos de encerramento de presença e Discord no DraftMontagem.
- [ ] Criar entidade DraftMontagemPresenca.
- [ ] Criar entidade DiscordServerConfiguration.
- [ ] Mapear novas entidades e campos no DbContext.
- [ ] Criar migration EF Core.
- [ ] Criar DTOs de presença, vínculo Discord, configuração Discord e ordem de escolha.
- [ ] Criar validators FluentValidation para presença, configuração, capitães e ordem.
- [ ] Criar autenticação/policy de token interno do bot com escopo limitado.
- [ ] Criar queries/handlers para configuração Discord.
- [ ] Criar endpoint GET/PUT /api/v1/discord/configuracoes.
- [ ] Criar query/handler para vínculo por discordUserId.
- [ ] Criar endpoint GET /api/v1/usuarios/discord/{discordUserId}/vinculo.
- [ ] Criar commands/handlers para confirmar e cancelar presença.
- [ ] Criar commands/handlers para encerrar presença manualmente.
- [ ] Criar hosted service para encerramento automático de presença.
- [ ] Criar commands/handlers para definir capitães após presença.
- [ ] Criar commands/handlers para definir ordem manual/sorteada.
- [ ] Adaptar criação de DraftMontagem para presença aberta.
- [ ] Adaptar finalização para registrar evento/estado de publicação Discord.
- [ ] Adicionar mensagens em MessageCodes e resources pt-BR/en-US/default.
- [ ] Adicionar testes de domínio e handlers principais.

## Frontend

- [ ] Ler design system antes de implementar UI.
- [ ] Criar tipos de configuração Discord e presença.
- [ ] Criar serviço de configuração Discord.
- [ ] Criar serviço de presença e ações de capitães/ordem.
- [ ] Adicionar seção Admin > Configurações > Discord.
- [ ] Adicionar formulário de canais e bot habilitado.
- [ ] Adaptar DraftsView para presença aberta/encerrada.
- [ ] Adicionar aviso e ações para menos de 10 confirmados.
- [ ] Adicionar controles para definir capitães após presença.
- [ ] Adicionar controles para ordem manual/sorteada.
- [ ] Atualizar pt.json e en.json mantendo sincronização.
- [ ] Adicionar testes de serviços/componentes críticos quando viável.

## Discord Bot

- [ ] Criar pasta discord-bot/.
- [ ] Criar package.json, tsconfig, Dockerfile, .env.example e README.
- [ ] Criar validação de env com zod.
- [ ] Criar cliente API com token interno.
- [ ] Criar catálogo de mensagens pt-BR/en-US.
- [ ] Criar bootstrap do client Discord.
- [ ] Criar registro de slash commands.
- [ ] Implementar /draft-criar.
- [ ] Implementar /draft-status.
- [ ] Implementar /draft-encerrar-presenca.
- [ ] Implementar /draft-definir-capitaes.
- [ ] Implementar /draft-definir-ordem-escolha.
- [ ] Implementar botões Confirmar Presença, Cancelar Presença e Ver Status.
- [ ] Implementar embeds de presença e times finalizados.
- [ ] Implementar polling simples de drafts ativos para sincronização.
- [ ] Implementar publicação de times finalizados.

## Verification

- [ ] Executar testes backend.
- [ ] Executar build backend.
- [ ] Executar testes frontend.
- [ ] Executar build frontend.
- [ ] Executar build TypeScript do bot.
- [ ] Auditar internacionalização frontend/backend/bot.
