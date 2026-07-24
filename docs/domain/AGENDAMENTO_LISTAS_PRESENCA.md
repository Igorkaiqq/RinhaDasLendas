# Agendamento Recorrente de Listas de Presença

## Objetivo e fronteiras

O backend avalia agendas semanais em `America/Sao_Paulo` e cria, na mesma transação, uma ocorrência, um draft com times de cinco e uma publicação `Presenca` pendente. O bot apenas consome essa publicação pelo polling existente; não existe endpoint, DTO ou regra de recorrência no bot ou no frontend.

Moderador, Admin e SuperAdmin gerenciam agendas com `CanManageDrafts`. A configuração sensível do Discord continua restrita por `CanManageUsers`. Nenhuma resposta administrativa expõe claims, tokens, canais, IDs de mensagem ou detalhes técnicos.

## Ciclo de vida

### Criação e edição

1. Em `/configuracoes`, informe nome, observação opcional, ao menos um dia da semana e os horários de publicação e encerramento.
2. Os horários têm precisão de minuto, pertencem ao mesmo dia e o encerramento deve ser posterior à publicação.
3. Uma agenda nova fica ativa. Edições afetam somente datas cuja ocorrência ainda não foi criada.
4. Criação, edição, pausa, reativação e arquivamento registram autoria autenticada e os nomes dos campos alterados, sem valores sensíveis.

### Pausa, reativação e arquivamento

- Pausar impede novas ocorrências; ocorrências e drafts já criados permanecem inalterados.
- Reativar antes ou exatamente no horário de publicação mantém o dia elegível. Reativar depois desse horário não recupera o mesmo dia.
- Pausa e reativação repetidas são idempotentes.
- Arquivar é exclusão lógica e definitiva para a operação normal; o histórico é preservado.

### Bloqueio e recuperação

Quando o bot está desativado ou a configuração necessária do Discord está incompleta, o backend cria uma ocorrência `Bloqueada` sem draft. Todo ciclo consulta bloqueadas em uma fase independente do marcador da agenda:

- se a configuração voltar antes do encerramento, um novo claim é adquirido e o draft é criado;
- se a indisponibilidade continuar, a ocorrência permanece bloqueada sem reescrever timestamps;
- se o encerramento passar, a ocorrência se torna `Perdida` e nenhum draft é criado.

Após indisponibilidade do serviço, todas as datas posteriores ao marcador são classificadas, sem corte por idade. Datas ainda dentro da janela podem criar drafts atrasados; datas encerradas ficam `Perdida`. O processamento avança em lotes e continua nos ciclos seguintes.

## Claims e deduplicação

O claim da ocorrência pertence ao scheduler, dura cinco minutos e protege a criação transacional do draft. O PostgreSQL usa seu próprio relógio para aquisição, expiração e validação da janela. Um processo interrompido pode ser retomado após a expiração, desde que o encerramento ainda não tenha ocorrido.

O claim da publicação pertence ao protocolo do bot e é independente. As barreiras são:

- uma ocorrência por agenda e data local;
- um draft por ocorrência;
- uma publicação principal confirmada por draft;
- estados e claims separados para a mensagem principal e a CTA.

Não remova claims nem altere estados diretamente no banco durante a operação normal.

## Falhas de envio

- Falha conhecida antes do envio registra `Falha` na publicação e permite a republicação administrativa existente.
- Resultado incerto durante envio ou conclusão exige reconciliação; não repita manualmente a mensagem sem verificar o Discord.
- Falha da CTA não invalida nem republica a mensagem principal.
- Nenhuma falha de publicação solicita um novo draft ao scheduler.

O procedimento detalhado de publicação está em [Operações de Draft, Presença e Discord](./DRAFT_DISCORD_OPERATIONS.md).

## Configuração e limites de lote

O ciclo usa `PresenceSchedule:IntervalSeconds`, com padrão de 30 segundos, e não sobrepõe execuções dentro da mesma instância. Os limites ficam na seção `PresenceSchedule`:

