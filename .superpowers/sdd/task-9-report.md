# Relatório da Task 9

## Status

T093 a T096 concluídas no checkout autorizado da branch `feature/016-melhorias-drafts-presenca-discord`.

Confirmação e cancelamento repetidos são no-op no agregado. Concorrência de persistência é reconciliada apenas quando o estado exato solicitado já foi alcançado; conflitos diferentes continuam sendo propagados ou retornam o código localizado `MV088`.

## RED/GREEN

### RED

- O teste de domínio falhou porque o segundo cancelamento lançava `MV073`.
- Duas requisições HTTP de confirmação, liberadas simultaneamente por barreira, produziram uma resposta HTTP 500.
- A primeira execução completa no devcontainer identificou a ausência do socket Docker para Testcontainers; o proxy temporário documentado foi usado na verificação final.
- A execução com Testcontainers revelou que `PostgreSqlApiFactory` dependia de configuração externa para criar o usuário do JWT. A factory passou a configurar seu próprio SuperAdmin de teste.

### GREEN

- Confirmação repetida retorna a mesma presença, sem alterar `VersaoEstado`.
- Cancelamento repetido mantém a presença cancelada, sem exceção ou nova versão.
- As duas confirmações HTTP concorrentes retornam HTTP 200 e o PostgreSQL mantém uma presença confirmada.
- No-op não chama persistência, SignalR nem métricas.
- Violação única fora dos dois índices esperados de presença continua lançando `DbUpdateException`.
- Suíte focada final: 80 aprovados, 0 falhas.
- Backend completo: 154 aprovados, 0 falhas.

## Persistência

- `TrySaveChangesAsync` retorna `Persistido`, `ConflitoDeVersao` ou `ConflitoDePresencaConfirmada` sem expor tipos do EF no Domain.
- `DbUpdateConcurrencyException` é traduzida somente para `ConflitoDeVersao`.
- PostgreSQL `23505` é traduzido somente para os índices `ix_draft_montagem_presencas_draft_montagem_id_usuario_id` e `ix_draft_montagem_presencas_draft_montagem_id_jogador_id`.
- `ReloadByIdAsync` limpa o change tracker antes de recarregar o agregado completo.
- Handlers retornam sucesso sem efeitos duplicados somente quando a confirmação ou o cancelamento exato já está persistido.
- Conflitos de versão com estado divergente retornam `MV088`; demais falhas de banco não são engolidas.

## Verificações

- `dotnet test BackEnd/RinhaDasLendas.sln --filter "FullyQualifiedName~DraftMontagemTests|FullyQualifiedName~DraftMontagemBehaviorIntegrationTests" --configuration Release`: 47 aprovados na primeira GREEN focada.
- Foco ampliado de domínio, integração e segurança: 80 aprovados, 0 falhas.
- `dotnet test BackEnd/RinhaDasLendas.sln --configuration Release`: 154 aprovados, 0 falhas.
- `dotnet build BackEnd/RinhaDasLendas.sln --configuration Release`: aprovado, 0 erros e 2 avisos NU1903.
- `dotnet ef migrations has-pending-model-changes`: nenhum change pendente.
- `git diff --check`: aprovado antes do relatório; repetido antes do commit.
- Proxy Docker temporário e override temporário de porta PostgreSQL removidos após os testes.

## Auditoria de internacionalização

- Textos hardcoded no frontend encontrados: Não; frontend não alterado.
- Mensagens hardcoded no backend encontradas: Não.
- `pt.json` e `en.json` sincronizados: Sim; catálogos frontend não alterados.
- Recursos backend atualizados: Sim; `MV088` existe em padrão, `pt-BR` e `en-US`.
- Acentuação em português revisada: Sim.
- Placeholders, botões, títulos, badges, toasts e estados vazios revisados: Sim; UI não alterada.
- Validações frontend e backend usam i18n/recurso: Sim; nenhuma validação nova exibe texto direto.
- Novos arquivos respeitam o padrão: Sim.

## Concerns

- `Microsoft.OpenApi` 2.4.1 continua emitindo NU1903 por vulnerabilidade conhecida; dependência não alterada nesta task.
- Testcontainers ainda requer proxy Docker temporário quando a suíte completa roda no devcontainer sem socket montado.
