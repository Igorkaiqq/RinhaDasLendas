# API Standards

## Objetivo

Padronizar a construção das APIs REST do projeto.

---

# Convenções de Rotas

Utilizar substantivos.

Correto:

```http
GET /api/jogadores
GET /api/partidas
GET /api/series
```

Evitar:

```http
GET /api/getJogadores
POST /api/createJogador
```

---

# Verbos HTTP

## GET

Consultar dados.

## POST

Criar recursos.

## PUT

Atualizar recurso completo.

## PATCH

Atualização parcial.

## DELETE

Remoção lógica ou física.

---

# Status Codes

## 200

Sucesso.

## 201

Recurso criado.

## 400

Erro de validação.

## 401

Não autenticado.

## 403

Sem permissão.

## 404

Não encontrado.

## 409

Conflito.

## 500

Erro interno.

---

# DTOs

Nunca retornar entidades diretamente.

Sempre utilizar DTOs.

Exemplo:

```csharp
JogadorResponseDto
```

---

# Paginação

Toda listagem futura deve suportar paginação.

Formato:

```http
GET /api/jogadores?page=1&pageSize=20
```

---

# Versionamento

Utilizar:

```http
/api/v1/
```

Exemplo:

```http
/api/v1/jogadores
```

---

# Tratamento de Erros

Formato padrão:

```json
{
  "message": "Jogador não encontrado",
  "errors": []
}
```

Validação:

```json
{
  "message": "Erro de validação",
  "errors": [
    "Nome é obrigatório"
  ]
}
```

---

# Logging

Registrar:

* Erros
* Integrações externas
* Eventos importantes

Nunca registrar:

* Senhas
* Tokens
* Secrets

---

# OpenAPI

Toda API deve possuir documentação Swagger.

Obrigatório em ambiente de desenvolvimento.
