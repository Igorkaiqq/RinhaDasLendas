# Feature Specification: Robustecer Drafts, Discord e Jogadores

**Feature Branch**: `feature/016-melhorias-drafts-presenca-discord`

**Created**: 2026-07-10

**Status**: Draft

**Input**: User description: "Implementar em sequência as 15 melhorias identificadas na auditoria do sistema que envolve bot Discord, draft e jogadores."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Entrar no draft certo pelo Discord (Priority: P1)

Como jogador que recebeu o convite no Discord, quero abrir o link e cair diretamente na rinha correta no site, para confirmar presença ou acompanhar o draft sem procurar manualmente.

**Why this priority**: O convite Discord é uma das portas principais do fluxo; se o link não abre o draft certo, o usuário perde confiança e pode confirmar presença no lugar errado.

**Independent Test**: Pode ser testado abrindo um convite de draft com identificador válido e verificando que o site seleciona automaticamente o draft correspondente.

**Acceptance Scenarios**:

1. **Given** existe um draft ativo com convite publicado no Discord, **When** o jogador abre o link do convite, **Then** o site exibe esse draft como selecionado.
2. **Given** o link contém um identificador inválido ou inacessível, **When** o jogador abre o link, **Then** o site informa que o draft não foi encontrado e mantém a listagem utilizável.

---

### User Story 2 - Operar o bot com configuração segura e mensagens claras (Priority: P1)

Como administrador, quero que o bot respeite a configuração de ativação, valide datas corretamente e explique falhas com mensagens claras, para reduzir suporte manual e evitar comandos executados em configuração inválida.

**Why this priority**: O bot pode criar/publicar listas e interagir com jogadores; erros de configuração, data e API precisam ser previsíveis.

**Independent Test**: Pode ser testado desativando a configuração do bot, usando datas passadas e simulando erros da API, verificando respostas claras ao usuário e logs técnicos úteis.

**Acceptance Scenarios**:

1. **Given** a integração Discord está desativada, **When** um comando do bot tenta criar ou publicar draft, **Then** o bot não executa a ação e informa que a integração está indisponível.
2. **Given** o usuário informa uma data/hora passada, **When** o comando de criação é executado, **Then** o bot rejeita a entrada com orientação de data futura em horário de Brasília.
3. **Given** a API retorna um erro conhecido, **When** o bot processa a falha, **Then** a resposta usa uma mensagem amigável específica sem expor payload técnico.

---

### User Story 3 - Evitar republicação e permitir recuperação operacional (Priority: P1)

Como administrador, quero que publicações no Discord sejam persistidas e recuperáveis, para que reinícios do bot não dupliquem mensagens e para que eu possa republicar quando algo for apagado ou falhar.

**Why this priority**: Publicações duplicadas no canal confundem jogadores e exigem limpeza manual; falhas de publicação precisam de caminho de correção.

**Independent Test**: Pode ser testado reiniciando o bot após publicar presença/times e verificando que não há duplicidade, além de acionar uma republicação manual quando necessário.

**Acceptance Scenarios**:

1. **Given** uma lista de presença já foi publicada, **When** o bot reinicia e faz nova varredura, **Then** ele não publica uma segunda mensagem para a mesma lista.
2. **Given** a publicação no Discord falhou ou foi removida, **When** um administrador solicita republicação, **Then** o sistema publica novamente e registra o novo estado.

---

### User Story 4 - Presença confiável entre site, bot e jogadores (Priority: P2)

Como jogador ou administrador, quero que confirmação, cancelamento e presença manual sejam idempotentes, atualizem a tela em tempo real e impeçam duplicidade, para que a lista represente corretamente quem vai jogar.

**Why this priority**: A lista de presença é a base para times, reservas e capitães; divergência aqui compromete todo o draft.

**Independent Test**: Pode ser testado com confirmações simultâneas, cancelamentos repetidos e presença manual, verificando que só existe um estado válido por jogador no draft.

**Acceptance Scenarios**:

