# Data Model: Agendamento Recorrente de Listas de Presença

## Conventions

- Banco PostgreSQL, nomes de tabelas e colunas em snake_case.
- Chaves primárias UUID e FKs explícitas com exclusão restrita.
- Horários recorrentes usam `time without time zone`; datas locais usam `date`; instantes usam `timestamp with time zone` em UTC.
- Mapeamento por Fluent API e alteração exclusivamente por migration EF Core.
- `America/Sao_Paulo` é fixo nesta feature e não é persistido por agenda.
- Todos os enums são persistidos em colunas PostgreSQL `smallint` com checks compatíveis com seus valores definidos.

## Enums

```csharp
public enum DiaSemanaIso { Segunda = 1, Terca = 2, Quarta = 3, Quinta = 4, Sexta = 5, Sabado = 6, Domingo = 7 }
public enum AgendamentoPresencaStatus { Ativo, Pausado, Arquivado }
public enum OcorrenciaAgendamentoPresencaStatus { Processando, Bloqueada, Criada, Perdida, Falha }
public enum AgendamentoPresencaAcao { Criado, Editado, Pausado, Reativado, Arquivado }
```

## AgendamentoPresenca

**Table**: `agendamentos_presenca`

| Domain field | Column | Type | Null | Rule |
|--------------|--------|------|------|------|
| `Id` | `id` | uuid | No | PK |
| `Nome` | `nome` | varchar(100) | No | Normalizado; 3-100 caracteres |
| `Observacao` | `observacao` | varchar(500) | Yes | Até 500 caracteres |
| `HorarioPublicacaoLocal` | `horario_publicacao_local` | time without time zone | No | Precisão de minuto |
| `HorarioEncerramentoLocal` | `horario_encerramento_local` | time without time zone | No | Mesmo dia e posterior à publicação |
| `Status` | `status` | smallint | No | `Ativo`, `Pausado`, `Arquivado` |
| `AtivadoEm` | `ativado_em` | timestamp with time zone | No | Ativação mais recente |
| `PausadoEm` | `pausado_em` | timestamp with time zone | Yes | Preenchido quando pausada |
| `ArquivadoEm` | `arquivado_em` | timestamp with time zone | Yes | Preenchido no arquivamento lógico |
| `UltimaDataAvaliada` | `ultima_data_avaliada` | date | No | Última data local integralmente classificada |
| `CriadoPorUsuarioId` | `criado_por_usuario_id` | uuid | No | FK de autoria |
| `CriadoEm` | `criado_em` | timestamp with time zone | No | UTC |
| `AtualizadoEm` | `atualizado_em` | timestamp with time zone | No | UTC |

### Invariants

- Deve possuir ao menos um `AgendamentoPresencaDiaSemana`, sem duplicidade.
- Horários não carregam segundos e `HorarioEncerramentoLocal > HorarioPublicacaoLocal`.
- Arquivada não pode ser editada, pausada novamente ou reativada.
- `Pausar` agenda pausada e `Reativar` agenda ativa são idempotentes.
- `Editar`, `Pausar`, `Reativar` e `Arquivar` recebem responsável e instante, criam histórico e não alteram ocorrências existentes.
- `MarcarDataAvaliada` não retrocede e só é chamado após classificação completa da data.
- `OcorreEm(DateOnly)` usa `DiaSemanaIso`, sem depender da cultura do processo.
- Na criação, `UltimaDataAvaliada` recebe a data local anterior quando a hora atual em São Paulo é menor ou igual a `HorarioPublicacaoLocal`; recebe a data local atual somente quando a hora atual é maior.
- Na reativação, o novo marcador é `max(UltimaDataAvaliada, data calculada pela regra de criação)`, impedindo retrocesso e recuperação indevida do mesmo dia.
- A ocorrência do mesmo dia é bloqueada somente quando `AtivadoEm > PublicacaoPrevistaEm`; igualdade é elegível.

### Indexes

- Índice parcial para agendas `Ativo` por `ultima_data_avaliada` e horários de execução.
- Índice por `criado_por_usuario_id` para integridade/auditoria quando necessário.

### Paginated Projection Order

