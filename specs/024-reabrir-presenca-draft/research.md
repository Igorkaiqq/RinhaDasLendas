# Research: Reabertura de Presença do Draft

## Transição e dados derivados

**Decision**: Permitir somente `PresencaEncerrada → PresencaAberta`, preservar presenças e zerar apenas quantidades derivadas, continuação excepcional e prazo automático.

**Rationale**: Antes dos capitães não existem times, participantes de escolha ou ordem que precisem ser descartados. O próximo encerramento recalcula a estrutura com a lista atual.

**Alternatives considered**: Aceitar confirmações com status encerrado manteria dados contraditórios; voltar depois dos capitães exigiria perda de progresso; manter o prazo vencido encerraria novamente em até 30 segundos.

## Contrato HTTP e autorização

**Decision**: Usar `PATCH /api/v1/draft-montagens/{id}/reabrir-presenca`, protegido somente por `CanManageDrafts`.

**Rationale**: A operação altera parcialmente o estado do draft e exige identidade humana para auditoria. O endpoint retorna a projeção pública atualizada e mantém o padrão de 404 e erro localizado existente.

**Alternatives considered**: `POST` seria menos aderente ao padrão de atualização parcial; permitir o bot criaria autoria ambígua e não resolve a necessidade operacional relatada.

## Persistência e concorrência

**Decision**: Reutilizar a persistência do agregado e `DraftMontagemAcaoAdministrativa`, sem migration, e salvar a mudança inteira uma única vez.

**Rationale**: O modelo já possui todos os campos e tabela de auditoria necessários. O `VersaoEstado` atualizado por `Touch()` e o tratamento existente de concorrência evitam estado parcial.

**Alternatives considered**: Nova tabela ou colunas específicas adicionariam complexidade sem informação nova.

## Interface e confirmação

**Decision**: Reutilizar `DraftReasonDialog` como confirmação sem motivo e apresentar a quantidade selecionada/exigida de capitães no painel existente.

**Rationale**: A ação é importante, mas não destrutiva; o componente já resolve foco, bloqueio durante salvamento e responsividade. A contagem elimina a ambiguidade observada com 19 jogadores.

**Alternatives considered**: `window.confirm` quebraria o padrão visual/acessível; novo diálogo duplicaria comportamento; habilitar quatro capitães em 19 alteraria a regra correta de times completos.

## Atualização em tempo real e Discord

**Decision**: Publicar o estado atualizado pelo notifier existente e não criar publicação ou interação Discord nova.

**Rationale**: Observadores web precisam refletir a transição, mas a reabertura administrativa não deve depender de integração externa nem republicar mensagens automaticamente.

**Alternatives considered**: Reabrir/republicar no Discord como uma única operação aumentaria escopo, acoplamento e risco de duplicação.
