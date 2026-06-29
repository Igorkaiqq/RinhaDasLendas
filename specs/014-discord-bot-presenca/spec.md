# Feature Specification: Discord Bot e Lista de Presença no DraftMontagem

**Branch**: `014-discord-bot-presenca`  
**Date**: 2026-06-24

## Summary

Adicionar a lista de presença como etapa inicial oficial do fluxo de DraftMontagem e integrar esse fluxo ao Discord por meio de um bot. O DraftMontagem continua sendo o único modelo de draft para montagem de times. A presença pode ser confirmada ou cancelada pela Web ou pelo Discord, sempre com o backend como fonte única da verdade. Após o encerramento da presença, o sistema calcula times e reservas, permite definir capitães, define a ordem de escolha, viabiliza a montagem dos times e publica os times finalizados no Discord.

## User Scenarios & Testing

### User Story 1 - Criar draft com lista de presença

Como Moderador, Admin ou SuperAdmin, quero criar um draft que comece com lista de presença aberta para que os jogadores confirmem participação antes da montagem dos times.

**Acceptance Scenarios**:

1. Dado um usuário autorizado, quando ele cria um draft pela Web ou Discord, então o draft fica com presença aberta e passa a aceitar confirmações.
2. Dado um draft criado via Discord, quando houver canal de presença configurado e bot habilitado, então a lista de presença é publicada no Discord.
3. Dado um draft com presença aberta, quando um jogador confirmado pela Web consulta a tela, então a presença dele aparece também para quem consulta pelo Discord.

### User Story 2 - Confirmar e cancelar presença com vínculo obrigatório

Como jogador, quero confirmar ou cancelar minha presença pela Web ou Discord para participar do draft sem duplicidade.

**Acceptance Scenarios**:

1. Dado um jogador com conta Discord vinculada, quando ele confirma presença pelo Discord, então sua presença é registrada uma única vez.
2. Dado um usuário Discord sem conta vinculada, quando ele tenta confirmar presença, então a presença não é registrada e ele recebe orientação para vincular a conta.
3. Dado um jogador com presença confirmada, quando ele cancela antes do encerramento, então a presença passa a cancelada e a lista compartilhada é atualizada.
4. Dado um draft com presença encerrada, quando um jogador comum tenta confirmar ou cancelar presença, então a ação é bloqueada.

### User Story 3 - Encerrar presença e preparar montagem

Como Moderador, Admin ou SuperAdmin, quero encerrar a lista de presença manualmente ou automaticamente no horário definido para iniciar a preparação da montagem.

**Acceptance Scenarios**:

1. Dado um draft com presença aberta, quando o horário de encerramento chega, então a presença é encerrada automaticamente.
2. Dado um usuário autorizado, quando ele encerra presença manualmente, então novos jogadores comuns não conseguem confirmar presença.
3. Dado pelo menos 10 jogadores confirmados, quando a presença é encerrada, então o sistema calcula quantidade de times e reservas usando grupos de 5 jogadores.
4. Dado menos de 10 jogadores confirmados, quando a presença seria encerrada automaticamente, então o sistema bloqueia a continuidade automática e exige decisão manual de Moderador, Admin ou SuperAdmin.

### User Story 4 - Definir capitães e ordem de escolha

Como organizador, quero definir capitães e ordem de escolha somente após a presença encerrada para que a montagem use apenas jogadores confirmados.

**Acceptance Scenarios**:

1. Dado presença encerrada, quando o organizador define capitães, então somente jogadores confirmados podem ser escolhidos.
2. Dado uma quantidade calculada de times, quando capitães são definidos, então a quantidade de capitães deve ser igual à quantidade de times.
3. Dado capitães definidos, quando o organizador define ordem manual, então a ordem informada passa a controlar a montagem.
4. Dado capitães definidos, quando o organizador solicita ordem sorteada, então o sistema define uma ordem válida entre os capitães.

### User Story 5 - Publicar times finalizados no Discord

Como comunidade, quero ver os times finalizados publicados automaticamente no Discord para acompanhar o resultado do draft.

**Acceptance Scenarios**:

1. Dado um draft finalizado, quando houver canal de drafts configurado e bot habilitado, então o bot publica os times finais no Discord.
2. Dado uma falha de publicação, quando o bot não conseguir publicar, então o erro fica visível para administração e o draft permanece finalizado no sistema.
3. Dado alterações feitas pela Web, quando usuários consultam pelo Discord, então o bot reflete o estado oficial do backend.

## Requirements

### Functional Requirements