- A projeção paginada de agendas aplica, no banco e antes de `Skip`/`Take`, `ProximaExecucaoEm ASC NULLS LAST, Nome ASC, Id ASC`.
- `Id` é o desempate final obrigatório; agendas pausadas com `ProximaExecucaoEm` nula ficam após agendas com próxima execução e permanecem ordenadas por `Nome` e `Id`.
- Count usa os mesmos filtros, sem ordenação; a consulta de itens preserva a ordem total em todas as páginas.

## AgendamentoPresencaDiaSemana

**Table**: `agendamentos_presenca_dias_semana`

| Domain field | Column | Type | Null | Rule |
|--------------|--------|------|------|------|
| `AgendamentoPresencaId` | `agendamento_presenca_id` | uuid | No | PK composta/FK restrita |
| `DiaSemana` | `dia_semana` | smallint | No | PK composta; ISO 1-7 |

**Constraint**: `UNIQUE (agendamento_presenca_id, dia_semana)` e check de 1 a 7.

## OcorrenciaAgendamentoPresenca

**Table**: `ocorrencias_agendamentos_presenca`

| Domain field | Column | Type | Null | Rule |
|--------------|--------|------|------|------|
| `Id` | `id` | uuid | No | PK |
| `AgendamentoPresencaId` | `agendamento_presenca_id` | uuid | No | FK restrita |
| `DataLocal` | `data_local` | date | No | Data em `America/Sao_Paulo` |
| `PublicacaoPrevistaEm` | `publicacao_prevista_em` | timestamp with time zone | No | UTC |
| `EncerramentoPrevistoEm` | `encerramento_previsto_em` | timestamp with time zone | No | UTC e posterior à publicação |
| `Status` | `status` | smallint | No | Enum de ocorrência |
| `DraftMontagemId` | `draft_montagem_id` | uuid | Yes | FK restrita; obrigatório em `Criada` |
| `CodigoFalha` | `codigo_falha` | varchar(16) | Yes | Código público estável |
| `ClaimId` | `claim_id` | uuid | Yes | Dono atual do processamento |
| `ClaimExpiresAt` | `claim_expires_at` | timestamp with time zone | Yes | Expira cinco minutos após aquisição |
| `UltimaTentativaEm` | `ultima_tentativa_em` | timestamp with time zone | Yes | UTC |
| `CriadaEm` | `criada_em` | timestamp with time zone | No | UTC |
| `AtualizadaEm` | `atualizada_em` | timestamp with time zone | No | UTC |

### Constraints And Indexes

- `UNIQUE (agendamento_presenca_id, data_local)` é a barreira final contra duas ocorrências para a mesma agenda/data.
- Check `encerramento_previsto_em > publicacao_prevista_em`.
- FK `draft_montagem_id` restrita e índice `UNIQUE (draft_montagem_id) WHERE draft_montagem_id IS NOT NULL` para impedir associação repetida.
- Índice por `(status, claim_expires_at, encerramento_previsto_em)` para bloqueadas e claims retomáveis.
- Índice por `(agendamento_presenca_id, data_local DESC)` para histórico paginado.

### State Transitions

| From | Operation | To | Conditions |
|------|-----------|----|------------|
| Inexistente | `TryClaimOccurrenceAsync` | `Processando` | Agenda ativa, configurada, dentro da janela e claim adquirido |
| Inexistente | `TryUpsertBlockedOccurrenceAsync` | `Bloqueada` | Bot desativado/configuração incompleta antes do encerramento |
| `Bloqueada` | `TryUpsertBlockedOccurrenceAsync` | `Bloqueada` | Indisponibilidade permanece; atualiza tentativa/código sem draft |
| `Bloqueada` | `TryClaimOccurrenceAsync` | `Processando` | Configuração voltou, janela aberta e claim adquirido |
| Inexistente/`Bloqueada` | `TryUpsertMissedOccurrenceAsync` | `Perdida` | Encerramento alcançado |
| `Processando` | `TryCompleteWithDraftAsync` | `Criada` | Mesmo claim; draft e publicação inseridos na mesma transação |
| `Processando` | `TryMarkFailedAsync` | `Falha` | Mesmo claim e erro terminal conhecido |
| `Processando` com claim expirado | `TryClaimOccurrenceAsync` | `Processando` | Janela aberta; novo claim substitui o anterior |

