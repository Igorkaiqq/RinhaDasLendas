# Design: Arquivamento de Drafts Antigos

## Contexto

A feature 016 definiu que drafts cancelados deveriam permanecer no banco e ficar ocultos da listagem padrão, mas não definiu uma ação administrativa formal para arquivar e restaurar drafts antigos. O sistema precisa reduzir a poluição da lista sem apagar histórico, presenças, escolhas, times, publicações Discord ou auditoria.

## Objetivo

Permitir que usuários com role `Admin` ou `SuperAdmin` arquivem drafts em qualquer estado e possam restaurá-los posteriormente. O arquivamento deve ser reversível, auditável e independente do status que representa a etapa do draft. Quando o draft ainda estiver ativo, arquivá-lo também o cancela definitivamente para interromper a operação com segurança.

## Fora de Escopo

- Exclusão física de drafts ou registros relacionados.
- Arquivamento automático por idade.
- Permissão de arquivamento para `Moderador`, `Capitão` ou `Jogador`.
- Alteração de publicações ou mensagens existentes no Discord.

## Modelo de Domínio

`DraftMontagem` recebe metadados de arquivamento separados do status operacional:

- `ArquivadoEm`: instante UTC em que o draft foi arquivado; nulo quando ativo na listagem.
- `ArquivadoPorUsuarioId`: usuário responsável pelo arquivamento; nulo quando não arquivado.
- `MotivoArquivamento`: motivo obrigatório, normalizado e persistido; nulo quando não arquivado.

O status permanece inalterado quando já for `Cancelada` ou `Finalizada`. Um draft em andamento muda para `Cancelada` na mesma operação que registra o arquivamento. Assim, restaurar apenas limpa os metadados atuais de arquivamento e nunca tenta reconstruir prazos, turnos ou o estado ativo anterior.

As ações administrativas existentes registram eventos imutáveis de `Arquivamento` e `Restauracao`, com responsável, instante e motivo quando aplicável. Arquivar um draft ativo registra também a ação distinta `CancelamentoPorArquivamento`, usando o mesmo motivo, sem substituir nem duplicar a ação de arquivamento. O tipo específico permite ocultar esse histórico na projeção acessível a Moderadores. Restaurar limpa o estado atual de arquivamento, mas não remove o histórico dessas ações.

## Regras

- Drafts nos estados `PresencaAberta`, `PresencaEncerrada`, `CapitaesDefinidos`, `OrdemDefinida`, `Aberta`, `Finalizada` e `Cancelada` podem ser arquivados.
- Arquivar um draft não terminal deve cancelá-lo na mesma transação, usando o motivo de arquivamento também como motivo de cancelamento.
- Apenas `Admin` e `SuperAdmin` podem arquivar ou restaurar.
- Arquivar um draft já arquivado e restaurar um draft não arquivado são operações idempotentes quando a solicitação é válida e não geram nova auditoria.
- Em arquivamentos concorrentes, a primeira confirmação preserva motivo e responsável. Em operações opostas concorrentes, a operação perdedora retorna conflito e não sobrescreve o estado confirmado.
- O motivo de arquivamento é obrigatório e não pode conter apenas espaços.
- A restauração não exige motivo.
- Arquivamento não altera presenças, picks, times ou relações históricas; somente drafts ativos têm o status alterado para `Cancelada`.
- O cancelamento decorrente do arquivamento deve ser publicado no Discord. Indisponibilidade da integração não desfaz o arquivamento e mantém a publicação disponível para nova tentativa.
- Draft arquivado não participa da listagem padrão nem de fluxos operacionais, exceto enquanto houver entrega pendente do cancelamento no Discord.
- Acesso direto por identificador continua disponível para `Admin` e `SuperAdmin`; demais usuários recebem o mesmo comportamento de recurso não encontrado usado para itens inacessíveis.

## Autorização

O backend é a fonte de verdade. Uma policy específica, separada de `CanManageDrafts`, exige role `Admin` ou `SuperAdmin`. Isso evita conceder a ação ao `Moderador`, que atualmente também possui `CanManageDrafts`.

O frontend usa a permissão retornada pelo backend apenas para exibir controles e filtros. Requisições sem a role necessária continuam bloqueadas no endpoint.

## API e Aplicação

Endpoints administrativos:

- `PATCH /api/v1/draft-montagens/{id}/arquivar`
  - Corpo: `{ "motivo": "...", "versaoEstado": 0 }`.
  - Retorna resultado reduzido com ID, status, estado de arquivamento e nova versão; o cliente recarrega lista ou detalhe quando necessário.
- `PATCH /api/v1/draft-montagens/{id}/restaurar`
  - Corpo: `{ "versaoEstado": 0 }`.
  - Retorna resultado reduzido com ID, status, estado de arquivamento e nova versão; o cliente recarrega lista ou detalhe.
- `GET /api/v1/draft-montagens/{id}/arquivamento`
  - Retorna detalhe, metadados atuais e histórico somente para `Admin` e `SuperAdmin`.

A listagem recebe `includeArchived=false` por padrão. O filtro `includeArchived=true` é aceito apenas para `Admin` e `SuperAdmin`; para os demais usuários autenticados, a tentativa retorna `403 Forbidden` em vez de ignorar silenciosamente o parâmetro.