| Chave | Padrão | Faixa aceita | Efeito |
|-------|--------|--------------|--------|
| `MaxBlockedPerCycle` | 50 | 1-1000 | Bloqueadas reavaliadas por ciclo |
| `MaxSchedulesPerCycle` | 50 | 1-1000 | Agendas candidatas por ciclo |
| `MaxDatesPerSchedulePerCycle` | 31 | 1-366 | Datas classificadas por agenda e ciclo |

Os limites controlam carga, não descartam backlog. O marcador persistido e o cursor circular retomam o trabalho nos ciclos seguintes e evitam que uma agenda com falha permanente impeça as demais.

## Métricas e diagnóstico

O meter `RinhaDasLendas.PresenceSchedule` publica:

| Métrica | Interpretação operacional |
|---------|--------------------------|
| `rinha_presence_schedule_evaluated_total` | Agendas candidatas avaliadas |
| `rinha_presence_schedule_created_total` | Ocorrências concluídas com draft |
| `rinha_presence_schedule_blocked_total` | Novas ocorrências bloqueadas |
| `rinha_presence_schedule_missed_total` | Ocorrências encerradas sem draft |
| `rinha_presence_schedule_failures_total` | Falhas classificadas por código público estável |
| `rinha_presence_schedule_conflicts_total` | Conflitos concorrentes por código público estável |
| `rinha_presence_schedule_cycle_duration_ms` | Duração do ciclo em milissegundos |

Nomes, observações, usuários, claims, tokens, guilds, canais, payloads e IDs de mensagem não entram em tags. Diagnósticos técnicos usam etapa fechada, tipo de exceção e código público estável.

## Runbook

### Agenda não executou

1. Confirme que a agenda está ativa, inclui o dia local e possui janela ainda aberta.
2. Consulte o histórico da agenda e identifique `Bloqueada`, `Perdida` ou `Falha` pelo código público.
3. Verifique se o bot está ativado e se a configuração do Discord está completa, sem registrar ou compartilhar segredos.
4. Compare os contadores de avaliadas, bloqueadas, perdidas e falhas e a duração do ciclo.
5. Aguarde o próximo ciclo após corrigir configuração. Não crie outro draft se já existir ocorrência ou draft para a data.

### Ocorrência bloqueada

1. Corrija ativação, guild e canais pela interface autorizada.
2. Mantenha a agenda ativa e aguarde a reavaliação independente.
3. Antes do encerramento, confirme a mudança para `Criada`; após o encerramento, o resultado esperado é `Perdida`.

### Publicação não apareceu no Discord

1. Confirme que o draft existe e que `Presenca` está `Pendente`, `Falha` ou `RequerReconciliacao`.
2. Em `RequerReconciliacao`, procure a mensagem no canal antes de qualquer republicação.
3. Use a ação administrativa de republicação somente para falha conhecida ou após reconciliação.
4. Não altere a ocorrência do scheduler nem crie draft compensatório.

### Backlog ou ciclos lentos

1. Observe `rinha_presence_schedule_cycle_duration_ms` e os contadores entre ciclos.
2. Confirme que o backlog continua avançando e que conflitos não crescem continuamente.
3. Ajuste limites gradualmente dentro das faixas documentadas; reinicie a aplicação para aplicar configuração.
4. Reverta o ajuste se banco, API ou latência do ciclo degradarem.

### Recuperação após reinício

1. Inicie normalmente a API; não limpe claims manualmente.
2. Claims expirados tornam-se retomáveis após cinco minutos e somente dentro da janela.
3. Confirme que datas antigas são classificadas em lotes até o marcador alcançar a data atual.
4. Valide que cada agenda e data possui no máximo uma ocorrência e um draft.

## Validação operacional

- Bot: `npm test --prefix discord-bot` e `npm run build --prefix discord-bot`.
- Frontend do histórico: `npm test --prefix FrontEnd -- src/constants/systemUpdates.spec.ts src/services/systemUpdates.spec.ts src/i18n/i18n.spec.ts`.
- Backend: execute a suíte Release pelo devcontainer conforme `AGENTS.md` quando houver mudança no scheduler.
- Documentos e whitespace: `git diff --check`.
