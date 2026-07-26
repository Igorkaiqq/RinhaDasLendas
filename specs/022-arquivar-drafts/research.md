# Research: Arquivamento Administrativo de Drafts

## Decisão 1: arquivamento separado do status operacional

**Decision**: Representar o estado atual por `ArquivadoEm`, `ArquivadoPorUsuarioId` e `MotivoArquivamento`. `Arquivado` é derivado de `ArquivadoEm != null`; restauração limpa os três metadados atuais, enquanto ações administrativas imutáveis preservam ciclos anteriores.

**Rationale**: Um novo status apagaria a diferença entre `Finalizada` e `Cancelada`. Metadados independentes permitem ocultar/restaurar sem reconstruir o fluxo.

**Alternatives considered**:

- Novo status `Arquivada`: rejeitado por misturar ciclo operacional e visibilidade administrativa.
- Booleano adicional persistido: rejeitado por duplicar uma informação derivável e exigir constraint extra de sincronização.
- Exclusão física: rejeitada por destruir histórico e conflitar com relações `Restrict`.

## Decisão 2: drafts ativos são cancelados dentro do agregado

**Decision**: `DraftMontagem.Arquivar` recebe motivo, responsável e instante. Nos cinco estados não terminais, cancela o draft, limpa turno/prazos, registra ações distintas de `CancelamentoPorArquivamento` e `Arquivamento` e cria publicação `Cancelamento/Pendente`. Em `Finalizada` e `Cancelada`, apenas arquiva. O tipo específico continua representando cancelamento, mas permite que a projeção de Moderador o diferencie de cancelamentos operacionais comuns.

**Rationale**: Ocultar sem cancelar deixaria timers, presença e escolhas ativos. Uma única transição de domínio protege a invariante de que nenhum draft arquivado permanece operacional.

**Alternatives considered**:

- Pausar e retomar: rejeitado por exigir reconstrução segura de prazos, turnos e claims.
- Cancelar em handler e arquivar na entidade: rejeitado por dividir uma invariante crítica.

## Decisão 3: uma persistência atômica com intenção Discord durável

**Decision**: Persistir raiz, metadados, ações e publicação de cancelamento em um único `SaveChanges`. O bot só envia após o commit; falha do Discord altera apenas o estado da publicação e nunca desfaz o arquivamento.

**Rationale**: I/O externo não participa de transação PostgreSQL. A publicação existente já oferece claim, conclusão, falha e reconciliação duráveis.

**Alternatives considered**:

- Enviar Discord no handler: rejeitado por permitir mensagem sem commit e bloquear a operação na integração.
- Criar intenção sob demanda no claim: rejeitado porque quebraria a prova de atomicidade do arquivamento.
- Outbox genérico novo: rejeitado por duplicar o protocolo de publicação existente.

**Accepted external race**: Uma publicação operacional cujo `channel.send` começou antes do commit do arquivamento não pode ser desfeita atomicamente. Ao arquivar, claims antigos são invalidados para conclusão, o bot revalida o estado imediatamente antes do envio e a publicação de cancelamento compensatória é priorizada. A janela residual entre revalidação e Discord é documentada e coberta por teste; manter transação ou lock do banco durante I/O externo foi rejeitado por indisponibilidade e deadlock.

## Decisão 4: novo tipo de publicação `Cancelamento`

**Decision**: Estender o enum e os contratos do bot com `Cancelamento`. Draft arquivado entra no polling apenas se possuir essa publicação em `Pendente`, `EmAndamento` ou `RequerReconciliacao`; publicações antigas de presença/chamada/times ficam inoperantes.

**Rationale**: A query atual mantém terminais com qualquer publicação acionável e o bot prioriza estados explícitos. Filtrar tanto no backend quanto no candidato do bot evita envio operacional obsoleto.

**Alternatives considered**:

- Reutilizar `TimesDefinidos`: rejeitado por semântica, mensagem e regras de claim incorretas.
- Remover todas as publicações ao arquivar: rejeitado por apagar histórico e impedir o cancelamento aprovado.

## Decisão 5: policy dedicada Admin+

**Decision**: Criar `CanArchiveDrafts`, concedida somente a `Admin` e `SuperAdmin`, registrada nos ambientes normal e Testing e devolvida por `/api/v1/auth/me/permissions`. `includeArchived=true`, arquivar, restaurar e consultar histórico usam essa policy.

**Rationale**: `CanManageDrafts` inclui `Moderador`. Reutilizá-la concederia uma capacidade destrutiva além do requisito.

**Alternatives considered**:

- Verificar role apenas no frontend: rejeitado porque o backend é a fonte de verdade.
- Reutilizar `CanManageUsers`: rejeitado porque o nome e a evolução da capacidade seriam incorretos.

## Decisão 6: projeção administrativa dedicada

**Decision**: Listas e detalhes públicos expõem somente `arquivado` e `versaoEstado`. Motivo, responsável e ações de arquivamento ficam em `GET /{id}/arquivamento`, protegido por Admin+. O endpoint `/administracao`, acessível a Moderador, filtra os tipos `Arquivamento`, `Restauracao` e `CancelamentoPorArquivamento`.

**Rationale**: Admin precisa selecionar e restaurar um item arquivado sem ampliar o histórico sensível para Moderadores ou usuários comuns.

**Alternatives considered**:

- Restringir `/administracao` inteiro a Admin+: rejeitado por regredir capacidades operacionais de Moderador.
- Retornar metadados na listagem: rejeitado por exposição e payload desnecessários.

