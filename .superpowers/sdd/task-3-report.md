# Relatório da Unidade 3: lifecycle dos handlers de draft

## Resultado

- Os 19 handlers não pertencentes ao fluxo de publicação Discord consomem `IDraftMontagemRealtimePublisher`.
- A publicação ocorre somente depois da persistência bem-sucedida e não recebe o `CancellationToken` da request.
- A matriz comportamental executa os 19 handlers e comprova, por linha, sucesso e caminho sem evento.
- `IDraftMontagemRealtimeNotifier`, seu método personalizado, o adapter e os doubles necessários aos handlers de publicação Discord foram preservados para a Unidade 4.
- O layout exige `VersaoEstado >= 0`: valor negativo usa `MV104`; base válida divergente retorna `MV103`/HTTP 409 antes de mutação, persistência ou publicação.

## Matriz de cobertura

| Handler | Sucesso executado | Retorno preservado | Caminho zero publisher executado |
|---|---|---|---|
| `ConfirmarPresencaDraftMontagemCommandHandler` | `save -> publish(None)` | `DraftMontagemResponseDto` | repetição no-op; conflitos cobertos em `PresencaConcurrencyHandlerTests` |
| `CancelarPresencaDraftMontagemCommandHandler` | `save -> publish(None)` | `DraftMontagemResponseDto` | repetição no-op; conflitos cobertos em `PresencaConcurrencyHandlerTests` |
| `AdicionarPresencaManualDraftMontagemCommandHandler` | `save -> publish(None)` | `DraftMontagemResponseDto` | repetição rejeitada |
| `RemoverPresencaManualDraftMontagemCommandHandler` | `save -> publish(None)` | `DraftMontagemResponseDto` | repetição rejeitada |
| `EncerrarPresencaDraftMontagemCommandHandler` | `save -> publish(None)` | `DraftMontagemResponseDto` | repetição rejeitada |
| `ReabrirPresencaDraftMontagemCommandHandler` | `save -> publish(None)` | `DraftMontagemResponseDto` | repetição rejeitada |
| `SelecionarModoDraftMontagemCommandHandler` | `save -> publish(None)` | `DraftMontagemResponseDto` | mesmo modo é no-op |
| `DefinirCapitaesDraftMontagemCommandHandler` | `save -> publish(None)` | `DraftMontagemResponseDto` | repetição rejeitada |
| `SortearCapitaesDraftMontagemCommandHandler` | `save -> publish(None)` | `DraftMontagemResponseDto` | estado terminal rejeitado |
| `DefinirOrdemEscolhaDraftMontagemCommandHandler` | `save -> publish(None)` | `DraftMontagemResponseDto` | repetição rejeitada |
| `IniciarDraftMontagemTempoRealCommandHandler` | `save -> publish(None)` | `DraftMontagemRealtimeStateDto` | repetição rejeitada |
| `RegistrarPickDraftMontagemCommandHandler` | `save -> publish(None)` | `DraftMontagemRealtimeStateDto` | repetição pelo capitão anterior rejeitada |
| `AvancarTurnoDraftMontagemTimeoutCommandHandler` | `save -> publish(None)` | `DraftMontagemRealtimeStateDto` | turno ainda vigente é no-op |
| `SubstituirReservaDraftMontagemCommandHandler` | `save -> publish(None)` | `DraftMontagemRealtimeStateDto` | repetição rejeitada |
| `SalvarLayoutDraftMontagemCommandHandler` | `save -> publish(None)` | `DraftMontagemResponseDto` | repetição com versão-base stale retorna `MV103` |
| `FinalizarDraftMontagemCommandHandler` | `save -> publish(None)` | `DraftMontagemResponseDto` | repetição rejeitada; conflito de save coberto separadamente |
| `CancelarDraftMontagemCommandHandler` | `save -> publish(None)` | `DraftMontagemResponseDto` | repetição rejeitada |
| `ArquivarDraftMontagemCommandHandler` | `save -> publish(Archived)` | `DraftMontagemArquivamentoResultadoDto` | já arquivado é no-op; stale coberto separadamente |
| `RestaurarDraftMontagemCommandHandler` | `save -> publish(Restored)` | `DraftMontagemArquivamentoResultadoDto` | já ativo é no-op; stale coberto separadamente |

## Evidências TDD e verificação

- Baseline: 11 testes focados passaram antes das alterações.
- RED: a suíte falhou pela ausência do publisher nos constructors e de `VersaoEstado` no contrato de layout.
- RED da revisão: versão `-1` não produzia erro no validator e chegava ao handler como `MV103/409`.
- GREEN focado: 58 testes passaram. A matriz possui 38 casos comportamentais, 19 sucessos e 19 caminhos sem evento.
- Regressão relacionada: 209 testes de handlers, validators, endpoints, concorrência, métricas e segurança passaram.
- `git diff --check`: sem erros de whitespace.
- A reflexão de constructors foi removida como evidência principal; cada linha instancia e executa o handler real.
- Inventário de token: nenhuma chamada a `PublishAfterCommitAsync` recebe `CancellationToken`.

## Teste HTTP stale

`SalvarLayoutComVersaoBaseDefasada_DeveRetornarMv103SemMutarSalvarOuPublicar` usa `WebApplicationFactory`, PostgreSQL isolado e publisher substituído. O teste comprova:

- HTTP 409;
- `MessageCode` igual a `MV103`;
- `VersaoEstado`, times e participantes inalterados no banco;
- zero chamadas ao publisher.

## Versão-base inválida

- `SalvarLayoutDraftMontagemValidator` rejeita valor negativo com `MessageCodes.DraftStateVersionInvalid` (`MV104`).
- O teste HTTP real confirma 400 no envelope padrão de validação (`ME031`), com a mensagem localizada de `MV104` em `Errors`, banco inalterado e zero publisher.
- O cenário stale usa versão não negativa e continua comprovando `MV103`/409.

## Auditoria de internacionalização

| Item | Resultado | Evidência |
|---|---|---|
| Textos hardcoded no frontend | Sim, auditado; nenhum texto novo | Frontend não foi alterado nesta unidade |
| Mensagens hardcoded no backend | Sim, auditado; nenhuma encontrada nas alterações | conflito usa `MV103`; versão inválida usa `MV104` |
| `pt.json` e `en.json` sincronizados | Sim | nenhum locale frontend foi alterado |
| Recursos backend atualizados | Sim | nenhuma chave nova; `MV103` e `MV104` já existem em padrão, `pt-BR` e `en-US` |
| Acentuação em português revisada | Sim | nomes de testes, relatório e mensagens existentes revisados |
| Placeholders, botões, títulos, badges, toasts e estados vazios revisados | Sim | nenhum elemento de UI foi alterado |
| Validações frontend/backend usam i18n/recurso | Sim | required usa `MessageCodes.FieldRequired`; faixa usa `MV104`; conflito usa `MV103` |
| Novos arquivos respeitam o padrão | Sim | testes não expõem texto ao usuário; relatório está em português |

## Revisão final

- Nenhum arquivo de tasks ou plans foi alterado.
- Nenhuma alteração estrutural de banco ou migration foi necessária.
- O publisher compartilhado permanece registrado por DI e encapsula timeout, telemetria e falhas de transporte.
- Sem concerns bloqueantes identificados para a Unidade 3.
