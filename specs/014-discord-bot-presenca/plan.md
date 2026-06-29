# Implementation Plan: Discord Bot e Lista de Presença no DraftMontagem

**Branch**: `014-discord-bot-presenca` | **Date**: 2026-06-24 | **Spec**: [spec.md](./spec.md)

## Summary

Evoluir o DraftMontagem para incluir uma etapa inicial de lista de presença compartilhada entre Web e Discord. O Discord Bot será um projeto independente no monorepo e consumirá exclusivamente a API. O backend continuará sendo a fonte única da verdade para vínculo Discord, permissões, presença, capitães, ordem de escolha, montagem e finalização. Para o MVP, sincronização Discord/Web usará chamadas diretas e polling simples, mantendo eventos internos preparados para futura evolução com outbox e RabbitMQ.

## Technical Context

**Backend**: .NET 10, ASP.NET Core Web API, EF Core, PostgreSQL, Identity, JWT, MediatR, FluentValidation, resources `.resx`.

**Frontend**: Vue 3, TypeScript, Composition API, Axios, Vue Router, vue-i18n.

**Discord Bot**: Node.js, TypeScript, discord.js, dotenv, zod, fetch/axios, deploy independente.

**Storage**: PostgreSQL com migrations EF Core, UUID, snake_case, FKs explícitas e índices únicos.

**Constraints**: Não criar `backend/` ou `frontend/`; manter `BackEnd/` e `FrontEnd/`. Criar apenas `discord-bot/`. Bot não acessa banco. Não criar terceiro modelo de draft. Não implementar RabbitMQ nem Snake Draft no MVP.

## Constitution Check

- **MVP Primeiro**: PASS. Escopo limita RabbitMQ e Snake Draft para futuro.
- **Uso Interno**: PASS. Fluxo atende comunidade interna e permissões RBAC.
- **Simplicidade de Uso**: PASS. Usuários confirmam presença via Web ou Discord.
- **Integrações Não Devem Travar**: PASS. Falhas do Discord não bloqueiam Web nem corrompem draft.
- **Arquitetura e Qualidade**: PASS. Backend é fonte da verdade, bot usa API, regras ficam no domínio/aplicação.
- **Internacionalização**: PASS. Backend usa resources, frontend usa i18n, bot usa catálogo local preparado para i18n.

## Structure Decision

Usar o `DraftMontagem` como único modelo de draft e adicionar presença como etapa inicial. Criar `DraftMontagemPresenca` vinculada a `DraftMontagem`. Ajustar o estado do DraftMontagem para representar `PresencaAberta`, `PresencaEncerrada`, `CapitaesDefinidos`, `OrdemDefinida`, `Aberta`, `Finalizada` e `Cancelada`. A etapa `Aberta` continua representando montagem operacional dos times.

Criar `DiscordServerConfiguration` no domínio porque os canais configurados afetam regras operacionais da integração. Persistência e autenticação interna do bot ficam na infraestrutura/API.

## Data Model Changes

- `DraftMontagem`: permitir criação sem capitães; adicionar horário de encerramento, guild/message id Discord e modo de ordem de escolha.
- `DraftMontagemPresenca`: presença por usuário/jogador em um DraftMontagem.
- `DiscordServerConfiguration`: configuração por guild.
- `DraftMontagemTime`: manter estrutura existente, mas permitir criação dos times após encerramento da presença e definição de capitães.
- `DraftMontagemParticipante`: gerado a partir das presenças confirmadas após encerramento/continuidade manual.

## API Changes

- Adicionar endpoints de configuração Discord.
- Adicionar endpoint de vínculo por Discord user id.
- Adicionar endpoints de presença em `draft-montagens`.
- Adicionar endpoints para capitães e ordem de escolha.
- Adicionar endpoint de drafts ativos para bot.
- Adicionar suporte a token interno limitado do bot.

## Frontend Changes

- Criar área `Admin > Configurações > Discord` dentro da tela de configurações ou rota administrativa dedicada.
- Adicionar lista de presença e ações de presença ao fluxo visual de drafts.
- Exibir alerta e ações para menos de 10 confirmados.
- Adicionar telas/controles para definir capitães e ordem de escolha após presença encerrada.

## Discord Bot Changes

- Criar `discord-bot/` com projeto TypeScript independente.
- Implementar comandos slash e componentes de presença.
- Implementar cliente API com token interno.
- Implementar catálogo local de mensagens pt-BR/en-US.
- Implementar polling simples para refletir alterações Web no Discord e publicar finalização.

## Implementation Phases

1. Backend: domínio, migrations, DTOs, validators, handlers e endpoints para presença/configuração/token interno.
2. Backend: adaptação do DraftMontagem para capitães pós-presença e ordem de escolha.
3. Frontend: configuração Discord e presença no fluxo de DraftMontagem.
4. Discord Bot: estrutura, commands, buttons, embeds, API client e polling.
5. Testes, build, auditoria i18n e documentação.

## Risks and Mitigations

- **Alterar DraftMontagem existente pode quebrar a tela atual**: fazer mudanças incrementais e manter DTOs compatíveis quando possível.
- **Capitães deixam de existir na criação**: ajustar validators, handlers e UI em conjunto.
- **Discord indisponível**: registrar falhas e manter estado oficial no backend.
- **Token interno amplo demais**: criar policy/handler dedicado e endpoints mínimos.
- **Sincronização Web/Discord eventual**: polling curto e endpoints de estado oficial mitigam divergências temporárias.

## Verification

- Testes de domínio para cálculo de times/reservas, presença e capitães.
- Testes de validators para configuração Discord, presença e ordem.
- Testes de handlers para confirmar/cancelar/encerrar presença.
- Build backend.
- Testes e build frontend.
- Build TypeScript do bot.
- Auditoria de internacionalização.