## Decisão 7: concorrência otimista explícita no contrato

**Decision**: Expor `versaoEstado` em resumo e detalhe. Arquivar e restaurar recebem a versão observada. O handler compara a versão antes de mutar, usa `TrySaveChangesAsync`, limpa o tracker e recarrega incluindo arquivados após conflito. Repetição para estado já atingido retorna o estado atual sem auditoria; dois arquivamentos preservam o primeiro autor/motivo; transições opostas ou operação concorrente retornam `409` para a perdedora. Violação concorrente da unicidade de publicação `Cancelamento` é classificada como convergência ou conflito após recarga, nunca como `500`.

**Rationale**: O token atual detecta corrida no banco, mas a versão do cliente é necessária para distinguir repetição após timeout de uma restauração baseada em estado obsoleto.

**Alternatives considered**:

- Somente `DbUpdateConcurrencyException`: rejeitado porque uma requisição velha pode chegar após outra transação já concluída.
- Última escrita vence: rejeitado por sobrescrever decisão administrativa sem revisão.

## Decisão 8: filtros explícitos, sem query filter global

**Decision**: `GetByIdAsync`, listagens, timers, elegibilidade e comandos normais exigem `ArquivadoEm == null`. Métodos `IncludingArchived` ficam limitados a Admin+ e ao cancelamento Discord pendente.

**Rationale**: Um filtro global exigiria `IgnoreQueryFilters` em caminhos sensíveis e poderia ocultar a publicação de cancelamento do bot.

**Alternatives considered**:

- `HasQueryFilter`: rejeitado por efeitos implícitos em consultas administrativas, polling e SQL manual.
- Filtrar apenas no frontend: rejeitado por permitir URL direta, realtime e mutações.

## Decisão 9: realtime sem dados sensíveis

**Decision**: Após arquivar, emitir `DraftMontagemArchived` somente com o ID. Clientes removem o resumo e limpam a seleção. Não transmitir DTO completo, motivo ou responsável no grupo existente.

**Rationale**: Usuários autenticados podem estar inscritos no grupo por ID. O evento mínimo revoga a visão sem revelar metadados administrativos.

**Alternatives considered**:

- Reutilizar `StateUpdated`: rejeitado por expor o estado arquivado e permitir ações durante reconciliação.
- Não notificar: rejeitado porque outras sessões manteriam o draft visível até novo carregamento.

## Decisão 10: carregamento interno e republicação contextual

**Decision**: Criar métodos `GetByIdIncludingArchivedAsync` e `ReloadByIdIncludingArchivedAsync` restritos aos handlers Admin+ e de publicação `Cancelamento`. Conclusão ou falha do cancelamento arquivado não chama o notifier de estado completo. Republicar um tipo operacional preserva o endpoint com `CanManageDrafts`; republicar `Cancelamento` em draft arquivado usa endpoint e command separados protegidos por `CanArchiveDrafts` na API.

**Rationale**: Filtrar `GetByIdAsync` é necessário para bloquear comandos normais, mas o bot precisa concluir sua intenção interna sem receber `404`; Moderador não pode ganhar uma operação sobre arquivados por meio do endpoint de republicação existente.

**Alternatives considered**:

- Fazer `GetByIdAsync` incluir arquivados: rejeitado por reabrir todos os comandos e acessos diretos.
- Restringir toda republicação a Admin+: rejeitado por regredir a operação normal de Moderadores.
- Inspecionar roles ou policies dentro do handler genérico: rejeitado por duplicar autorização do framework na Application.

## Decisão 11: estender componentes existentes

**Decision**: Manter `DraftsView.vue` como orquestrador. `DraftNavigator` recebe filtro e badge; `DraftWorkspaceHeader` recebe badge; `DraftReasonDialog` cobre arquivar/restaurar; `DraftDiscordPublicationPanel` cobre cancelamento. Nenhum componente importa autenticação ou serviço.

**Rationale**: O fluxo da feature 021 já possui limites, foco, concorrência e responsividade testados.

**Alternatives considered**:

- Tela separada de arquivados: rejeitada por duplicar navegação e seleção.
- Novos dialogs: rejeitados porque o dialog contextual existente já cobre motivo, confirmação e foco.

## Decisão 12: atualização editorial somente na entrega

**Decision**: Adicionar a próxima versão disponível em `systemUpdates.ts` durante a implementação final, com PT/EN e link para Drafts, somente depois dos gates locais.

**Rationale**: O padrão do produto proíbe anunciar funcionalidade ainda indisponível e exige exatamente uma release destacada.

**Alternatives considered**:

- Fixar antecipadamente `2026.07.5`: rejeitado porque outra entrega pode ocupar a sequência antes do merge.

## Decisão 13: códigos e recursos localizados estáveis

**Decision**: Reservar `MV101` para motivo obrigatório, `MV102` para limite de 500 caracteres, `MV103` para conflito de estado e `MV104` para versão inválida; usar `MSIS029` e `MSIS030` para arquivamento e restauração concluídos. Todos existem em resources base, pt-BR e en-US. Recurso inacessível reutiliza `ME035` para não revelar se o draft arquivado existe.

**Rationale**: O frontend precisa diferenciar validação e conflito sem interpretar texto, e todas as mensagens da API devem obedecer à localização atual.

**Alternatives considered**:

- Mensagens hardcoded nos handlers: rejeitadas pelo padrão de internacionalização.
- Reutilizar um erro genérico para conflito: rejeitado porque impediria tratamento seguro de recarga e nova confirmação.
