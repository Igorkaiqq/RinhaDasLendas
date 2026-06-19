# Research: Draft de Jogadores

## Decision: Draft alternado simples para MVP

Rationale: A especificacao pede registro da ordem de escolha, transparencia e fluxo manual funcional. Draft alternado Team A/Team B e mais facil de explicar, testar e operar do que snake draft.

Alternatives considered: Snake draft; rejeitado para esta etapa por adicionar regra extra sem requisito explicito. Draft automatico por balanceamento; rejeitado porque a etapa e caracterizada por escolhas de capitaes.

## Decision: Capitaes contam como membros do time

Rationale: A constituicao define dois times de ate cinco jogadores. Capitaes sao jogadores participantes e devem aparecer nos times finais, evitando composicao de seis pessoas.

Alternatives considered: Capitaes fora da contagem; rejeitado por nao representar uma partida padrao de League of Legends.

## Decision: Persistir participantes e escolhas de forma relacional

Rationale: O historico de escolhas e a prevencao de duplicidade dependem de integridade relacional, consultas claras e futuras ligacoes com partidas/estatisticas.

Alternatives considered: Armazenar listas em JSON; rejeitado por contrariar diretrizes de banco e dificultar chaves estrangeiras para jogadores.

## Decision: Regras criticas na entidade de dominio

Rationale: Duplicidade de jogador, status da sessao, ordem de pick e conclusao automatica sao invariantes de dominio e devem sobreviver a qualquer cliente ou chamada direta de API.

Alternatives considered: Validar apenas no handler ou frontend; rejeitado por risco de inconsistencia.

## Decision: UI em tela unica de operacao

Rationale: Draft e uma tela principal do sistema e precisa mostrar capitaes, ordem, jogadores disponiveis e picks sem navegação excessiva.

Alternatives considered: Wizard multi-etapas; rejeitado por tornar o uso durante a rinha mais lento.
