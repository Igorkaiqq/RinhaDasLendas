# Research: Corrigir Sincronização e Operação do Draft

## R01 - Contrato compartilhado separado do personalizado

**Decision**: Criar `DraftMontagemRealtimeSnapshotDto(Montagem, ServerNow)` exclusivamente para o evento SignalR e preservar o endpoint autenticado como `DraftMontagemRealtimeStateDto(Montagem, ServerNow, CanCurrentUserPick)`.

**Rationale**: O payload de grupo não pode carregar capacidade calculada para o autor da mutação. O DTO separado deixa identidade apenas no GET/resposta individual sem quebrar o contrato HTTP atual.

**Alternatives considered**: Manter `CanCurrentUserPick` no broadcast foi rejeitado por vazamento entre sessões; emitir evento por usuário foi rejeitado por custo e complexidade; remover capacidade foi rejeitado porque a UI precisa da autorização atual.

## R02 - Publisher pós-commit best-effort observável

**Decision**: Implementar `DraftMontagemRealtimePublisher` na Application contra `IDraftMontagemRealtimeNotifier`, `IDraftMontagemRealtimeTelemetry` e `IDraftMontagemRepository`. `PublishAfterCommitAsync(Guid, DraftMontagemAvailabilityChange)` não recebe token da request; cria timeout interno de 5 s e absorve/registra qualquer falha, inclusive `OperationCanceledException` causada por esse timeout. `Archived` recarrega incluindo arquivados; `Restored` usa a consulta normal.

**Rationale**: Falha de transporte não pode transformar commit em erro funcional nem incentivar retry duplicador. Reconciliação HTTP/fallback é o mecanismo de recuperação nesta entrega.

**Alternatives considered**: Implementação na API foi rejeitada por colocar orquestração de caso de uso no adaptador; usar request token foi rejeitado porque desconexão após commit suprimiria publicação; publicar antes do commit permite estado inexistente; retry ilimitado/outbox foram rejeitados pelo escopo.

## R03 - Cobertura completa de mutações

**Decision**: Cobrir presença, modo, capitães, ordem, início, pick, timeout, substituição, layout, finalização, cancelamento, archive/restore e publicação Discord. Os SQL de claim, conclusão, falha e expiração alteram publicação visível e portanto atualizam, na mesma transação/comando, `draft_montagens.versao_estado = versao_estado + 1` e `data_atualizacao`, retornando `DraftMontagemVersionStamp(Id, VersaoEstado, DataAtualizacao)`. Republicação por agregado e reconciliação de expirados seguem a mesma regra e publicam cada stamp retornado após commit; no-op não incrementa nem publica.

**Rationale**: O contrato é snapshot completo; qualquer mudança visível ausente deixa clientes divergentes. A versão evita evento para no-op e rejeição.

**Alternatives considered**: Retornar apenas `bool`/ID foi rejeitado porque não prova a versão publicada; incrementar em segundo comando foi rejeitado por janela de inconsistência; interceptor genérico de EF foi rejeitado porque os SQL raw não passam pelo change tracker.

## R04 - Join autorizado via query

**Decision**: `CanViewDraftMontagemQuery(Guid)` retorna true somente quando `ICurrentUser.UserId` existe, `IsBot` é false e `GetByIdAsync` encontra draft não arquivado. Inexistente, arquivado e identidade não humana usam o mesmo `MessageCodes.DraftRealtimeUnavailable`. `GetDraftMontagemRealtimeStateQueryHandler` aplica exatamente a mesma regra, mantendo paridade GET/Join.

**Rationale**: `[Authorize]` valida autenticação, não acesso ao recurso. A query mantém autorização testável na Application e o Hub fino.

**Alternatives considered**: Checar repositório diretamente no Hub viola camadas; confiar no GUID permite inscrição indevida; policy sem recurso não valida existência/contexto.

## R05 - Projeções mínimas de candidatos

**Decision**: Repositórios retornarão `DraftMontagemRealtimeCandidate(Guid Id)` e `DraftMontagemPresenceClosureCandidate(Guid Id)`. O SQL filtra elegibilidade básica; o comando em scope próprio recarrega e revalida expiração, duração, contagem e estado antes de decidir.

**Rationale**: O worker só precisa endereçar o comando. Levar contagem/instantes ao loop duplica decisão e permite agir sobre projeção obsoleta.

**Alternatives considered**: Manter agregados foi rejeitado por custo; SQL cru foi rejeitado porque projeção EF é suficiente; cache/store novo foi rejeitado por YAGNI.

## R06 - Scope e comando por draft