1. **Given** dois eventos tentam confirmar o mesmo jogador ao mesmo tempo, **When** ambos são processados, **Then** o jogador aparece uma única vez como confirmado.
2. **Given** uma presença muda por site ou bot, **When** outro usuário está com o draft aberto, **Then** a tela dele recebe o estado atualizado sem recarregar manualmente.
3. **Given** um administrador busca jogador para presença manual, **When** digita parte do nome, **Then** o sistema mostra apenas jogadores elegíveis e ainda não confirmados.

---

### User Story 5 - Gerenciar fluxo e auditoria do draft com transparência (Priority: P2)

Como administrador, quero ver o estado de publicação, motivos de cancelamento/removal e métricas de etapas do draft, para diagnosticar problemas e explicar decisões ao grupo.

**Why this priority**: Operação de rinha precisa de rastreabilidade suficiente para resolver conflitos sem ler logs técnicos.

**Independent Test**: Pode ser testado cancelando draft, removendo presença, publicando/republicando no Discord e verificando histórico, estado visível e métricas/logs.

**Acceptance Scenarios**:

1. **Given** um draft foi publicado no Discord, **When** um administrador abre o site, **Then** ele vê se a presença/times foram publicados, estão pendentes ou falharam.
2. **Given** um administrador cancela draft ou remove presença, **When** a ação é concluída, **Then** o motivo e o responsável ficam auditáveis.
3. **Given** uma etapa crítica falha, **When** o operador verifica logs/métricas, **Then** consegue identificar a etapa e o motivo geral da falha.

---

### User Story 6 - Confirmar ações administrativas com contexto (Priority: P2)

Como administrador, quero confirmar cancelamento, remoção manual e republicações em um modal contextual, para entender o impacto da ação sem depender do prompt nativo do navegador.

**Why this priority**: A confirmação contextual reduz erros em ações administrativas sensíveis e melhora acessibilidade e previsibilidade sem alterar as regras de negócio existentes.

**Independent Test**: Abrir cada uma das quatro ações e verificar título, contexto, motivo obrigatório, variante e serviço executado.

**Acceptance Scenarios**:

1. **Given** uma ação administrativa de draft, **When** o administrador a inicia, **Then** o site abre um modal contextual localizado em vez de `window.prompt`.
2. **Given** uma republicação Discord, **When** o modal abre, **Then** exibe o tipo e o status atual da publicação.
3. **Given** motivo vazio, **When** o administrador tenta confirmar, **Then** nenhum serviço é chamado.

---

### User Story 7 - Operar drafts com segurança e consistência verificável (Priority: P1)

Como administrador, quero que comandos, publicações e presenças tenham autorização, idempotência e contratos verificáveis, para operar drafts sem duplicações, efeitos indevidos ou falhas internas.

**Why this priority**: Os controles impedem abuso de comandos administrativos, indisponibilidade compartilhada, duplicação de mensagens e respostas 500 em operações repetidas ou concorrentes.

**Independent Test**: Executar a matriz de autenticação e autorização, duas tentativas concorrentes de publicação e presença, payloads inválidos e reconexão realtime; cada fluxo deve produzir estado único, resposta localizada e nenhuma exposição operacional indevida.

**Acceptance Scenarios**:

1. **Given** um membro Discord sem permissão administrativa, **When** executa comando mutável de draft, **Then** recebe negação localizada e nenhuma chamada mutável chega ao backend.
2. **Given** duas instâncias do bot tentando publicar o mesmo draft, **When** ambas solicitam autorização de envio, **Then** apenas uma adquire o claim persistido e pode enviar.
3. **Given** um envio cujo resultado ficou desconhecido após queda do bot, **When** o claim deixa de estar ativo, **Then** a publicação exige reconciliação administrativa e não é reenviada automaticamente.
4. **Given** confirmações ou cancelamentos repetidos ou concorrentes, **When** o estado desejado já foi alcançado, **Then** todas as chamadas terminam sem erro interno e existe uma única presença efetiva.
5. **Given** um usuário comum autenticado, **When** consulta um draft, **Then** não recebe motivos de auditoria nem identificadores operacionais do Discord.
6. **Given** uma publicação pendente ou em andamento de presença, chamada de presença ou times, **When** o bot lista o trabalho operacional, **Then** ela é retornada sem depender de guild, status final ou posição entre os 50 drafts mais recentes.
7. **Given** a mensagem principal de presença foi publicada, **When** a chamada de presença falha, **Then** a publicação principal permanece concluída e somente a chamada fica recuperável.
8. **Given** duas buscas manuais se sobrepõem ou o administrador troca de draft, **When** uma resposta antiga chega por último, **Then** ela não substitui os resultados da busca e do draft atuais.

