# Relatório da Unidade 3: lifecycle dos handlers de draft

## Resultado

- Os 19 handlers não pertencentes ao fluxo de publicação Discord consomem `IDraftMontagemRealtimePublisher`.
- A publicação ocorre somente depois da persistência bem-sucedida e não recebe o `CancellationToken` da request.
- Rejeições, conflitos e operações sem alteração não chamam o publisher.
- `IDraftMontagemRealtimeNotifier`, seu método personalizado, o adapter e os doubles necessários aos handlers de publicação Discord foram preservados para a Unidade 4.
- O layout exige `VersaoEstado`; base divergente retorna `MV103`/HTTP 409 antes de mutação, persistência ou publicação.

## Matriz de cobertura

| Handler | Publicação após commit | Caminho sem publicação verificado |
|---|---|---|
| `ConfirmarPresencaDraftMontagemCommandHandler` | `None` | no-op e conflitos de persistência |
| `CancelarPresencaDraftMontagemCommandHandler` | `None` | no-op e conflitos de persistência |
| `AdicionarPresencaManualDraftMontagemCommandHandler` | `None` | rejeição antes do save |
| `RemoverPresencaManualDraftMontagemCommandHandler` | `None` | rejeição antes do save |
| `EncerrarPresencaDraftMontagemCommandHandler` | `None` | rejeição antes do save |
| `ReabrirPresencaDraftMontagemCommandHandler` | `None` | draft ausente/rejeição |
| `SelecionarModoDraftMontagemCommandHandler` | `None` | mesmo modo é no-op |
| `DefinirCapitaesDraftMontagemCommandHandler` | `None` | rejeição antes do save |
| `SortearCapitaesDraftMontagemCommandHandler` | `None` | rejeição antes do save |
| `DefinirOrdemEscolhaDraftMontagemCommandHandler` | `None` | rejeição antes do save |
| `IniciarDraftMontagemTempoRealCommandHandler` | `None` | rejeição/conflito antes do commit |
| `RegistrarPickDraftMontagemCommandHandler` | `None` | rejeição/conflito antes do commit |
| `AvancarTurnoDraftMontagemTimeoutCommandHandler` | `None` | timeout não aplicável é no-op |
| `SubstituirReservaDraftMontagemCommandHandler` | `None` | rejeição/conflito antes do commit |
| `SalvarLayoutDraftMontagemCommandHandler` | `None` | versão-base stale retorna `MV103` |
| `FinalizarDraftMontagemCommandHandler` | `None` | conflito de versão retorna `MV103` |
| `CancelarDraftMontagemCommandHandler` | `None` | rejeição antes do save |
| `ArquivarDraftMontagemCommandHandler` | `Archived` | já arquivado é no-op; stale/conflito não publica |
| `RestaurarDraftMontagemCommandHandler` | `Restored` | já ativo é no-op; stale/conflito não publica |

## Evidências TDD e verificação

- Baseline: 11 testes focados passaram antes das alterações.
- RED: a suíte falhou pela ausência do publisher nos constructors e de `VersaoEstado` no contrato de layout.
- GREEN focado: 37 testes passaram, incluindo a teoria parametrizada com os 19 handlers e o teste HTTP real PostgreSQL para `MV103`/409.
- Regressão relacionada: 149 testes de handlers, concorrência, métricas e segurança passaram.
- `git diff --check`: sem erros de whitespace.
- Inventário por busca: nenhum dos 19 handlers mantém dependência de `IDraftMontagemRealtimeNotifier`; somente handlers/helpers de publicação Discord continuam usando o seam antigo.
- Inventário de token: nenhuma chamada a `PublishAfterCommitAsync` recebe `CancellationToken`.

## Teste HTTP stale

`SalvarLayoutComVersaoBaseDefasada_DeveRetornarMv103SemMutarSalvarOuPublicar` usa `WebApplicationFactory`, PostgreSQL isolado e publisher substituído. O teste comprova:

- HTTP 409;
- `MessageCode` igual a `MV103`;
- `VersaoEstado`, times e participantes inalterados no banco;
- zero chamadas ao publisher.

## Auditoria de internacionalização

| Item | Resultado | Evidência |
|---|---|---|
| Textos hardcoded no frontend | Sim, auditado; nenhum texto novo | Frontend não foi alterado nesta unidade |
| Mensagens hardcoded no backend | Sim, auditado; nenhuma encontrada nas alterações | conflito usa `MessageCodes.DraftStateConflict` |
| `pt.json` e `en.json` sincronizados | Sim | nenhum locale frontend foi alterado |
| Recursos backend atualizados | Sim | nenhuma chave nova; `MV103` já existe em padrão, `pt-BR` e `en-US` |
| Acentuação em português revisada | Sim | nomes de testes, relatório e mensagens existentes revisados |
| Placeholders, botões, títulos, badges, toasts e estados vazios revisados | Sim | nenhum elemento de UI foi alterado |
| Validações frontend/backend usam i18n/recurso | Sim | validação nova usa `MessageCodes.FieldRequired`; conflito usa `MV103` |
| Novos arquivos respeitam o padrão | Sim | testes não expõem texto ao usuário; relatório está em português |

## Revisão final

- Nenhum arquivo de tasks ou plans foi alterado.
- Nenhuma alteração estrutural de banco ou migration foi necessária.
- O publisher compartilhado permanece registrado por DI e encapsula timeout, telemetria e falhas de transporte.
- Sem concerns bloqueantes identificados para a Unidade 3.
