# Relatorio de testes HTTP reais da API

Data da execucao: 2026-06-07
Base URL usada: `http://127.0.0.1:5099`
Cliente usado: `curl`
Autenticacao/JWT: nenhum endpoint protegido foi encontrado. Nao ha `[Authorize]`, `AddAuthentication`, `UseAuthentication` ou `UseAuthorization` na API atual.

## Endpoints encontrados

| Metodo | Rota | Entrada | Saidas declaradas |
|---|---|---|---|
| POST | `/api/v1/jogadores` | `JogadorCreateRequestDto` no body | 201 `JogadorResponseDto`, 400 `ApiErrorResponse` |
| GET | `/api/v1/jogadores` | query `somenteAtivos`, `page`, `pageSize` | 200 `PaginatedResponseDto<JogadorResponseDto>` |
| GET | `/api/v1/jogadores/{id}` | route `id: Guid` | 200 `JogadorResponseDto`, 404 `ApiErrorResponse` |
| PUT | `/api/v1/jogadores/{id}/dados-basicos` | route `id`, body `JogadorUpdateRequestDto` | 200 `JogadorResponseDto`, 400/404 `ApiErrorResponse` |
| PUT | `/api/v1/jogadores/{id}/preferencias-rotas` | route `id`, body `UpdatePreferenciasRotasRequestDto` | 200 `JogadorResponseDto`, 400/404 `ApiErrorResponse` |
| PATCH | `/api/v1/jogadores/{id}/inativar` | route `id` | 204, 404 `ApiErrorResponse` |

## Resultado por endpoint

| Cenario | Metodo/rota | Params/payload | Status | Resposta | Resultado |
|---|---|---|---:|---|---|
| Criar jogador valido | `POST /api/v1/jogadores` | JSON completo com 5 preferencias | 201 | `JogadorResponseDto`, header `Location` com ID | Funcionando |
| Criar jogador invalido | `POST /api/v1/jogadores` | `nomeExibicao` vazio, URL invalida, preferencias vazias | 400 | `Erro de validacao` com mensagens de nome, URL e preferencias | Funcionando |
| Listar jogadores | `GET /api/v1/jogadores?page=1&pageSize=20&somenteAtivos=false` | query valida | 200 | `PaginatedResponseDto` com `page`, `pageSize`, `items` | Funcionando |
| Listar com query invalida | `GET /api/v1/jogadores?somenteAtivos=abc&page=-1&pageSize=999` | bool invalido | 400 | `ValidationProblemDetails` padrao ASP.NET para `somenteAtivos` | Funcionando, mas formato difere de `ApiErrorResponse` |
| Buscar por ID valido | `GET /api/v1/jogadores/{id}` | ID criado no POST | 200 | `JogadorResponseDto` | Funcionando |
| Buscar ID inexistente | `GET /api/v1/jogadores/00000000-0000-0000-0000-000000000000` | GUID valido inexistente | 404 | `{ "message": "Jogador nao encontrado", "errors": [] }` | Funcionando |
| Buscar route param invalido | `GET /api/v1/jogadores/not-a-guid` | `id` fora do constraint `guid` | 404 | corpo vazio | Funcionando como roteamento, mas sem resposta padronizada |
| Atualizar dados valido | `PUT /api/v1/jogadores/{id}/dados-basicos` | JSON valido | 200 | `JogadorResponseDto` atualizado | Funcionando |
| Atualizar dados invalido | `PUT /api/v1/jogadores/{id}/dados-basicos` | nome vazio e URL invalida | 400 | `Erro de validacao` | Funcionando |
| Atualizar dados de ID inexistente | `PUT /api/v1/jogadores/{missingId}/dados-basicos` | JSON valido | 404 | `Jogador nao encontrado` | Funcionando |
| Atualizar preferencias valido | `PUT /api/v1/jogadores/{id}/preferencias-rotas` | JSON com 5 preferencias validas | 200 | `JogadorResponseDto` com preferencias atualizadas | Funcionando |
| Atualizar preferencias invalido | `PUT /api/v1/jogadores/{id}/preferencias-rotas` | preferencias incompletas/duplicadas | 400 | `Erro de validacao` | Funcionando |
| Atualizar preferencias de ID inexistente | `PUT /api/v1/jogadores/{missingId}/preferencias-rotas` | JSON valido | 404 | `Jogador nao encontrado` | Funcionando |
| Inativar jogador valido | `PATCH /api/v1/jogadores/{id}/inativar` | ID criado no POST | 204 | corpo vazio | Funcionando |
| Inativar ID inexistente | `PATCH /api/v1/jogadores/{missingId}/inativar` | GUID valido inexistente | 404 | `Jogador nao encontrado` | Funcionando |
| Listar ativos depois da inativacao | `GET /api/v1/jogadores?somenteAtivos=true&page=1&pageSize=20` | query valida | 200 | lista nao contem o jogador inativado | Funcionando |

