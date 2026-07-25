# Backend API Contract: Agendamentos de Presença

## Boundary And Authorization

- Base route: `/api/v1/discord/agendamentos-presenca`.
- Todos os endpoints exigem JWT e `AuthPermissions.CanManageDrafts`.
- Anônimo recebe `401`; autenticado sem permissão recebe `403`.
- `ResponsavelUsuarioId` é obtido do claim autenticado no controller e nunca aceito no body.
- Nenhum contrato retorna entidades, claim de ocorrência, guild/canal/token, IDs de mensagem, payload ou detalhe técnico Discord.
- Mensagens de erro usam o envelope padrão da API e `messageCode` localizado por resources PT-BR/en-US.

## DTOs

```csharp
public sealed record SaveAgendamentoPresencaRequestDto(
    string Nome,
    string? Observacao,
    IReadOnlyCollection<DiaSemanaIso> DiasSemana,
    TimeOnly HorarioPublicacao,
    TimeOnly HorarioEncerramento);

public sealed record AgendamentoPresencaSummaryDto(
    Guid Id, string Nome, string? Observacao, AgendamentoPresencaStatus Status,
    IReadOnlyCollection<DiaSemanaIso> DiasSemana, TimeOnly HorarioPublicacao,
    TimeOnly HorarioEncerramento, DateTimeOffset? ProximaExecucaoEm,
    OcorrenciaAgendamentoPresencaSummaryDto? UltimaOcorrencia);

public sealed record OcorrenciaAgendamentoPresencaSummaryDto(
    Guid Id, DateOnly DataLocal, DateTimeOffset PublicacaoPrevistaEm,
    DateTimeOffset EncerramentoPrevistoEm, OcorrenciaAgendamentoPresencaStatus Status,
    Guid? DraftMontagemId, string? MessageCode);

public sealed record PaginatedAgendamentoPresencaResponseDto(
    int Page,
    int PageSize,
    IReadOnlyCollection<AgendamentoPresencaSummaryDto> Items,
    int TotalItems,
    int TotalPages,
    int ActiveItems);

public sealed record PaginatedResponseDto<T>(
    int Page,
    int PageSize,
    IReadOnlyCollection<T> Items,
    int TotalItems = 0,
    int TotalPages = 0);
```

JSON usa a política camelCase existente. `TimeOnly` é enviado como `HH:mm`; instantes são ISO 8601 UTC; `DateOnly` é `yyyy-MM-dd`; enums usam os nomes definidos no domínio.

Para ambas as listagens, `page` inicia em `1`, `pageSize` usa default `20` e limite `100`, e `TotalPages` é calculado a partir de `TotalItems` e do `PageSize` efetivo.

## Endpoints

### List Schedules

```http
GET /api/v1/discord/agendamentos-presenca?page=1&pageSize=20
```

- **200**: `PaginatedAgendamentoPresencaResponseDto` de não arquivados, incluindo pausados, ordenada obrigatoriamente por `ProximaExecucaoEm ASC NULLS LAST, Nome ASC, Id ASC` e com última ocorrência; `TotalItems` vem de count com os mesmos filtros e `ActiveItems` é o total global de agendas ativas, independente da página carregada.
- **400**: `page` ou `pageSize` inválido.
- **401/403**: conforme autorização.

O teste de contrato usa ao menos duas páginas com empate em `ProximaExecucaoEm` e `Nome`, além de agendas pausadas com próxima execução nula, e comprova que nenhum ID é duplicado ou omitido.

### Create Schedule

```http
POST /api/v1/discord/agendamentos-presenca
Content-Type: application/json
```

Body: `SaveAgendamentoPresencaRequestDto`.

- **201**: `AgendamentoPresencaSummaryDto`, com `Location` para o detalhe.
- **400**: `MV089`-`MV094` conforme validação.
- **401/403**: conforme autorização.
- **409**: conflito conhecido com `MV097`.

### Get Schedule

```http
GET /api/v1/discord/agendamentos-presenca/{id}
```

- **200**: `AgendamentoPresencaSummaryDto` administrativo seguro.
- **404**: ausente ou arquivado, `MV099`.
- **401/403**: conforme autorização.

### Update Schedule

```http
PUT /api/v1/discord/agendamentos-presenca/{id}
Content-Type: application/json
```

Body: `SaveAgendamentoPresencaRequestDto`.

- **200**: `AgendamentoPresencaSummaryDto` atualizado; ocorrências/drafts existentes permanecem inalterados.
- **400**: validação ou janela inválida.
- **404**: ausente ou arquivado, `MV099`.
- **409**: concorrência conhecida, `MV097`.
- **401/403**: conforme autorização.

### Pause Schedule

```http
POST /api/v1/discord/agendamentos-presenca/{id}/pausar
```

- **200**: `AgendamentoPresencaSummaryDto`; operação idempotente.
- **404**: ausente ou arquivado.
- **409**: concorrência conhecida.
- **401/403**: conforme autorização.

### Reactivate Schedule

```http
POST /api/v1/discord/agendamentos-presenca/{id}/reativar
```

- **200**: `AgendamentoPresencaSummaryDto`; operação idempotente.
- Reativação exatamente no horário previsto mantém a ocorrência daquele dia elegível; somente `AtivadoEm > PublicacaoPrevistaEm` a bloqueia.
- **404**: ausente ou arquivado.
- **409**: concorrência conhecida.
- **401/403**: conforme autorização.

### Archive Schedule

```http
DELETE /api/v1/discord/agendamentos-presenca/{id}
```

- **204**: arquivamento lógico concluído, sem body.
- **404**: ausente ou já arquivado conforme semântica de recurso não disponível.
- **409**: concorrência conhecida.
- **401/403**: conforme autorização.

### List Occurrences

```http
GET /api/v1/discord/agendamentos-presenca/{id}/ocorrencias?page=1&pageSize=20
```

- **200**: `PaginatedResponseDto<OcorrenciaAgendamentoPresencaSummaryDto>`, em `DataLocal` decrescente; `TotalItems` vem de count da mesma agenda.
- **400**: paginação inválida.
- **404**: agenda ausente ou arquivada.
- **401/403**: conforme autorização.

## Validation Matrix

| Input/rule | Result |
|------------|--------|
| Nome vazio | `400`, `MV089` |
| Nome fora de 3-100 | `400`, `MV090` |
| Observação acima de 500 | `400`, `MV091` |
| Nenhum dia | `400`, `MV092` |
| Dia duplicado | `400`, `MV093` |
| Encerramento igual/anterior ou fora do mesmo dia | `400`, `MV094` |
| Agenda arquivada | `404`, `MV099`; domínio preserva `MV095` internamente |
| Conflito conhecido | `409`, `MV097` |

## Security Projection Test

Cada resposta deve ser testada para ausência de `claimId`, `claimExpiresAt`, `discordGuildId`, `channelId`, `messageId`, `token`, payload Discord, detalhes técnicos e autoria controlável pelo cliente.
