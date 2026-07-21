# Relatório da Task 9

## Status

T093 a T096 concluídas no checkout autorizado da branch `feature/016-melhorias-drafts-presenca-discord`.

Confirmação e cancelamento repetidos são no-op no agregado. Concorrência de persistência é reconciliada apenas quando o estado exato solicitado já foi alcançado; conflitos diferentes continuam sendo propagados ou retornam o código localizado `MV088`.

Os findings Medium de revisão foram corrigidos em incremento separado, sem amend: a concorrência HTTP agora é coordenada dentro do servidor após os dois carregamentos da mesma versão, confirmação e cancelamento exercitam a branch real de conflito e as classificações/reloads possuem cobertura determinística.

## RED/GREEN

### RED

- O teste de domínio falhou porque o segundo cancelamento lançava `MV073`.
- Duas requisições HTTP de confirmação, liberadas simultaneamente por barreira, produziram uma resposta HTTP 500.
- A primeira execução completa no devcontainer identificou a ausência do socket Docker para Testcontainers; o proxy temporário documentado foi usado na verificação final.
- A execução com Testcontainers revelou que `PostgreSqlApiFactory` dependia de configuração externa para criar o usuário do JWT. A factory passou a configurar seu próprio SuperAdmin de teste.
- A revisão mostrou que a barreira HTTP original sincronizava somente o envio dos clientes e não provava que ambos os handlers carregaram a mesma versão.
- A revisão também mostrou ausência de testes diretos das branches de reconciliação dos handlers e da classificação exata dos conflitos de persistência.

### GREEN

- Confirmação repetida retorna a mesma presença, sem alterar `VersaoEstado`.
- Cancelamento repetido mantém a presença cancelada, sem exceção ou nova versão.
- As duas confirmações HTTP concorrentes carregam a mesma versão antes de qualquer save, retornam HTTP 200 e o PostgreSQL mantém uma presença confirmada.
- Dois cancelamentos HTTP concorrentes carregam a mesma versão, retornam HTTP 200 e persistem estado cancelado.
- Em ambos os fluxos, os dois saves observam a barreira completa; há um `Persistido`, um conflito classificado e somente uma notificação/métrica.
- No-op não chama persistência, SignalR nem métricas.
- Violação única fora dos dois índices esperados de presença continua lançando `DbUpdateException`.
- Handlers de confirmar/cancelar cobrem conflitos de versão e presença: estado desejado retorna sucesso sem efeitos; estado divergente retorna `MV088`.
- Classificador puro cobre `DbUpdateConcurrencyException`, cada constraint exata de presença e constraint diferente.
- Reload real comprova `ChangeTracker.Clear`: entidade anterior fica detached e mutação não persistida não contamina o estado recarregado.
- Suíte focada final: 94 aprovados, 0 falhas.
- Backend completo: 168 aprovados, 0 falhas.

## Persistência

- `TrySaveChangesAsync` retorna `Persistido`, `ConflitoDeVersao` ou `ConflitoDePresencaConfirmada` sem expor tipos do EF no Domain.
- `DbUpdateConcurrencyException` é traduzida somente para `ConflitoDeVersao`.
- PostgreSQL `23505` é traduzido somente para os índices `ix_draft_montagem_presencas_draft_montagem_id_usuario_id` e `ix_draft_montagem_presencas_draft_montagem_id_jogador_id`.
- `ReloadByIdAsync` limpa o change tracker antes de recarregar o agregado completo.
- Handlers retornam sucesso sem efeitos duplicados somente quando a confirmação ou o cancelamento exato já está persistido.
- Conflitos de versão com estado divergente retornam `MV088`; demais falhas de banco não são engolidas.
- `DraftMontagemSaveConflictClassifier` permanece internal à Infrastructure e não expõe EF/Npgsql ao Domain ou Application.

## Concorrência e linearização

- O decorator test-only de `IDraftMontagemRepository` coordena as duas requisições após `GetByIdAsync` carregar o agregado e antes de `TrySaveChangesAsync`.
- Os testes registram as duas versões carregadas, a quantidade de loads observada por cada save e os resultados de persistência.
- Nenhum hook de teste foi adicionado a código de produção.
- O vencedor lineariza no commit do banco. O sucesso reconciliado lineariza no instante do reload que observa o estado desejado.
- Outra requisição pode alterar o estado depois desse instante e antes de a resposta HTTP chegar; o contrato não promete estabilidade até a chegada ao cliente.

## Verificações

- `dotnet test BackEnd/RinhaDasLendas.sln --filter "FullyQualifiedName~DraftMontagemTests|FullyQualifiedName~DraftMontagemBehaviorIntegrationTests" --configuration Release`: 47 aprovados na primeira GREEN focada.
- Foco ampliado de domínio, integração, handlers, classifier e segurança: 94 aprovados, 0 falhas.
- `dotnet test BackEnd/RinhaDasLendas.sln --configuration Release`: 168 aprovados, 0 falhas.
- `dotnet build BackEnd/RinhaDasLendas.sln --configuration Release`: aprovado, 0 erros e 2 avisos NU1903.
- `dotnet ef migrations has-pending-model-changes`: nenhum change pendente.
- `git diff --check`: aprovado antes do relatório; repetido antes do commit.
- Proxy Docker temporário e override temporário de porta PostgreSQL removidos após os testes.
- Proxy Docker temporário da correção de review removido após a suíte completa.

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
