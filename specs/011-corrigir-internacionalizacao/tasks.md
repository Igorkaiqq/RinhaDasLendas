# Tasks: Corrigir internacionalização de textos

**Input**: `spec.md`, `plan.md`
**Status**: Approved by direct user request on 2026-06-24: “Bora lá corrigir isso agora no código!”

## Phase 1 - Setup

- [X] T001 Migrar locale files para `FrontEnd/src/i18n/locales/pt.json` e `FrontEnd/src/i18n/locales/en.json`.
- [X] T002 Atualizar tipos, serviço i18n e testes para locale codes `pt` e `en`.

## Phase 2 - Front-end

- [X] T003 Substituir textos visíveis hardcoded em views por `t(...)`.
- [X] T004 Substituir textos visíveis hardcoded em componentes de layout, jogadores, times, usuários e drafts por `t(...)`.
- [X] T005 Adicionar chaves equivalentes em `pt.json` e `en.json`.

## Phase 3 - Back-end

- [X] T006 Adicionar códigos/resources faltantes em português e inglês.
- [X] T007 Substituir mensagens hardcoded em controllers e middleware por `IMessageProvider`.
- [X] T008 Substituir mensagens hardcoded em validators, handlers e serviços por resources.

## Phase 4 - Validation

- [X] T009 Executar auditoria de textos hardcoded e acentuação.
- [X] T010 Executar testes/builds relevantes de front-end e back-end.
