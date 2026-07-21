# Tasks: Melhorias Drafts, Presenca e Discord

## Backend

- [ ] Adicionar testes de dominio para presenca manual em `DraftMontagem`.
- [ ] Implementar metodos de dominio para adicionar/remover presenca manual.
- [ ] Criar DTOs, commands, handlers e validators de presenca manual.
- [ ] Expor endpoints admin em `DraftMontagensController` com `CanManageDrafts`.
- [ ] Ajustar query/listagem para ocultar cancelados por padrao e permitir filtro explicito.
- [ ] Adicionar `DataRinha` ao resumo de draft montagem.
- [ ] Atualizar resources `.resx` para mensagens novas.
- [ ] Adicionar/ajustar testes de Application/Domain.

## Discord Bot

- [ ] Adicionar teste para horario de Brasilia em `parsePresenceClosingTime`.
- [ ] Corrigir conversao de horario do bot sem quebrar validacao de data.
- [ ] Validar build do bot.

## Frontend

- [ ] Atualizar tipos e servico `draftMontagens` para filtros e presenca manual.
- [ ] Mostrar data da rinha na lista de drafts.
- [ ] Ocultar cancelados por padrao e permitir filtro por status.
- [ ] Adicionar seletor ADM+ para adicionar jogador a presenca aberta.
- [ ] Adicionar acao ADM+ para remover jogador de presenca aberta.
- [ ] Atualizar `pt.json` e `en.json` mantendo sincronizacao.
- [ ] Validar que textos novos nao estao hardcoded.

## Verification

- [ ] Executar testes backend.
- [ ] Executar build backend.
- [ ] Executar testes frontend.
- [ ] Executar build frontend.
- [ ] Executar testes/build bot.
- [ ] Auditar internacionalizacao.
