# Draft Montagem em Tempo Real

## Objetivo

O draft montagem em tempo real evolui a montagem visual de times para permitir que capitães ativos escolham jogadores em uma ordem oficial, sincronizada para todos os usuários conectados.

## Fonte Oficial

O backend é a fonte oficial do estado do draft.

O frontend pode bloquear botões por experiência de uso, mas não decide regras críticas como:

- quem é o capitão da vez;
- se o jogador pode ser escolhido;
- se o turno expirou;
- se o jogador é reserva;
- se o time ainda possui vaga.

## Modos

`DraftMontagem` pode operar em dois modos:

- `Manual`: montagem visual por drag and drop.
- `TempoReal`: capitães escolhem jogadores por turno.

Quando a montagem está em `TempoReal`, o layout manual fica bloqueado.

## Capitães

Um capitão só pode escolher se:

- possui jogador vinculado ao usuário autenticado;
- é capitão de um time da montagem;
- está no turno atual;
- o draft está aberto;
- o turno ainda não expirou.

## Ordem de Escolha

A ordem segue `DraftMontagemTime.Ordem`.

Times completos são ignorados ao avançar o turno.

O draft finaliza quando todos os times estão completos ou quando não há jogadores livres elegíveis.

## Timer

Cada turno dura 30 segundos.

O timer é controlado pelo backend por meio de processamento periódico de turnos expirados. Se o capitão não escolher dentro do prazo, o sistema registra timeout e passa a vez.

## Reservas

Reservas não participam da escolha dos capitães.

Reservas representam complemento emergencial para casos em que um participante confirmado não comparece ou precisa sair.

Um reserva entra em time somente por substituição administrativa, com registro de:

- time;
- jogador que saiu;
- reserva que entrou;
- motivo;
- responsável;
- data/hora.

## Sincronização

Usuários conectados a uma montagem recebem atualizações quando ocorre:

- início do modo tempo real;
- escolha válida;
- timeout;
- cancelamento;
- finalização;
- substituição emergencial.

## Internacionalização

Todas as mensagens do backend devem usar resources.

Todos os textos visíveis no frontend devem usar chaves de i18n em português e inglês.