**Decision**: O scope de scan vive apenas até materializar IDs; cada ID usa novo scope e comando que recarrega/revalida. Cada item captura e observa qualquer `Exception`, inclusive `OperationCanceledException` não relacionada ao host, e continua. Somente `OperationCanceledException` quando `stoppingToken.IsCancellationRequested` propaga e encerra o worker.

**Rationale**: Isola tracking, transação, autoria e falha. Um conflito não contamina o `DbContext` nem interrompe os demais candidatos.

**Alternatives considered**: Um scope/lote foi rejeitado por estado contaminado; paralelismo irrestrito foi rejeitado por complexidade e carga; mutar agregado no hosted service foi rejeitado por contornar CQRS.

## R07 - Autoria sistêmica explícita e migration aditiva

**Decision**: Modelar `DraftMontagemActor(Tipo, UsuarioId)` com `User` e `System`. A migration adiciona `responsavel_tipo`, torna `responsavel_usuario_id` nullable e preserva histórico. `DraftMontagemAcaoAdministrativaResponseDto` e OpenAPI expõem `ResponsavelTipo` e `ResponsavelUsuarioId?`; TypeScript acompanha e a UI mostra nome/ID para `User` ou label localizada `Sistema`/`System` para `System`.

**Rationale**: Um GUID sintético simularia humano e exigiria usuário-semente frágil. Tipo explícito é auditável e preserva registros existentes.

**Alternatives considered**: Usuário técnico foi rejeitado por falsa identidade e ciclo de vida; `Guid.Empty` viola domínio/FK; texto livre foi rejeitado por baixa integridade. Nenhuma migration adicional de índice é prevista porque já existe índice `(status, modo, turno_expira_em)`.

## R08 - Responsabilidades frontend

**Decision**: `DraftsView.vue` coordena geração, lanes, GET canônico, fallback de 3000/2000 ms, status final, conflito e guardas; `draftMontagemRealtime.ts` possui start/Join/retry/callbacks/stop e entrega readiness/degradação sem declarar `connected`; `DraftVisualBoard.vue` possui clone local, dirty, versão-base e emite estado/intenções.

**Rationale**: A view conhece seleção e navegação, o service conhece SignalR e o board conhece edição. Evita store novo e mantém responsabilidades atuais.

**Alternatives considered**: Pinia/store novo foi rejeitado por escopo local; colocar polling no board mistura transporte e apresentação; colocar dirty na view sem clone no board perde a fonte real da edição.

## R09 - Start -> Join -> GET e ciclo de conexão

**Decision**: Iniciar conexão, registrar callbacks, executar Join autorizado e só então GET personalizado. A abertura inicial e a recuperação só emitem `connected` depois de start + Join + GET canônico bem-sucedidos. Falha de Join/rejoin interrompe a conexão corrente e agenda restart único com `[0, 2000, 5000, 10000, 15000]`; falha do GET mantém o estado degradado. Em ambos os casos o fallback consulta a cada 3000 ms com timeout de 2000 ms por requisição. `onclose` segue o mesmo caminho.

**Rationale**: Join antes do GET fecha a janela de perda; comparação de versão resolve evento entre Join e resposta. O limite de 3000 ms + 2000 ms torna verificável a convergência de pior caso em até 5000 ms depois que o backend volta. `onclose` cobre esgotamento da política automática.

**Alternatives considered**: GET antes de Join mantém a janela; `withAutomaticReconnect()` padrão não cobre fechamento final; polling permanente cria carga e ruído.

## R10 - Chave de snapshot, geração e lanes

**Decision**: Aceitar compartilhado por `(draftId, generation, versaoEstado)` e somente quando versão for estritamente maior. Lanes `passive`/`mutation` mantêm request IDs próprios para shared, enquanto toda resposta HTTP personalizada recebe `personalizedSequence` global; metadados só aplicam se essa sequência superar `lastPersonalizedSequence` e a versão não for inferior ao shared atual. O detalhe administrativo que inclui o draft canônico percorre a lane passiva. Busca auxiliar de elegíveis de presença/capitães usa `auxiliaryRequestId` e `AbortController` próprios.

**Rationale**: Ordem de resolução HTTP/SignalR é independente. Uma sequência global de requests pode descartar sucesso novo quando refresh antigo termina depois.

**Alternatives considered**: Timestamp foi rejeitado por relógios/ordenação; última resposta vence foi rejeitado por regressão; uma lane global foi rejeitada por interferência entre leitura e escrita.

## R11 - Busca auxiliar opcional