Estados `Criada`, `Perdida` e `Falha` são terminais para o scheduler. Claim divergente nunca conclui nem falha a ocorrência. Falha transitória não confirma transição e permite nova tentativa.

## HistoricoAgendamentoPresenca

**Table**: `historicos_agendamentos_presenca`

| Domain field | Column | Type | Null | Rule |
|--------------|--------|------|------|------|
| `Id` | `id` | uuid | No | PK |
| `AgendamentoPresencaId` | `agendamento_presenca_id` | uuid | No | FK restrita |
| `Acao` | `acao` | smallint | No | `AgendamentoPresencaAcao` |
| `ResponsavelUsuarioId` | `responsavel_usuario_id` | uuid | No | Identidade autenticada |
| `RegistradoEm` | `registrado_em` | timestamp with time zone | No | UTC |
| `CamposAlterados` | `campos_alterados` | varchar(200) | No | Nomes estáveis de campos separados por vírgula, sem valores |

`campos_alterados` aceita somente nomes do conjunto estável `Nome`, `Observacao`, `DiasSemana`, `HorarioPublicacaoLocal`, `HorarioEncerramentoLocal` e `Status`, ordenados e separados por vírgula. Não armazena valores anteriores/novos, token, guild/canal, payload, IDs de mensagem ou texto livre.

## Existing Draft Relationship

- `OcorrenciaAgendamentoPresenca.DraftMontagemId` referencia o `DraftMontagem` existente.
- O draft é criado com nome `Nome configurado - dd/MM/yyyy`, observação da agenda, tamanho de equipe `5`, critério de capitães `Manual`, encerramento configurado e publicação Discord `Presenca` pendente.
- Agenda e ocorrência não armazenam IDs de mensagem Discord. Publicação, claim de mensagem, falha e reconciliação continuam no modelo operacional existente.

## Atomicity And Recovery

1. Advisory lock derivado de `AgendamentoPresencaId + DataLocal` serializa concorrentes cooperativos.
2. `INSERT ... ON CONFLICT` e a constraint única garantem uma linha mesmo sob concorrência não cooperativa.
3. Claim persistido identifica o vencedor e expira em cinco minutos.
4. `TryCompleteWithDraftAsync` valida `OcorrenciaId + ClaimId`, insere draft/publicação e atualiza a ocorrência para `Criada` em uma transação.
5. Rollback deixa a ocorrência retomável e não expõe draft/publicação parcial.
6. Para indisponibilidade de múltiplos dias, cada data após `UltimaDataAvaliada` é classificada antes do avanço; uma falha de persistência mantém o marcador anterior para repetição segura.
7. Em todo ciclo, `ListBlockedAsync(agora)` seleciona ocorrências `Bloqueada` independentemente do marcador da agenda; cada item é mantido bloqueado, readquirido para criação ou marcado `Perdida` conforme configuração e encerramento.

## Message Codes

| Constant | Code | Purpose |
|----------|------|---------|
| `PresenceScheduleNameRequired` | `MV089` | Nome obrigatório |
| `PresenceScheduleNameLengthInvalid` | `MV090` | Limite de nome |
| `PresenceScheduleObservationTooLong` | `MV091` | Limite de observação |
| `PresenceScheduleDayRequired` | `MV092` | Ao menos um dia |
| `PresenceScheduleDayDuplicated` | `MV093` | Dia duplicado |
| `PresenceScheduleTimeRangeInvalid` | `MV094` | Janela inválida |
| `PresenceScheduleArchived` | `MV095` | Agenda arquivada imutável |
| `PresenceScheduleTimeZoneInvalid` | `MV096` | Horário local inválido/ambíguo |
| `PresenceScheduleOccurrenceConflict` | `MV097` | Conflito conhecido |
| `PresenceScheduleDiscordUnavailable` | `MV098` | Discord/configuração indisponível |
| `PresenceScheduleNotFound` | `MV099` | Agenda ausente/arquivada |
| `PresenceScheduleWindowExpired` | `MV100` | Janela encerrada |

Cada código deve existir em constantes, catálogo e resources PT-BR/en-US antes do uso por validator, handler ou API.
