# Correção do Ciclo Completo de Draft

**Data**: 2026-07-29

**Status**: Aprovado para especificação incremental

## Contexto

A auditoria do ciclo de draft identificou falhas nas regras centrais, na sincronização em tempo real e nas publicações Discord. As correções serão entregues em três features encadeadas para reduzir risco, permitir verificação independente e evitar que uma mudança simultânea em domínio, frontend e bot esconda regressões.

## Regra de Capitão

O cargo global `Capitão` indica que um jogador pode exercer essa função, mas não concede autoridade permanente em todos os drafts.

Em cada rinha diária, Admin+ seleciona os capitães daquele draft antes do início das escolhas em tempo real. Somente jogadores ativos, confirmados, incluídos no recorte titular pela ordem de presença e com cargo global `Capitão` são elegíveis.

Um jogador com cargo global `Capitão` que não foi selecionado como capitão daquele draft permanece jogador comum. Ele pode ser escolhido por outro capitão e não recebe autoridade de escolha no novo time.

Um jogador fora do recorte titular permanece reserva mesmo que tenha cargo global `Capitão`. Se houver substituição do capitão diário, Admin+ escolhe explicitamente o novo capitão entre os jogadores elegíveis. A entrada de uma reserva com cargo `Capitão` não transfere autoridade automaticamente.

## Feature 028 - Núcleo do Ciclo

Depois do encerramento da presença, Admin+ escolhe o modo da montagem.

- `Manual`: segue diretamente para o board de montagem. Somente Admin+ pode distribuir jogadores. Não exige capitães nem ordem de picks. A finalização exige todos os titulares distribuídos e todos os times completos.
- `TempoReal`: exige a seleção dos capitães diários, a definição da ordem e uma ação explícita de início. O estado `OrdemDefinida` passa a ser alcançável e representa a preparação concluída antes do primeiro turno.

A criação direta com jogadores previamente selecionados continua disponível somente para Admin+ e nasce como montagem `Manual`. Ela não passa por presença, seleção de modo, capitães ou ordem de picks.

Timeout registra uma tentativa consumida no histórico e avança o ordinal global, mas o time recebe uma nova oportunidade em rodada posterior até preencher sua vaga.

Depois de `Finalizada`, nenhuma alteração de layout, capitão, pick ou substituição é aceita. Durante o tempo real, alterações de capitão atualizam atomicamente time, participante e autoridade do turno.

Drafts ativos criados antes da mudança preservam o fluxo legado até alcançarem estado terminal. O novo fluxo vale para drafts criados depois da ativação da feature.

## Feature 029 - Sincronização e Operação

O broadcast SignalR carregará apenas estado compartilhado. Capacidades personalizadas, como permissão de pick do usuário atual, serão resolvidas por conexão ou consulta individual.

Toda transição relevante publicará atualização. Falha de notificação posterior ao commit não transformará uma operação persistida em falha funcional. Workers usarão autoria sistêmica, isolarão falhas por draft e continuarão processando o lote.

O frontend separará versões de mutação e refresh passivo, tratará busca auxiliar como enriquecimento opcional, recuperará falhas de conexão e protegerá layout manual não salvo contra atualizações remotas e navegação.

## Feature 030 - Publicações e Discord

A finalização criará a intenção `TimesDefinidos/Pendente` na mesma transação. Fechamento, reabertura e cancelamento criarão intenções duráveis de atualizar a mensagem principal.

Pendências de presença inaplicáveis serão encerradas, enquanto presença reaberta sem prazo continuará publicável até ação administrativa. Polling será sequencial e terá timeout. Claims continuarão garantindo idempotência.

Interações, configuração e draft deverão pertencer à mesma guild. Credenciais internas serão separadas por capacidade. Datas serão interpretadas em `America/Sao_Paulo`.

## Estratégia de Testes

Cada feature terá testes de domínio, handlers, integração e interfaces afetadas. Ao final das três features, uma jornada automatizada cobrirá:

`publicação -> presença -> fechamento -> modo -> capitães -> ordem -> início -> picks/timeouts -> substituição -> finalização -> publicação dos times`.

Cenários multicliente, bot offline, retry, concorrência, cross-guild e compatibilidade de drafts legados serão obrigatórios nas features correspondentes.

## Fora de Escopo

- Promover automaticamente um jogador ao cargo global `Capitão`.
- Tornar capitão diário todo jogador com cargo global `Capitão`.
- Alterar a ordem de presença para favorecer capitães confirmados tarde.
- Entregar as três features em um único deploy sem verificação intermediária.
