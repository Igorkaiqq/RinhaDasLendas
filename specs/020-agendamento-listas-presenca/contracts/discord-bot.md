# Discord Bot Contract: Compatibilidade de Draft Agendado

## Invariant

O bot permanece adaptador de publicação. Ele não conhece `AgendamentoPresenca`, não calcula recorrência, não avalia `America/Sao_Paulo` e não decide se uma ocorrência deve existir.

## API Consumption

- **Nenhum endpoint novo é consumido pelo bot.**
- Em particular, o bot não chama `/api/v1/discord/agendamentos-presenca` nem qualquer rota de ocorrência.
- Nenhum DTO, status ou campo de agenda é adicionado ao contrato Node.js do bot.
- Nenhum timer de agenda ou dependência de scheduler é adicionado ao processo do bot.

## Existing Polling Protocol

1. O backend cria um `DraftMontagem` normal com publicação `Presenca` pendente.
2. O draft agendado entra no polling existente exatamente como qualquer outro draft operacionalmente acionável.
3. O bot adquire o claim persistido de publicação já existente.
4. O bot publica a mensagem principal no canal configurado.
5. O bot conclui, registra falha conhecida ou deixa resultado incerto para reconciliação pelo protocolo atual.
6. A CTA mantém claim, conclusão e recuperação independentes da mensagem principal.

## Deduplication Boundary

- O scheduler garante no máximo um draft por agenda/data.
- O protocolo existente do bot garante no máximo uma publicação principal confirmada por draft.
- Falha de publicação nunca solicita ou cria outro draft como compensação.
- Claim expirável da ocorrência e claim da publicação são mecanismos distintos; nenhum substitui o outro.

## Failure Semantics

- Falha conhecida continua no estado operacional `Falha` e permite republicação administrativa existente.
- Resultado incerto continua em `RequerReconciliacao`.
- Mensagem principal e CTA continuam com estados independentes.
- Bot desativado/configuração incompleta é verificado antes da criação do draft pelo backend; o bot não implementa fallback de recorrência.

## Regression Proof

Adicionar fixture de um draft agendado sem campo novo e executar `runDraftPollingCycle` em dois ciclos, comprovando:

- o draft é encontrado pelo polling existente;
- um claim vencedor é adquirido;
- embed e mensagem principal são enviados uma vez;
- conclusão e CTA seguem o fluxo existente;
- o segundo ciclo não duplica a mensagem;
- nenhuma regra ou endpoint de agenda é introduzido.

Se o teste passar sem mudança no código de produção do bot, somente a prova de regressão deve ser mantida.
