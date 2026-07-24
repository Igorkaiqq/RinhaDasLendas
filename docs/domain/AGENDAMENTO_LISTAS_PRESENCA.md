# Agendamento Recorrente de Listas de Presença

## Objetivo e fronteiras

O backend avalia agendas semanais em `America/Sao_Paulo`. O bot apenas consome o draft pelo polling existente; não existe endpoint, DTO ou regra de recorrência adicional no bot ou no frontend.

Moderador, Admin e SuperAdmin gerenciam agendas com `CanManageDrafts`. A configuração sensível do Discord continua restrita por `CanManageUsers`. Nenhuma resposta administrativa expõe claims, tokens, canais, IDs de mensagem ou detalhes técnicos.

## Rastreabilidade

- [Especificação da feature](../../specs/020-agendamento-listas-presenca/spec.md)
- [Plano técnico](../../specs/020-agendamento-listas-presenca/plan.md)
- [Modelo de dados](../../specs/020-agendamento-listas-presenca/data-model.md)
- [Contrato da API backend](../../specs/020-agendamento-listas-presenca/contracts/backend-api.md)
- [Contrato do bot Discord](../../specs/020-agendamento-listas-presenca/contracts/discord-bot.md)
- [Contrato da interface](../../specs/020-agendamento-listas-presenca/contracts/frontend-ui.md)
- [Design aprovado](../superpowers/specs/2026-07-23-agendamento-listas-presenca-design.md)
- [Plano de implementação aprovado](../superpowers/plans/2026-07-23-agendamento-listas-presenca.md)

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

O processamento possui duas etapas transacionais separadas:

1. `TryClaimOccurrenceAsync` cria ou readquire a ocorrência em `Processando`, grava o claim de cinco minutos e confirma essa primeira transação.
2. `TryCompleteWithDraftAsync` abre outra transação, valida claim e janela pelo relógio do PostgreSQL e, como uma unidade, cria o draft, cria a publicação `Presenca/Pendente` e conclui a ocorrência como `Criada`.

Se o processo cair entre as etapas, a ocorrência `Processando` permanece sem draft. Após a expiração de cinco minutos, outro ciclo pode readquirir o claim enquanto a janela estiver aberta. Se a segunda transação falhar, seu rollback não deixa draft ou publicação parcial.

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

O ciclo não sobrepõe execuções dentro da mesma instância. As chaves ficam na seção `PresenceSchedule`:

| Chave | Padrão | Faixa efetiva após normalização | Efeito |
|-------|--------|----------------------------------|--------|
| `IntervalSeconds` | 30 | 1-3600 | Intervalo entre ciclos, em segundos |
| `MaxBlockedPerCycle` | 50 | 1-1000 | Bloqueadas reavaliadas por ciclo |
| `MaxSchedulesPerCycle` | 50 | 1-1000 | Agendas candidatas por ciclo |
| `MaxDatesPerSchedulePerCycle` | 31 | 1-366 | Datas classificadas por agenda e ciclo |

Todos os valores passam por `Math.Clamp`: valores abaixo ou acima da faixa são substituídos silenciosamente pelo limite mais próximo, sem erro de configuração. Os limites controlam carga, não descartam backlog. O marcador persistido e o cursor circular retomam o trabalho nos ciclos seguintes e evitam que uma agenda com falha permanente impeça as demais.

## Instrumentação e exportação

O processo cria o `Meter` `RinhaDasLendas.PresenceSchedule` e os instrumentos abaixo:

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

Esta instrumentação existe somente dentro do processo. A aplicação não configura exporter OpenTelemetry, endpoint Prometheus nem outro endpoint padrão para essas métricas. Os valores só ficam observáveis quando o ambiente conecta explicitamente um `MeterListener` ou uma pipeline OpenTelemetry. Adicionar exporter ou dependência de observabilidade permanece uma evolução futura, fora desta entrega.

## Runbook

### Agenda não executou

1. Consulte a API autenticada `GET /api/v1/discord/agendamentos-presenca?page=1&pageSize=20` com um JWT de Moderador+ obtido pelo fluxo normal; não registre o token no comando ou no documento.
2. Consulte `GET /api/v1/discord/agendamentos-presenca/{id}/ocorrencias?page=1&pageSize=20` e identifique `Bloqueada`, `Perdida` ou `Falha` pelo `messageCode`.
3. Confirme que a agenda está ativa, inclui o dia local e possui janela ainda aberta.
4. Verifique se o bot está ativado e se a configuração do Discord está completa pela interface autorizada, sem registrar ou compartilhar segredos.
5. Consulte a saída estruturada do processo da API e procure `Presence schedule processing failure` com os campos `Stage`, `ErrorType` e `Code`.
6. Aguarde o próximo ciclo após corrigir configuração. Não crie outro draft se já existir ocorrência ou draft para a data.

