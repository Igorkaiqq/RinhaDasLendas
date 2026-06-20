# API Contract: Draft Montagens

Base path: `/api/v1/draft-montagens`

## Listar Montagens

`GET /api/v1/draft-montagens?search=&status=&page=1&pageSize=20`

**Response 200**

```json
{
  "page": 1,
  "pageSize": 20,
  "items": [
    {
      "id": "uuid",
      "nome": "Rinha de sexta",
      "status": "Aberta",
      "tamanhoEquipe": 5,
      "quantidadeTimes": 3,
      "quantidadeReservas": 3,
      "dataCadastro": "2026-06-20T00:00:00Z",
      "dataAtualizacao": "2026-06-20T00:00:00Z"
    }
  ],
  "totalItems": 1,
  "totalPages": 1
}
```

## Consultar Montagem

`GET /api/v1/draft-montagens/{id}`

**Response 200**

```json
{
  "id": "uuid",
  "nome": "Rinha de sexta",
  "observacoes": null,
  "status": "Aberta",
  "tamanhoEquipe": 5,
  "quantidadeTimes": 3,
  "quantidadeReservas": 3,
  "criterioCapitaes": "Manual",
  "times": [
    {
      "id": "uuid",
      "nome": "Time Azul",
      "ordem": 1,
      "cor": "blue",
      "capitaoId": "uuid",
      "jogadores": []
    }
  ],
  "livres": [],
  "reservas": [],
  "dataCadastro": "2026-06-20T00:00:00Z",
  "dataAtualizacao": "2026-06-20T00:00:00Z"
}
```

## Criar Montagem

`POST /api/v1/draft-montagens`

**Request**

```json
{
  "nome": "Rinha de sexta",
  "observacoes": null,
  "tamanhoEquipe": 5,
  "sortearCapitaes": false,
  "capitaesIds": ["uuid", "uuid", "uuid"],
  "jogadoresIds": ["uuid"]
}
```

**Response 201**: montagem completa.

**Validation 400**

- Jogadores insuficientes.
- Jogador duplicado.
- Jogador inativo.
- Quantidade de capitaes invalida.
- Capitao fora da lista de participantes.

## Salvar Layout

`PUT /api/v1/draft-montagens/{id}/layout`

**Request**

```json
{
  "times": [
    {
      "timeId": "uuid",
      "nome": "Time Azul",
      "capitaoId": "uuid",
      "jogadores": [
        {
          "jogadorId": "uuid",
          "ordem": 1,
          "rotaContextual": "Mid"
        }
      ]
    }
  ],
  "livres": [
    {
      "jogadorId": "uuid",
      "ordem": 1,
      "rotaContextual": null
    }
  ],
  "reservas": [
    {
      "jogadorId": "uuid",
      "ordem": 1,
      "rotaContextual": "Support"
    }
  ]
}
```

**Response 200**: montagem completa atualizada.

## Registrar Movimento Individual

`POST /api/v1/draft-montagens/{id}/movimentos`

**Request**

```json
{
  "jogadorId": "uuid",
  "origem": {
    "estado": "Livre",
    "timeId": null
  },
  "destino": {
    "estado": "Time",
    "timeId": "uuid",
    "ordem": 3
  }
}
```

**Response 200**: montagem completa atualizada.

**Note**: Para o MVP, o salvamento de layout completo pode ser implementado primeiro. O movimento individual fica documentado para auto-save ou auditoria incremental.

## Sortear Capitaes

`POST /api/v1/draft-montagens/{id}/capitaes/sortear`

**Response 200**: montagem com capitaes atualizados.

## Finalizar Montagem

`PATCH /api/v1/draft-montagens/{id}/finalizar`

**Response 200**: montagem finalizada.

## Cancelar Montagem

`PATCH /api/v1/draft-montagens/{id}/cancelar`

**Request**

```json
{ "motivo": "Jogadores desistiram" }
```

**Response 200**: montagem cancelada.