- **FR-001**: O sistema deve usar DraftMontagem como modelo único de draft para o fluxo Web e Discord.
- **FR-002**: O sistema deve criar DraftMontagem inicialmente em etapa de presença aberta, sem exigir capitães na criação.
- **FR-003**: O sistema deve permitir confirmação de presença pela Web e pelo Discord.
- **FR-004**: O sistema deve permitir cancelamento de presença pela Web e pelo Discord enquanto a presença estiver aberta.
- **FR-005**: O sistema deve impedir presença duplicada ativa para o mesmo usuário no mesmo draft.
- **FR-006**: O sistema deve exigir vínculo entre Discord e conta interna para confirmar presença via Discord.
- **FR-007**: O sistema deve validar permissões pelo backend para toda ação iniciada via Discord.
- **FR-008**: O sistema deve permitir encerramento manual da presença por Moderador, Admin ou SuperAdmin.
- **FR-009**: O sistema deve encerrar presença automaticamente no horário configurado quando as condições mínimas forem atendidas.
- **FR-010**: O sistema deve bloquear continuidade automática quando houver menos de 10 jogadores confirmados.
- **FR-011**: O sistema deve permitir que Moderador, Admin ou SuperAdmin cancele ou continue manualmente com menos de 10 jogadores confirmados.
- **FR-012**: O sistema deve calcular quantidade de times como total de confirmados dividido por 5, considerando apenas a parte inteira.
- **FR-013**: O sistema deve classificar jogadores excedentes como reservas.
- **FR-014**: O sistema deve permitir times menores quando um organizador autorizar manualmente o fluxo com menos de 10 jogadores.
- **FR-015**: O sistema deve permitir definição de capitães somente após presença encerrada.
- **FR-016**: O sistema deve permitir capitães apenas entre jogadores confirmados.
- **FR-017**: O sistema deve exigir quantidade de capitães igual à quantidade de times calculada ou autorizada manualmente.
- **FR-018**: O sistema deve permitir definir ordem de escolha manual ou sorteada.
- **FR-019**: O sistema deve manter arquitetura preparada para modo Snake Draft futuro, sem exigir esse modo no MVP.
- **FR-020**: O sistema deve publicar a lista de presença no canal Discord configurado quando o draft for criado via Discord e o bot estiver habilitado.
- **FR-021**: O sistema deve publicar os times finalizados no canal Discord configurado quando o draft for finalizado.
- **FR-022**: O sistema deve permitir configuração administrativa dos canais Discord: presença, novidades, administrativo, drafts/times definidos e resultado de partidas.
- **FR-023**: O sistema deve permitir habilitar ou desabilitar o bot por guild configurada.
- **FR-024**: O sistema deve registrar auditoria das ações principais: criação, confirmação, cancelamento, encerramento, capitães, ordem de escolha e finalização.
- **FR-025**: O sistema deve expor mensagens claras para falhas de vínculo, presença encerrada, permissão insuficiente e falha de publicação.
- **FR-026**: O sistema deve refletir no Discord alterações de presença, encerramento, capitães e finalização feitas pela Web.
- **FR-027**: O sistema deve preservar a montagem visual existente após a etapa de presença.
- **FR-028**: O sistema deve manter o backend como fonte única da verdade para status, jogadores, capitães, permissões e times.

### Non-Functional Requirements

- **NFR-001**: O fluxo de confirmação de presença deve responder ao usuário em até 3 segundos em condições normais.
- **NFR-002**: O sistema deve suportar pelo menos 30 jogadores confirmados em um draft sem degradação perceptível para usuários.
- **NFR-003**: Falhas do Discord não devem impedir uso da Web nem corromper o estado oficial do draft.
- **NFR-004**: Mensagens visíveis no frontend e backend devem seguir o padrão de internacionalização do projeto.
- **NFR-005**: Mensagens do bot devem ficar centralizadas em catálogo local para permitir internacionalização futura.

## Key Entities

- **DraftMontagem**: Draft oficial usado para presença, preparação, capitães, ordem de escolha, montagem e finalização.
- **DraftMontagemPresenca**: Registro de confirmação ou cancelamento de presença de um usuário no draft.
- **DiscordServerConfiguration**: Configuração dos canais e estado do bot para uma guild Discord.
- **ExternalAccount**: Vínculo entre usuário interno e identidade externa Discord.
- **DraftMontagemTime**: Time formado na montagem, com capitão e ordem de escolha.
- **DraftMontagemParticipante**: Jogador confirmado que participa da montagem como livre, capitão, membro de time ou reserva.

## Edge Cases

- Usuário Discord sem vínculo tenta confirmar presença.
- Usuário tenta confirmar presença duas vezes.
- Usuário tenta cancelar presença após encerramento.
- Organizador encerra presença com menos de 10 confirmados.
- Horário de encerramento automático chega enquanto o bot está indisponível.
- Canal Discord configurado não existe ou o bot não tem permissão de publicação.
- Capitão escolhido não está entre os confirmados.
- Quantidade de capitães informada difere da quantidade de times.
- Web finaliza draft enquanto o bot está offline.

## Assumptions

- O vínculo Discord oficial é o vínculo externo ativo do usuário interno.
- O tamanho padrão de time é 5 jogadores.
- Menos de 10 jogadores exige decisão manual e não segue finalização automática.
- O Discord usa mensagens em pt-BR no MVP, com estrutura preparada para en-US.
- A sincronização Web e Discord pode usar polling simples ou solicitação direta controlada no MVP.
- RabbitMQ e outbox completa ficam fora do MVP, mas eventos internos devem preparar evolução futura.

## Success Criteria

- **SC-001**: 95% das confirmações ou cancelamentos de presença retornam feedback ao usuário em até 3 segundos em ambiente normal.
- **SC-002**: Um usuário não consegue aparecer duas vezes como presença ativa no mesmo draft.
- **SC-003**: Um usuário Discord sem vínculo não consegue confirmar presença pelo Discord em 100% dos testes funcionais.
- **SC-004**: Um draft com 17 confirmados gera 3 times e 2 reservas após encerramento aprovado.
- **SC-005**: Um draft com menos de 10 confirmados não avança automaticamente para montagem.
- **SC-006**: Capitães só podem ser definidos entre jogadores confirmados em 100% dos testes de permissão e validação.
- **SC-007**: Times finalizados pela Web são publicados no Discord quando bot e canal estiverem configurados.
- **SC-008**: Falhas de publicação no Discord são registradas e não alteram indevidamente o estado finalizado do draft.

## Non-Goals

- Não implementar RabbitMQ no MVP.
- Não implementar Snake Draft no MVP.
- Não substituir RBAC do backend por permissões do Discord.
- Não criar modelo concorrente de draft além do DraftMontagem.
- Não acessar banco diretamente pelo bot.