**Decision**: Elegíveis de presença/capitães são enriquecimento opcional com `auxiliaryRequestId` e `AbortController` próprios. Iniciar/finalizar busca auxiliar nunca incrementa request IDs das lanes canônicas nem `personalizedSequence`. Esta regra não se aplica ao detalhe administrativo que transporta o draft canônico, que usa a lane passiva.

**Rationale**: A busca não é estado canônico do draft e não pode bloquear sincronização.

**Alternatives considered**: Incorporar ao snapshot aumenta payload e personalização; falhar abertura inteira degrada ações não relacionadas.

## R12 - Reconciliação de 409

**Decision**: Todas as mutações do ciclo tratam HTTP 409 como barreira: bloqueiam nova tentativa, executam GET personalizado imediato na geração atual, aplicam a maior versão e só então liberam a ação. Layout dirty preserva clone e versão-base até decisão.

**Rationale**: O 409 prova que a base local não é canônica; repetir antes de reconciliar pode duplicar efeitos ou apresentar controles indevidos.

**Alternatives considered**: Retry automático foi rejeitado por duplicação; apenas toast mantém estado obsoleto; aplicar body do erro foi rejeitado porque o contrato de erro não é snapshot.

## R13 - Diálogo único de layout e guardas

**Decision**: `DraftVisualBoard` recebe `canonicalResetToken: number` e `acceptedSaveVersion: number | null`. Remoto maior durante dirty não altera clone. Descartar aplica o maior canônico na view e incrementa `canonicalResetToken`, forçando clone/reset. Salvar limpa dirty somente quando `acceptedSaveVersion` corresponde à resposta da mutação de layout vigente e às props canônicas dessa versão; respostas antigas ou eventos iguais não limpam.

**Rationale**: Uma intenção pendente evita modais concorrentes e decisões inconsistentes. `beforeunload` usa confirmação nativa por restrição do navegador; navegação interna usa o diálogo localizado.

**Alternatives considered**: Um modal por origem duplica lógica; sobrescrever silenciosamente viola FR-022; autosave foi rejeitado porque mudaria regra e pode salvar sobre base obsoleta.

## R14 - Status acessível PT/EN

**Decision**: Representar `connected`, `reconnecting`, `fallback`, `disconnected`; renderizar somente degradados em região `role="status"`, `aria-live="polite"`, com chaves idênticas em `pt.json`/`en.json` e acentuação revisada.

**Rationale**: Estado saudável não deve gerar ruído; degradação precisa ser perceptível sem depender de cor.

**Alternatives considered**: Toast repetido foi rejeitado por ruído; badge apenas visual falha em acessibilidade; texto hardcoded viola padrão do projeto.

## R15 - Infraestrutura deliberadamente não adicionada

**Decision**: Operar com snapshot completo, uma réplica e reconciliação HTTP; não adicionar Redis, backplane, outbox, event sourcing ou store frontend.

**Rationale**: As falhas são de contrato, ordering e lifecycle, não de fan-out entre réplicas. O escopo e a constituição pedem a menor solução correta.

**Alternatives considered**: Redis não corrige payload personalizado nem ordem; outbox excede a recuperação exigida; eventos incrementais ampliam superfície de consistência.

## R16 - Disponibilidade archive/restore

**Decision**: Manter `DraftMontagemArchived(Guid)` e adicionar `DraftMontagemRestored(Guid)`. Archive publica snapshot usando `ReloadByIdIncludingArchivedAsync`, depois availability archived; restore publica snapshot normal e availability restored. Ambos passam pelo publisher best-effort e timeout interno.

**Rationale**: Clientes de lista e clientes ainda no grupo precisam distinguir indisponibilidade e retorno sem inferir por ausência. O carregamento padrão exclui arquivados, portanto archive exige consulta explícita.

**Alternatives considered**: Um evento genérico sem estado foi rejeitado por compatibilidade e ambiguidade; omitir restore deixa listas administrativas obsoletas.

## R17 - Migração completa do helper antigo

**Decision**: Na unidade de handlers, migrar apenas as mutações não relacionadas à publicação e manter aditivos o método antigo do notifier, seu adapter e doubles para preservar compilação. Na unidade de SQL/publicação, migrar `DraftMontagemPublicationReconciliationService`, handlers de publicação e todos os testes/doubles; só então remover `DraftMontagemRealtimeNotificationPublisher.cs`, o método antigo do notifier, seu adapter e doubles após busca de zero referências e build verde.

**Rationale**: Deletar antes deixa callers quebrados; manter dois caminhos preserva semânticas divergentes de retry/cancelamento.

**Alternatives considered**: Adapter temporário foi rejeitado porque perpetua o helper que propaga falha pós-commit.