A projeção administrativa por identificador continua sendo o ponto de consulta do histórico e passa a incluir os metadados atuais de arquivamento. Ela exige a mesma policy exclusiva de `Admin` e `SuperAdmin`; projeções públicas e respostas para demais papéis não expõem motivo, responsável ou ações administrativas.

Commands e queries permanecem separados. Handlers validam entrada, identidade, versão e coordenam uma única persistência. O agregado executa cancelamento, arquivamento, ações administrativas de `CancelamentoPorArquivamento` e `Arquivamento` e criação da intenção de publicação como uma transição indivisível. O envio ao Discord ocorre somente após essa confirmação; se a persistência falhar, nenhum desses efeitos permanece e nenhuma mensagem é enviada. Controllers apenas recebem a requisição, aplicam policy, enviam o command/query e retornam a resposta.

O parâmetro `includeArchived=true` exige autorização condicional por `CanArchiveDrafts`; a listagem normal continua disponível aos demais usuários autenticados. Republicar `Cancelamento` de draft arquivado usa endpoint separado com a mesma policy, enquanto Moderadores preservam o endpoint de republicação dos tipos operacionais de drafts visíveis. Nenhuma policy ASP.NET é avaliada dentro da camada Application.

Uma mensagem Discord cujo envio externo começou antes do commit do arquivamento não pode ser desfeita atomicamente. Claims antigos são invalidados, o bot revalida antes de enviar, conclusões operacionais são recusadas e o cancelamento posterior atua como compensação. Manter lock ou transação durante I/O externo foi rejeitado por risco de indisponibilidade.

## Interface

- A listagem padrão não mostra drafts arquivados.
- `Admin` e `SuperAdmin` veem um filtro para incluir arquivados.
- Drafts arquivados recebem badge localizado de arquivado.
- Em qualquer draft não arquivado, `Admin` e `SuperAdmin` veem a ação de arquivar.
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
- Motivo vazio: erro de validação localizado.
- Usuário sem role adequada: `403 Forbidden` pela policy.
- Repetição para o mesmo estado: retorna o estado atual sem duplicar auditoria.
- Arquivamento concorrente: a primeira confirmação preserva motivo e responsável; solicitações posteriores retornam o estado já arquivado.
- Arquivamento e restauração concorrentes: a operação perdedora retorna conflito estruturado, sem sobrescrever a vencedora nem duplicar auditoria.

Mensagens do backend usam resources em português e inglês com `messageCode` estável. O frontend mapeia códigos para textos localizados e não exibe detalhes técnicos.

## Testes

### Domínio

- Arquiva draft cancelado e finalizado sem alterar o status.
- Cancela e arquiva draft em cada estado não terminal.
- Exige motivo não vazio.
- Aceita motivo com 500 caracteres, rejeita 501 e remove espaços das extremidades antes da validação.
- Arquivamento e restauração são idempotentes.
- Restauração preserva status e histórico administrativo.
- Draft ativo restaurado permanece cancelado e não retoma prazos ou turnos.
- Falha antes da persistência não deixa cancelamento, arquivamento, auditoria ou publicação parciais.

### Aplicação e Integração

- `Admin` e `SuperAdmin` podem arquivar/restaurar.
- `Moderador` e demais roles recebem `403`.
- Usuário não autenticado recebe `401`; autoria sempre deriva da identidade autenticada, nunca do corpo da requisição.
- Listagem padrão exclui arquivados.
- Filtro administrativo inclui arquivados.
- Filtro administrativo solicitado sem Admin+ recebe `403`.
- Acesso direto respeita visibilidade por role.
- Concorrência não cria ações administrativas duplicadas.
- Arquivamentos concorrentes preservam motivo e responsável da primeira confirmação.
- Arquivamento e restauração concorrentes retornam conflito para a operação perdedora sem sobrescrever a vencedora.
- Falha do Discord não desfaz o arquivamento e mantém o cancelamento publicável.
- Metadados e histórico de arquivamento não vazam em projeções públicas, contagens ou atualizações em tempo real.
- `Admin` e `SuperAdmin` consultam metadados e histórico pela projeção administrativa; `Moderador` e demais usuários autenticados recebem `403`, e não autenticados recebem `401`.

### Frontend

- Controles aparecem apenas com a permissão específica.
- Motivo é obrigatório no arquivamento.
- Arquivar remove o item da lista padrão.
- Filtro exibe arquivados e permite restaurar.
- Chaves `pt.json` e `en.json` permanecem sincronizadas.
- Mensagens do backend existem em português e inglês, códigos são estáveis e nenhuma chave técnica fica visível.
- Acentuação portuguesa e textos renderizados de filtros, badges, confirmações, erros e notificações são revisados.

## Critérios de Aceite

- Um `Admin` ou `SuperAdmin` arquiva qualquer draft informando motivo e ele desaparece da lista padrão.
- Se o draft estava ativo, ele é cancelado, deixa de participar de fluxos operacionais e o cancelamento é destinado ao Discord.
- O mesmo usuário inclui arquivados no filtro, encontra o draft e o restaura.
- O draft restaurado volta à lista com o mesmo status, presenças, times, picks e publicações.
- Um draft que estava ativo antes do arquivamento volta como cancelado e não retoma o fluxo anterior.
- `Moderador` e demais roles não conseguem executar nem visualizar as ações administrativas.
- Nenhum registro de draft é excluído fisicamente.