---

### Edge Cases

- Link Discord aponta para draft inexistente, cancelado ou sem permissão de visualização.
- Integração Discord está desativada enquanto há drafts aguardando publicação.
- Canal Discord existe, mas o bot não pode enviar mensagem, incorporar links ou mencionar o cargo configurado.
- Usuário confirma presença pelo Discord sem conta vinculada, com perfil incompleto ou jogador inativo.
- Duas confirmações do mesmo jogador chegam quase ao mesmo tempo pelo site e pelo bot.
- Bot reinicia após publicar presença, antes ou depois de registrar o identificador da mensagem no backend.
- Publicação no Discord é apagada manualmente por alguém no servidor.
- Draft é encerrado automaticamente com menos jogadores que o mínimo esperado.
- Administrador remove presença ou cancela draft sem informar motivo.
- Conexão em tempo real cai e reconecta durante uma alteração de presença ou pick.
- Duas instâncias do bot executam o polling ao mesmo tempo.
- Bot cai depois de enviar ao Discord, antes de concluir o registro da publicação.
- Cliente anônimo esgota seu limite de requisições sem afetar usuários autenticados ou o bot.
- Token interno usa valor vazio, curto ou placeholder conhecido em produção.
- Identidade administrativa autenticada não possui identificador de usuário válido.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O site MUST abrir automaticamente o draft indicado pelo link externo quando o identificador estiver presente e acessível.
- **FR-002**: O site MUST informar de forma clara quando um link externo aponta para draft inexistente, indisponível ou inacessível.
- **FR-003**: O bot MUST respeitar a configuração de integração ativada/desativada antes de criar, publicar ou interagir com drafts.
- **FR-004**: O bot MUST rejeitar datas e horários passados, mantendo orientação clara para uso de horário de Brasília.
- **FR-005**: O bot MUST transformar erros conhecidos do sistema em mensagens amigáveis específicas sem depender de texto técnico bruto.
- **FR-006**: O bot MUST diferenciar falhas de canal, permissão de envio, incorporação e menção de cargo.
- **FR-007**: O sistema MUST persistir o estado de publicação Discord suficiente para evitar republicação duplicada após reinício.
- **FR-008**: Administradores MUST conseguir republicar uma lista de presença ou times quando a publicação falhar, estiver ausente ou for removida.
- **FR-009**: O site MUST exibir estado visível da publicação Discord para presença e times quando aplicável.
- **FR-010**: Confirmação e cancelamento de presença MUST ser idempotentes para o mesmo jogador dentro do mesmo draft.
- **FR-011**: O sistema MUST impedir duplicidade de jogador confirmado no mesmo draft mesmo sob tentativas simultâneas.
- **FR-012**: Alterações de presença feitas pelo site, bot ou administração MUST atualizar usuários conectados ao draft em tempo real.
- **FR-013**: Administradores MUST buscar jogadores elegíveis para presença manual sem carregar toda a base de jogadores no cliente.
- **FR-014**: A busca de presença manual MUST excluir jogadores já confirmados e jogadores inelegíveis.
- **FR-015**: Ações administrativas sensíveis no draft MUST registrar responsável, momento e motivo quando o motivo for aplicável.
- **FR-016**: Cancelamento de draft e remoção administrativa de presença SHOULD solicitar motivo ao administrador.
- **FR-017**: O sistema MUST registrar métricas ou logs estruturados para etapas críticas: confirmação, cancelamento, encerramento, publicação Discord, republicação, picks e timeouts.
- **FR-018**: O fluxo MUST continuar tendo alternativa manual quando Discord estiver desativado ou indisponível.
- **FR-019**: Textos novos de frontend MUST usar as chaves de internacionalização existentes nos idiomas suportados.
- **FR-020**: Mensagens novas de backend MUST usar recursos localizados.
- **FR-021**: O frontend MUST substituir prompts nativos de cancelamento, remoção manual e republicação por modal contextual.
- **FR-022**: O modal MUST exigir motivo não vazio e impedir envio duplicado durante processamento.
- **FR-023**: Republicações MUST mostrar tipo e status atual; remoção manual MUST mostrar o jogador afetado.
- **FR-024**: O modal MUST funcionar por teclado, controlar foco e usar somente textos internacionalizados.
- **FR-025**: Comandos mutáveis do Discord MUST exigir `ManageGuild` ou cargo listado em `DRAFT_ADMIN_ROLE_IDS`, com verificação no registro e antes de qualquer efeito colateral.
- **FR-026**: O backend MUST rejeitar inicialização em produção quando o token interno estiver ausente, tiver menos de 32 caracteres ou usar placeholder conhecido.
- **FR-027**: A comparação do token interno MUST resistir a diferenças observáveis de tempo e nunca registrar o segredo.
- **FR-028**: O rate limiting MUST isolar clientes por identidade do bot, usuário autenticado ou endereço IP anônimo.
- **FR-029**: Erros de domínio conhecidos MUST preservar seu código estável em `messageCode` e localizar a mensagem sem expor detalhes técnicos.
- **FR-030**: A publicação Discord MUST adquirir claim atômico persistido antes do envio e somente o detentor do claim pode concluir ou registrar falha.
- **FR-031**: Uma tentativa de publicação com resultado desconhecido MUST entrar em `RequerReconciliacao` e MUST NOT ser reenviada automaticamente.
- **FR-032**: Toda interação mutável do bot MUST verificar `botEnabled` antes de chamar o backend.
- **FR-033**: Falha ao publicar um draft MUST NOT interromper o processamento dos demais drafts no mesmo ciclo.
- **FR-034**: Motivo e executor válidos MUST ser exigidos no backend para cancelamento, presença manual e republicação quando aplicável.
- **FR-035**: Payloads de publicação MUST validar tipo, claim, obrigatoriedade e limites antes de parsing ou persistência.
- **FR-036**: Mudanças de publicação e ações administrativas MUST emitir estado público atualizado via SignalR após persistência bem-sucedida.
- **FR-037**: Respostas comuns e realtime MUST NOT expor motivos administrativos, executor, códigos de falha ou identificadores operacionais do Discord.
- **FR-038**: Cancelamento de draft MUST registrar métrica estruturada sem dados pessoais, motivos ou segredos.
- **FR-039**: Cobertura de endpoint MUST exigir requisição comportamental com assertivas de status, resposta e persistência; listas estáticas não contam como cobertura.
- **FR-040**: A listagem operacional do bot MUST retornar todo draft que possua publicação `Pendente` ou `EmAndamento`, além dos drafts ainda candidatos a uma publicação aplicável, sem depender de `DiscordGuildId` e sem limite arbitrário que cause starvation; histórico finalizado sem ação MUST ser excluído.
- **FR-041**: A chamada com menção de cargo MUST ser uma publicação `ChamadaPresenca` independente da publicação principal `Presenca`, com claim, conclusão, falha e reconciliação próprios.
- **FR-042**: `ChamadaPresenca` MUST ser candidata somente quando `DRAFT_NOTIFY_ROLE_ID` estiver configurado; sua recuperação MUST NOT republicar a mensagem principal de presença.
- **FR-043**: A busca manual de jogadores MUST descartar respostas que não correspondam simultaneamente ao draft ativo, geração ativa, termo atual e versão mais recente da requisição, cancelando a requisição anterior quando possível.
- **FR-044**: A comparação do token interno MUST aplicar SHA-256 aos dois valores UTF-8 e comparar os hashes de tamanho fixo com `FixedTimeEquals`, sem decisão antecipada pelo comprimento original.

