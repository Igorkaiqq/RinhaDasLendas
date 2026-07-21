# Operações de Draft, Presença e Discord

## Objetivo

Este documento descreve o comportamento operacional esperado para drafts visuais integrados ao bot Discord. O backend continua sendo a fonte da verdade; o bot atua como adaptador de publicação e o frontend oferece ações administrativas de recuperação.

## Link do Discord para o draft

- Links de CTA usam `/drafts?draftId=<id>`.
- Ao abrir a página, o frontend tenta carregar primeiro o draft informado por `draftId`.
- Se o draft não existir ou não estiver acessível, a página mantém a lista de drafts utilizável e exibe mensagem localizada.

## Publicações no Discord

Cada draft pode ter estado persistido de publicação por tipo:

- `Presenca`: mensagem de lista de presença.
- `TimesDefinidos`: publicação dos times finais.

Estados possíveis:

- `Pendente`: o bot deve publicar no próximo processamento.
- `Publicada`: há publicação registrada com `message_id`.
- `Falha`: houve erro operacional na tentativa.
- `Ignorada`: publicação intencionalmente não executada.

O bot usa o estado persistido para evitar duplicação após restart. Estado `Pendente` tem prioridade sobre o cache local e permite republicação operacional.

## Republicação

Administradores podem solicitar republicação pelo frontend. A ação:

- muda o estado da publicação para `Pendente`;
- registra ação administrativa com usuário responsável e motivo;
- permite que o bot publique novamente e registre o novo `message_id`.

## Presença

Presenças são idempotentes no domínio:

- confirmar presença já confirmada retorna a presença existente;
- presença cancelada pode ser substituída por nova confirmação;
- presença só pode mudar enquanto o draft está com presença aberta.

O banco protege concorrência com índices únicos filtrados por `status = 'Confirmada'` para:

- draft + usuário;
- draft + jogador.

## Presença manual

Administradores adicionam presença manual usando busca elegível no backend. A busca retorna jogadores ativos, vinculados a usuário e ainda não confirmados no draft.

Ao remover presença manual, o frontend solicita motivo. O backend registra:

- usuário responsável;
- jogador alvo;
- motivo;
- data da ação.

## Tempo real

Mudanças relevantes disparam atualização SignalR do estado do draft:

- confirmação e cancelamento de presença;
- adição e remoção manual de presença;
- picks e timeouts;
- cancelamento do draft.

Quando a conexão SignalR reconecta, o frontend entra novamente no grupo do draft e recarrega o estado atual.

## Métricas

A API registra métricas em `rinha_draft_actions_total` com tags por ação. Ações cobertas:

- `presence_confirmed`;
- `presence_cancelled`;
- `presence_closed`;
- `discord_publication`;
- `pick`;
- `timeout`.

## Validação Operacional

Execute as verificações principais após mudanças nesse fluxo:

- `dotnet test BackEnd/RinhaDasLendas.sln --configuration Release` no container de desenvolvimento;
- `npm test` e `npm run build` em `FrontEnd/`;
- `npm test` e `npm run build` em `discord-bot/`.
