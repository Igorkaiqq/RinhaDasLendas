# Design: Arquivamento de Drafts Antigos

## Contexto

A feature 016 definiu que drafts cancelados deveriam permanecer no banco e ficar ocultos da listagem padrão, mas não definiu uma ação administrativa formal para arquivar e restaurar drafts antigos. O sistema precisa reduzir a poluição da lista sem apagar histórico, presenças, escolhas, times, publicações Discord ou auditoria.

## Objetivo

Permitir que usuários com role `Admin` ou `SuperAdmin` arquivem drafts cancelados ou finalizados e possam restaurá-los posteriormente. O arquivamento deve ser reversível, auditável e independente do status que representa a etapa do draft.

## Fora de Escopo

- Exclusão física de drafts ou registros relacionados.
- Arquivamento de drafts em andamento.
- Arquivamento automático por idade.
- Permissão de arquivamento para `Moderador`, `Capitão` ou `Jogador`.
- Alteração de publicações ou mensagens existentes no Discord.

## Modelo de Domínio

`DraftMontagem` recebe metadados de arquivamento separados do status operacional:

- `ArquivadoEm`: instante UTC em que o draft foi arquivado; nulo quando ativo na listagem.
- `ArquivadoPorUsuarioId`: usuário responsável pelo arquivamento; nulo quando não arquivado.
- `MotivoArquivamento`: motivo obrigatório, normalizado e persistido; nulo quando não arquivado.

O status original permanece `Cancelada` ou `Finalizada`. Assim, restaurar um draft apenas limpa os metadados atuais de arquivamento e não precisa reconstruir seu estado anterior.

As ações administrativas existentes registram eventos imutáveis de `Arquivamento` e `Restauração`, com responsável, instante e motivo quando aplicável. Restaurar limpa o estado atual de arquivamento, mas não remove o histórico dessas ações.

## Regras

- Apenas drafts com status `Cancelada` ou `Finalizada` podem ser arquivados.
- Apenas `Admin` e `SuperAdmin` podem arquivar ou restaurar.
- Arquivar um draft já arquivado e restaurar um draft não arquivado são operações idempotentes.
- O motivo de arquivamento é obrigatório e não pode conter apenas espaços.
- A restauração não exige motivo.
- Arquivamento não altera presenças, picks, times, publicações Discord ou status do draft.
- Draft arquivado não participa da listagem padrão nem da varredura operacional do bot.
- Acesso direto por identificador continua disponível para `Admin` e `SuperAdmin`; demais usuários recebem o mesmo comportamento de recurso não encontrado usado para itens inacessíveis.

## Autorização

O backend é a fonte de verdade. Uma policy específica, separada de `CanManageDrafts`, exige role `Admin` ou `SuperAdmin`. Isso evita conceder a ação ao `Moderador`, que atualmente também possui `CanManageDrafts`.

O frontend usa a permissão retornada pelo backend apenas para exibir controles e filtros. Requisições sem a role necessária continuam bloqueadas no endpoint.

## API e Aplicação

Endpoints administrativos:

- `POST /api/v1/draft-montagens/{id}/arquivar`
  - Corpo: `{ "motivo": "..." }`.
  - Retorna o draft atualizado.
- `POST /api/v1/draft-montagens/{id}/restaurar`
  - Sem corpo obrigatório.
  - Retorna o draft atualizado.

A listagem recebe `includeArchived=false` por padrão. O filtro `includeArchived=true` é aceito apenas para `Admin` e `SuperAdmin`; para os demais usuários, arquivados permanecem excluídos.

Commands e queries permanecem separados. Handlers validam existência, regra de status, idempotência e registram a ação administrativa. Controllers apenas recebem a requisição, aplicam policy, enviam o command/query e retornam a resposta.

## Interface

- A listagem padrão não mostra drafts arquivados.
- `Admin` e `SuperAdmin` veem um filtro para incluir arquivados.
- Drafts arquivados recebem badge localizado de arquivado.
- Em draft `Cancelada` ou `Finalizada`, `Admin` e `SuperAdmin` veem a ação de arquivar.
- A ação abre confirmação com campo de motivo obrigatório.
- Em draft arquivado, a ação disponível é restaurar, com confirmação simples.
- Após arquivar, a seleção atual é limpa ou avança para o primeiro draft visível.
- Após restaurar, a listagem é atualizada sem recarregar a página inteira.

Todos os títulos, botões, badges, confirmações, validações, estados vazios e mensagens usam chaves equivalentes em `pt.json` e `en.json`.

## Persistência

Uma migration adiciona as três colunas de arquivamento em `draft_montagens`, com chave estrangeira restritiva para `usuarios`. A consulta padrão deve usar um índice parcial sobre a ordenação da listagem com predicado `arquivado_em IS NULL`, sem alterar os relacionamentos existentes.

Não existe cascade de exclusão, pois a feature não exclui registros.

## Erros

- Draft inexistente ou inacessível: resposta padrão de recurso não encontrado.
- Status diferente de `Cancelada` ou `Finalizada`: erro de domínio localizado.
- Motivo vazio: erro de validação localizado.
- Usuário sem role adequada: `403 Forbidden` pela policy.
- Falha concorrente ao arquivar/restaurar: a operação deve convergir para o estado solicitado ou retornar conflito estruturado, sem duplicar auditoria.

Mensagens do backend usam resources em português e inglês com `messageCode` estável. O frontend mapeia códigos para textos localizados e não exibe detalhes técnicos.

## Testes

### Domínio

- Arquiva draft cancelado e finalizado.
- Rejeita arquivamento de draft em andamento.
- Exige motivo não vazio.
- Arquivamento e restauração são idempotentes.
- Restauração preserva status e histórico administrativo.

### Aplicação e Integração

- `Admin` e `SuperAdmin` podem arquivar/restaurar.
- `Moderador` e demais roles recebem `403`.
- Listagem padrão exclui arquivados.
- Filtro administrativo inclui arquivados.
- Acesso direto respeita visibilidade por role.
- Concorrência não cria ações administrativas duplicadas.

### Frontend

- Controles aparecem apenas com a permissão específica.
- Motivo é obrigatório no arquivamento.
- Arquivar remove o item da lista padrão.
- Filtro exibe arquivados e permite restaurar.
- Chaves `pt.json` e `en.json` permanecem sincronizadas.

## Critérios de Aceite

- Um `Admin` ou `SuperAdmin` arquiva um draft cancelado/finalizado informando motivo e ele desaparece da lista padrão.
- O mesmo usuário inclui arquivados no filtro, encontra o draft e o restaura.
- O draft restaurado volta à lista com o mesmo status, presenças, times, picks e publicações.
- `Moderador` e demais roles não conseguem executar nem visualizar as ações administrativas.
- Nenhum registro de draft é excluído fisicamente.