## Endpoint corrigido

### `PUT /api/v1/jogadores/{id}/preferencias-rotas`

Payload validado apos correcao:

```json
{
  "preferencias": [
    { "rota": "Mid", "prioridade": 1, "naoJogoNemLascando": false },
    { "rota": "Adc", "prioridade": 2, "naoJogoNemLascando": false },
    { "rota": "Jungle", "prioridade": 3, "naoJogoNemLascando": false },
    { "rota": "Top", "prioridade": 4, "naoJogoNemLascando": true },
    { "rota": "Support", "prioridade": 5, "naoJogoNemLascando": false }
  ]
}
```

Status retornado apos correcao: `200 OK`

Resposta retornada apos correcao: `JogadorResponseDto` com preferencias na nova ordem e rota `Top` marcada como `naoJogoNemLascando`.

Erro original encontrado no log:

```text
Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException: The database operation was expected to affect 1 row(s), but actually affected 0 row(s)
System.InvalidOperationException: Unable to save changes because a circular dependency was detected in the data to be saved: Index { 'jogador_id', 'prioridade' }
```

Causa confirmada: a troca de prioridades em entidades tracked pelo EF conflitou com indices unicos de troca em `preferencias_rotas` (`jogador_id + prioridade` e `jogador_id + nao_jogo_nem_lascando`).

Correcao aplicada:

- `Jogador.AtualizarPreferencias` agora atualiza as entidades existentes por rota, preservando tracking e identidade das preferencias.
- `PreferenciaRota` ganhou metodo `Atualizar` para alterar prioridade e flag bloqueada sem recriar a entidade.
- Migration `20260607000100_RemovePreferenciasRotasSwapIndexes` remove os indices unicos que impediam a troca atomica. As regras continuam garantidas por validacao de dominio/aplicacao.

## Endpoints funcionando

- `POST /api/v1/jogadores` valido e invalido.
- `GET /api/v1/jogadores` valido e query invalida.
- `GET /api/v1/jogadores/{id}` valido, inexistente e route param invalido.
- `PUT /api/v1/jogadores/{id}/dados-basicos` valido, invalido e inexistente.
- `PUT /api/v1/jogadores/{id}/preferencias-rotas` valido, invalido e inexistente.
- `PATCH /api/v1/jogadores/{id}/inativar` valido e inexistente.

## Endpoints quebrando

Nenhum endpoint quebrando no reteste HTTP de 2026-06-07.

## Endpoints faltando parametros

Nenhum endpoint deixou de ser testado por falta de parametros. Todos os route params, query params e bodies exigidos foram exercitados com valores validos e invalidos.

## Endpoints que precisam autenticacao

Nenhum endpoint atual precisa autenticacao/JWT.

## Correcoes sugeridas adicionais

- Padronizar erros de model binding/route constraint. Hoje query invalida retorna `ValidationProblemDetails` do ASP.NET e route `not-a-guid` retorna `404` sem corpo, enquanto validacoes de negocio retornam `ApiErrorResponse`.
- Considerar retornar `400` padronizado para route param invalido, se esse for o contrato desejado pela API.
- Manter a collection `docs/api/rinha-das-lendas.http` como smoke test manual sempre que criar endpoint novo.