### Key Entities *(include if feature involves data)*

- **Draft**: Evento de montagem de times, com status, presença, participantes, reservas, capitães, picks e estado de publicação.
- **Jogador**: Pessoa elegível para participar de drafts, com status ativo/inativo, perfil, vínculo de usuário e preferências.
- **Presença**: Participação de um jogador em um draft, com origem, status, ordem e histórico de alteração.
- **Publicação Discord**: Registro operacional de uma mensagem publicada, tipo de publicação, canal, mensagem, status e última falha conhecida.
- **Ação Administrativa**: Registro de alteração sensível feita por administrador, incluindo responsável, data e motivo quando aplicável.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos links de convite com draft válido abrem o draft correto sem seleção manual.
- **SC-002**: 100% dos comandos do bot bloqueiam criação/publicação quando a integração estiver desativada.
- **SC-003**: 95% das falhas conhecidas do bot exibem mensagem específica e amigável em vez de erro genérico.
- **SC-004**: Após reinício do bot, nenhum draft já publicado gera publicação duplicada em varredura normal.
- **SC-005**: Confirmações simultâneas do mesmo jogador resultam em no máximo uma presença confirmada por draft.
- **SC-006**: Usuários com o draft aberto veem mudanças de presença em até 5 segundos em condições normais de conexão.
- **SC-007**: Administradores conseguem localizar jogador elegível para presença manual em até 10 segundos sem carregar toda a base no cliente.
- **SC-008**: 100% das ações administrativas sensíveis da feature registram responsável e momento.
- **SC-009**: Usuários conseguem continuar o fluxo de draft pelo site mesmo quando Discord está desativado ou indisponível.
- **SC-010**: 100% das quatro ações com motivo na tela de drafts usam confirmação contextual integrada à interface.
- **SC-011**: 100% dos comandos mutáveis do Discord negam membros não autorizados antes de qualquer chamada mutável.
- **SC-012**: Duas tentativas concorrentes de publicação concedem exatamente um claim e produzem no máximo uma mensagem automática.
- **SC-013**: Nenhuma tentativa em `RequerReconciliacao` é reenviada sem ação administrativa explícita.
- **SC-014**: Confirmações e cancelamentos repetidos ou concorrentes não retornam HTTP 500 e mantêm uma única presença efetiva.
- **SC-015**: Saturar o limite de um cliente não reduz a cota de um usuário, bot ou IP diferente.
- **SC-016**: 100% dos endpoints críticos novos possuem testes negativos de autenticação, autorização, esquema incorreto e payload inválido.
- **SC-017**: Testes e builds de backend, frontend e bot aprovam com relógio determinístico e catálogos localizados sincronizados.
- **SC-018**: Nenhuma publicação acionável sofre starvation por guild ausente, status finalizado ou volume superior a 50 drafts, e nenhum histórico finalizado irrelevante entra no polling.
- **SC-019**: Falha ou resultado incerto da CTA altera somente `ChamadaPresenca`; recuperar a CTA produz no máximo uma CTA nova e nenhuma mensagem principal adicional.
- **SC-020**: Respostas atrasadas de busca manual nunca substituem resultados de termo, geração ou draft mais recentes.

## Assumptions

- O fluxo manual pelo site permanece como fonte confiável quando o Discord estiver desativado ou falhar.
- Administradores incluem SuperAdmin, Admin e Moderador conforme permissões atuais de gerenciamento de drafts.
- Jogadores elegíveis para presença manual são jogadores ativos, com perfil associado a usuário, ainda não confirmados no draft selecionado.
- Publicações Discord relevantes para esta feature são listas de presença, CTA de presença e times definidos.
- Métricas e logs não precisam expor dados sensíveis, tokens ou payloads técnicos ao usuário final.
- `DRAFT_ADMIN_ROLE_IDS` é uma lista opcional de IDs de cargos separados por vírgula; `ManageGuild` permanece como permissão administrativa segura padrão.
- Migrações de julho de 2026 da feature 017 ainda não foram publicadas e podem ser consolidadas antes do merge.
