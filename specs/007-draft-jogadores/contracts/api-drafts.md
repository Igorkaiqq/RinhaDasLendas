# API Contract: Draft de Jogadores

Base path: `/api/v1/drafts`

## Listar Drafts

`GET /api/v1/drafts?page=1&pageSize=20&status=Aberto`

Response `200 OK`:

```json
{
  "page": 1,
  "pageSize": 20,
  "items": [
    {
      "id": "uuid",
      "nome": "Rinha de Sexta",
      "status": "Aberto",
      "tamanhoTime": 5,
      "capitaoTimeA": { "id": "uuid", "nomeExibicao": "Capitao A" },
      "capitaoTimeB": { "id": "uuid", "nomeExibicao": "Capitao B" },
      "proximoTime": "TimeA",
      "totalEscolhas": 2,
      "dataCadastro": "2026-06-19T00:00:00Z"
    }
  ],
  "totalItems": 1,
  "totalPages": 1
}
```

## Obter Draft

`GET /api/v1/drafts/{id}`

Response `200 OK`: `DraftResponseDto` completo com participantes, times, disponiveis e escolhas.

Response `404 Not Found`: draft inexistente.

## Criar Draft

`POST /api/v1/drafts`

Request:

```json
{
  "nome": "Rinha de Sexta",
  "observacoes": "MD1 antes da call",
  "tamanhoTime": 5,
  "sortearCapitaes": false,
  "capitaoTimeAId": "uuid",
  "capitaoTimeBId": "uuid",
  "sortearPrimeiroPick": false,
  "primeiroTime": "TimeA",
  "jogadoresIds": ["uuid", "uuid", "uuid", "uuid"]
}
```

Response `201 Created`: `DraftResponseDto`.

Validation responses:
- `400 Bad Request` para campos invalidos.
- `409 Conflict` para jogadores duplicados, inativos ou capitaes invalidos.

## Registrar Pick

`POST /api/v1/drafts/{id}/picks`

Request:

```json
{
  "jogadorId": "uuid"
}
```

Response `200 OK`: `DraftResponseDto` atualizado.

Validation responses:
- `404 Not Found` para draft inexistente.
- `409 Conflict` para sessao fechada, jogador indisponivel ou escolha duplicada.

## Cancelar Draft

`PATCH /api/v1/drafts/{id}/cancelar`

Request:

```json
{
  "motivo": "Jogadores sairam da call"
}
```

Response `200 OK`: `DraftResponseDto` atualizado.

Validation responses:
- `404 Not Found` para draft inexistente.
- `409 Conflict` para sessao ja concluida ou cancelada.

## DraftResponseDto

```json
{
  "id": "uuid",
  "nome": "Rinha de Sexta",
  "observacoes": "MD1 antes da call",
  "status": "Aberto",
  "tamanhoTime": 5,
  "criterioCapitaes": "Manual",
  "criterioPrimeiroPick": "Manual",
  "proximoTime": "TimeA",
  "capitaoTimeA": { "id": "uuid", "nomeExibicao": "Capitao A" },
  "capitaoTimeB": { "id": "uuid", "nomeExibicao": "Capitao B" },
  "timeA": [{ "id": "uuid", "nomeExibicao": "Capitao A", "capitao": true }],
  "timeB": [{ "id": "uuid", "nomeExibicao": "Capitao B", "capitao": true }],
  "disponiveis": [{ "id": "uuid", "nomeExibicao": "Jogador" }],
  "escolhas": [
    {
      "sequencia": 1,
      "time": "TimeA",
      "capitaoId": "uuid",
      "jogadorId": "uuid",
      "jogadorNome": "Jogador",
      "dataEscolha": "2026-06-19T00:00:00Z"
    }
  ],
  "dataCadastro": "2026-06-19T00:00:00Z",
  "dataAtualizacao": "2026-06-19T00:00:00Z"
}
```
