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
- `ChamadaPresenca`: CTA opcional com menção ao cargo configurado.
- `TimesDefinidos`: publicação dos times finais.

Estados possíveis:

- `Pendente`: o bot deve publicar no próximo processamento.
- `EmAndamento`: um claim exclusivo foi adquirido e o resultado ainda não foi concluído.
- `Publicada`: há publicação registrada com `message_id`.
- `Falha`: houve erro operacional na tentativa.
- `RequerReconciliacao`: o envio ou a conclusão ficou incerto e exige ação administrativa.
- `Ignorada`: publicação intencionalmente não executada.

O bot usa o estado persistido para evitar duplicação após restart. A listagem operacional não depende de guild, não limita o trabalho aos 50 drafts mais recentes e inclui publicações pendentes, em andamento ou que exigem reconciliação mesmo em drafts com status terminal. Histórico finalizado ou cancelado sem publicação acionável fica fora do polling.

A mensagem principal `Presenca` e a CTA `ChamadaPresenca` possuem claims e resultados independentes. A primeira é concluída assim que o embed é registrado. A CTA só é candidata quando `DRAFT_NOTIFY_ROLE_ID` está configurado; falha conhecida antes do envio vira `Falha`, enquanto resultado incerto permanece `EmAndamento` até reconciliação. Recuperar a CTA não republica o embed principal.

### Drafts criados por agendamento

O scheduler do backend cria um `DraftMontagem` comum com publicação `Presenca` em `Pendente`. O polling atual encontra esse draft sem endpoint, campo ou timer adicional no bot. O bot não conhece a agenda, não calcula recorrência e não avalia horários: ele adquire o claim de publicação, envia o embed e a CTA e conclui cada publicação pelo protocolo descrito acima.

Os limites de duplicidade são complementares. O scheduler garante um draft por agenda e data; o claim da publicação garante uma mensagem principal confirmada por draft. Uma falha de envio não cria draft compensatório. Consulte [Agendamento Recorrente de Listas de Presença](./AGENDAMENTO_LISTAS_PRESENCA.md) para operação e recuperação do scheduler.

## Republicação

Administradores podem solicitar republicação pelo frontend. A ação:

- muda o estado da publicação para `Pendente`;
- registra ação administrativa com usuário responsável e motivo;
- permite que o bot publique novamente e registre o novo `message_id`.

O frontend mostra os três tipos separadamente. A ação adicional de republicar somente a chamada aparece quando `ChamadaPresenca` está em `Falha` ou `RequerReconciliacao`; as ações existentes de presença e times permanecem inalteradas.

## Cancelamento do draft

O cancelamento administrativo do draft é exclusivo do site. O bot não registra o comando `/draft-cancelar` e não chama o endpoint de cancelamento do draft. O botão de cancelar presença no Discord continua disponível porque altera somente a presença do próprio jogador.

No site, o cancelamento exige usuário administrativo autenticado e motivo não vazio, com no máximo 500 caracteres.

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

O frontend cancela a busca anterior ao iniciar outra e só aplica uma resposta quando draft, geração, termo e versão da requisição ainda correspondem ao contexto ativo.

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
- `timeout`;
- `draft_cancelled`.

O contador de cancelamento é incrementado somente após a persistência efetiva. Ele usa apenas as tags padrão `draft_id` e `action`; motivo, executor e identificadores do Discord não são enviados para a métrica. Tentativas repetidas rejeitadas pelo domínio e falhas de persistência não incrementam o contador.

## Validação Operacional

Execute as verificações principais após mudanças nesse fluxo:

- `dotnet test BackEnd/RinhaDasLendas.sln --configuration Release` no container de desenvolvimento;
- `npm test` e `npm run build` em `FrontEnd/`;
- `npm test` e `npm run build` em `discord-bot/`.