A API retorna o `messageCode` bruto, por exemplo `MV096` ou `MV098`. A interface traduz esse código com `settings.presenceSchedules.messageCodes`; clientes de API devem tratar o código estável, não depender do texto traduzido.

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

1. Execute as consultas de marcador e ocorrências abaixo em dois momentos separados por ao menos um ciclo.
2. Confirme que `ultima_data_avaliada` e o conjunto de ocorrências avançam e procure warnings estruturados repetidos nos logs.
3. Ajuste limites gradualmente dentro das faixas efetivas documentadas; reinicie a aplicação para aplicar configuração.
4. Reverta o ajuste se banco, API ou duração percebida do ciclo degradarem.

### Recuperação após reinício

1. Inicie normalmente a API; não limpe claims manualmente.
2. Claims expirados tornam-se retomáveis após cinco minutos e somente dentro da janela.
3. Confirme que datas antigas são classificadas em lotes até o marcador alcançar a data atual.
4. Valide que cada agenda e data possui no máximo uma ocorrência e um draft.

## Consultas reproduzíveis no banco de desenvolvimento

Os comandos abaixo usam exclusivamente o PostgreSQL local do devcontainer definido em `.devcontainer/docker-compose.yml` e não contêm token ou senha. A migration inicial altera o schema; as consultas posteriores são somente leitura.

Antes da primeira consulta, aplique as migrations no banco de desenvolvimento:

```bash
docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet ef database update --project /workspaces/RinhaDasLendas/BackEnd/src/RinhaDasLendas.Infrastructure/RinhaDasLendas.Infrastructure.csproj --startup-project /workspaces/RinhaDasLendas/BackEnd/src/RinhaDasLendas.Api/RinhaDasLendas.Api.csproj --configuration Release -- --environment Development
```

### Status e marcador das agendas

```bash
docker.exe exec rinhadaslendas_devcontainer-postgres-1 psql -U postgres -d rinha_das_lendas -c "SELECT id, status, ultima_data_avaliada, horario_publicacao_local, horario_encerramento_local FROM agendamentos_presenca ORDER BY ultima_data_avaliada, id;"
```

Status de agenda: `0=Ativo`, `1=Pausado`, `2=Arquivado`.

### Ocorrências, claims e publicação

```bash
docker.exe exec rinhadaslendas_devcontainer-postgres-1 psql -U postgres -d rinha_das_lendas -c "SELECT agendamento_presenca_id, data_local, status, draft_montagem_id, codigo_falha, claim_id, claim_expires_at, encerramento_previsto_em FROM ocorrencias_agendamentos_presenca ORDER BY data_local DESC, agendamento_presenca_id;"
```

Status de ocorrência: `0=Processando`, `1=Bloqueada`, `2=Criada`, `3=Perdida`, `4=Falha`. Claim `Processando` com `claim_expires_at <= clock_timestamp()` pode ser retomado somente antes do encerramento.

```bash
docker.exe exec rinhadaslendas_devcontainer-postgres-1 psql -U postgres -d rinha_das_lendas -c "SELECT o.agendamento_presenca_id, o.data_local, p.tipo, p.status FROM ocorrencias_agendamentos_presenca o JOIN draft_montagem_publicacoes_discord p ON p.draft_montagem_id = o.draft_montagem_id ORDER BY o.data_local DESC;"
```

### Barreiras de unicidade

As duas consultas devem retornar zero linhas:

```bash
docker.exe exec rinhadaslendas_devcontainer-postgres-1 psql -U postgres -d rinha_das_lendas -c "SELECT agendamento_presenca_id, data_local, count(*) FROM ocorrencias_agendamentos_presenca GROUP BY agendamento_presenca_id, data_local HAVING count(*) > 1;"
docker.exe exec rinhadaslendas_devcontainer-postgres-1 psql -U postgres -d rinha_das_lendas -c "SELECT draft_montagem_id, count(*) FROM ocorrencias_agendamentos_presenca WHERE draft_montagem_id IS NOT NULL GROUP BY draft_montagem_id HAVING count(*) > 1;"
```

## Validação operacional

- Bot: `npm test --prefix discord-bot` e `npm run build --prefix discord-bot`.
- Frontend do histórico: `npm test --prefix FrontEnd -- src/constants/systemUpdates.spec.ts src/services/systemUpdates.spec.ts src/i18n/i18n.spec.ts`.
- Backend focado pelo devcontainer Windows: `docker.exe exec rinhadaslendas_devcontainer-app-1 dotnet test /workspaces/RinhaDasLendas/BackEnd/RinhaDasLendas.sln --configuration Release --filter "FullyQualifiedName~AgendamentoPresencaBehaviorIntegrationTests.CiclosDoHandler_DevemCriarUmaOcorrenciaUmDraftEUmaPublicacaoPendente"`.
- Consulte também as [instruções de execução do projeto](../../AGENTS.md).
- Documentos e whitespace: `git diff --check`.
