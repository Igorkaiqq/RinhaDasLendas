## Unidade 4: SQL de Publicacao, Versao e Disponibilidade

### Resultado

- Claim, sucesso, falha, expiracao terminal e reconciliacao de expirados atualizam publicacao e pai no mesmo comando SQL PostgreSQL.
- Cada transicao efetiva incrementa `versao_estado`, define `data_atualizacao` e retorna o `DraftMontagemVersionStamp` persistido.
- Reconciliacao com multiplas publicacoes do mesmo draft incrementa o pai uma unica vez e retorna um stamp por draft.
- Republicacao por agregado retorna stamp exato; publicacao ja pendente retorna `null` sem `Touch`, save ou evento.
- Claims concorrentes continuam serializados pelo advisory lock; exatamente um concorrente recebe claim e stamp.
- Handlers e reconciliacao usam `IDraftMontagemRealtimePublisher` somente depois do commit.
- `DraftMontagemSnapshotScope` separa explicitamente o reload ativo/incluindo arquivados do evento de disponibilidade.
- Claim, sucesso, falha, reconciliacao de expirados e republicacao de `Cancelamento` arquivado publicam um shared snapshot com `IncludingArchived` e availability `None`.
- Archive e restore preservam os eventos exatos `DraftMontagemArchived` e `DraftMontagemRestored` via `Archived`/`Restored`.
- O helper, metodo de notificacao personalizada e adapters/doubles obsoletos foram removidos apos a migracao dos callers.
- Feature 030 nao foi alterada.

### TDD

- RED observado: compilacao falhou pela ausencia de `DraftMontagemVersionStamp` e `DraftMontagemPublicacaoClaimResult`.
- GREEN inicial encontrou e corrigiu a visibilidade de CTEs modificadores no snapshot PostgreSQL.
- Teste relacionado de concorrencia encontrou e corrigiu o retorno 404 do vencedor quando a publicacao ainda nao existia.
- A revisao adicionou RED para snapshot de arquivado sem evento de disponibilidade e GREEN com escopo de reload independente.
- Os testes usam bancos PostgreSQL isolados reais por `SecurityApiFactory(useIsolatedPostgreSql: true)`.

### Transacoes e concorrencia

- Claim usa transacao `ReadCommitted` e `pg_advisory_xact_lock` por draft/tipo antes do CTE atomico.
- Sucesso, falha e reconciliacao usam um unico statement com CTE de publicacao seguido do update do pai.
- A reconciliacao deduplica `draft_montagem_id` antes de incrementar o pai.
- Nenhum update do pai ocorre quando o CTE de transicao nao retorna linha.
- Republicacao depende do controle otimista EF existente e publica apenas depois de `SaveChangesAsync` concluir.
- Trigger PostgreSQL de teste prova que falha no update do pai reverte a alteracao do filho no mesmo statement.
- Conflito otimista de republicacao prova rollback de filho/auditoria/pai e zero tentativa de publicacao parcial.
- No-op de republicacao normal e de cancelamento arquivado preserva filho, pai, auditoria e contagem de eventos.
- `versao_estado` permanece a ordenacao autoritativa; `data_atualizacao` nao e usada para resolver concorrencia.

### Verificacao

- Focused + related: 248 passaram, 0 falharam, 0 ignorados.
- Build Release: sucesso, 0 warnings, 0 erros.
- Busca pelos simbolos legados fora de docs/specs: zero referencias.
- `git diff --check`: sucesso.

### Auditoria de internacionalizacao

- Textos hardcoded novos no frontend: Sim, auditado; nenhum frontend foi alterado.
- Mensagens hardcoded novas no backend: Sim, auditado; nenhuma mensagem voltada ao usuario foi adicionada.
- `pt.json` e `en.json` sincronizados: Sim; ambos permaneceram inalterados.
- Resources backend atualizados/sincronizados: Sim; nenhuma nova mensagem exigiu resource.
- Acentuacao em portugues revisada: Sim; nao houve novo texto de interface.
- Placeholders, botoes, titulos, badges, toasts e estados vazios revisados: Sim; fora do escopo e inalterados.
- Validacoes frontend/backend usam i18n/resource: Sim; nenhuma validacao foi adicionada ou alterada.
- Novos arquivos respeitam o padrao: Sim; os novos arquivos sao modelos tecnicos e testes sem mensagem de usuario.

### Concerns

- Nenhum concern bloqueante identificado.
- A publicacao realtime continua deliberadamente best-effort, sem outbox/backplane, conforme o escopo de uma replica da feature 029.
