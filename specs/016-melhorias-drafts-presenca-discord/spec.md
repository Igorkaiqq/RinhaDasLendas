# Feature Specification: Melhorias Drafts, Presenca e Discord

**Branch**: `feature/016-melhorias-drafts-presenca-discord`
**Date**: 2026-07-10

## Summary

Corrigir problemas operacionais no fluxo de lista de presenca, draft visual e bot Discord. A feature deve reduzir drafts antigos na visualizacao padrao, corrigir horario criado pelo bot, permitir gestao manual de presencas por ADM+ e deixar claro o caminho apos encerramento da presenca.

## User Stories

### Horario correto no Discord

Como administrador que cria draft pelo Discord, quero informar o horario de Brasilia e ver o mesmo horario no bot/site, para evitar confusao entre 19h30 e 22h30.

### Lista de drafts menos poluida

Como usuario do site, quero ver drafts recentes e ativos primeiro, sem drafts cancelados antigos poluindo a lista padrao.

### Inativacao logica de draft

Como administrador, quero cancelar/inativar drafts antigos sem deletar o registro do banco, para manter historico auditavel.

### Lista de presenca mais simples

Como usuario, quero que a lista destaque a data da rinha em vez de informacoes redundantes, para entender rapidamente qual evento estou vendo.

### Presenca manual por ADM+

Como ADM+, quero adicionar ou remover jogadores cadastrados da lista de presenca pelo site, para corrigir esquecimentos ou problemas do Discord.

### Encerramento acionavel

Como administrador, quando a presenca encerrar automaticamente ou manualmente, quero ver a proxima acao disponivel para montar o draft, para nao ficar preso sem escolher jogadores.

## Requirements

- O bot Discord deve interpretar entrada de data e hora como horario de Brasilia.
- Drafts cancelados devem permanecer no banco e ficar ocultos da listagem padrao.
- A listagem padrao deve priorizar drafts mais recentes pela data da rinha quando disponivel.
- Admins devem conseguir filtrar/visualizar drafts cancelados quando necessario.
- A lista lateral de drafts deve exibir a data da rinha quando houver `horarioEncerramentoPresenca`.
- ADM+ deve conseguir adicionar jogador cadastrado e ativo a uma presenca aberta.
- ADM+ deve conseguir remover jogador de uma presenca aberta.
- Presenca manual nao deve duplicar jogador ja confirmado.
- Acoes administrativas de presenca devem ser protegidas por policy backend `CanManageDrafts`.
- O encerramento da presenca deve deixar a montagem em estado consistente para definir/sortear capitaes e seguir para ordem de escolha.
- Todo texto frontend novo deve usar i18n em `pt.json` e `en.json`.
- Toda mensagem backend nova deve usar resources `.resx`.
- Nenhum segredo ou arquivo `.env` deve ser alterado.

## Success Criteria

- Criar draft no Discord para 19:30 Brasilia persiste horario equivalente correto em UTC e nao exibe 22:30 indevidamente.
- Drafts cancelados nao aparecem na listagem padrao, mas aparecem ao filtrar por status cancelado.
- Admin adiciona jogador ativo cadastrado a presenca aberta e a lista atualiza.
- Admin remove jogador de presenca aberta e a lista atualiza.
- Usuario comum nao consegue usar endpoints de presenca manual.
- Presenca encerrada com jogadores suficientes mostra a etapa de capitaes/ordem no site.
- Builds e testes relevantes de backend, frontend e bot passam.

## Non-Goals

- Nao deletar fisicamente drafts do banco.
- Nao criar comandos Discord para adicionar/remover presenca manual nesta rodada.
- Nao automatizar escolha de jogadores sem regra explicita aprovada.
- Nao modificar segredos, `.env` local ou configuracao de deploy.
