# Data Model: Draft de Jogadores

## DraftSessao

Representa uma sessao de draft.

Fields:
- `id`: identificador unico.
- `nome`: nome exibido da sessao, obrigatorio, maximo 120 caracteres.
- `observacoes`: texto opcional, maximo 500 caracteres.
- `status`: `Aberto`, `Concluido` ou `Cancelado`.
- `tamanho_time`: quantidade maxima de jogadores por time, entre 1 e 5.
- `capitao_time_a_id`: jogador capitao do time A.
- `capitao_time_b_id`: jogador capitao do time B.
- `criterio_capitaes`: `Manual` ou `Sorteio`.
- `proximo_time`: `TimeA` ou `TimeB` enquanto a sessao estiver aberta.
- `criterio_primeiro_pick`: `Manual` ou `Sorteio`.
- `motivo_cancelamento`: texto opcional registrado ao cancelar.
- `data_cadastro`: data de criacao.
- `data_atualizacao`: data da ultima alteracao.

Relationships:
- Possui muitos `DraftParticipante`.
- Possui muitas `DraftEscolha`.
- Referencia dois jogadores como capitaes.

Validation:
- Capitaes devem ser distintos.
- Capitaes devem estar entre participantes elegiveis.
- Sessao aberta aceita picks; concluida ou cancelada nao aceita.
- Quando os dois times atingem `tamanho_time`, status muda para `Concluido`.

## DraftParticipante

Representa um jogador incluido na sessao.

Fields:
- `id`: identificador unico.
- `draft_sessao_id`: sessao associada.
- `jogador_id`: jogador participante.
- `time`: sem time, `TimeA` ou `TimeB`.
- `capitao`: indica se o participante e capitao.
- `data_cadastro`: data de inclusao.

Relationships:
- Pertence a uma `DraftSessao`.
- Referencia um `Jogador`.

Validation:
- Um jogador nao pode aparecer duas vezes na mesma sessao.
- Participante capitao ja inicia no respectivo time.
- Participante escolhido deixa de estar disponivel.

## DraftEscolha

Representa uma escolha feita durante o draft.

Fields:
- `id`: identificador unico.
- `draft_sessao_id`: sessao associada.
- `sequencia`: ordem da escolha, iniciando em 1.
- `time`: `TimeA` ou `TimeB`.
- `capitao_id`: capitao responsavel pela escolha.
- `jogador_id`: jogador escolhido.
- `data_escolha`: momento do registro.

Relationships:
- Pertence a uma `DraftSessao`.
- Referencia jogador escolhido e capitao.

Validation:
- Sequencia e unica por sessao.
- Jogador escolhido deve estar disponivel.
- Time da escolha deve ser o `proximo_time` da sessao.

## State Transitions

- `Aberto` -> `Concluido`: automaticamente quando ambos os times atingem o limite ou quando nao ha mais escolhas validas.
- `Aberto` -> `Cancelado`: por acao do organizador.
- `Concluido` e `Cancelado` sao estados finais nesta etapa.
