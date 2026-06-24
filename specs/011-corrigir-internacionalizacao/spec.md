# Feature Specification: Corrigir internacionalização de textos

**Feature Branch**: `feature/010-internacionalizacao-textos`
**Created**: 2026-06-24
**Status**: Approved
**Input**: Corrigir textos hardcoded no front-end e back-end conforme regra obrigatória de internacionalização.

## User Scenarios & Testing

### User Story 1 - Textos do front-end localizados (Priority: P1)

Como usuário, quero que labels, títulos, botões, placeholders, badges, toasts, estados vazios e erros visuais sejam exibidos por chaves i18n para permitir alternância entre português e inglês.

**Independent Test**: Alternar locale entre `pt` e `en` e verificar telas principais sem texto visível hardcoded.

### User Story 2 - Mensagens da API localizadas (Priority: P1)

Como consumidor da API, quero que erros e validações retornem mensagens vindas dos resources do back-end, sem strings hardcoded.

**Independent Test**: Acionar respostas 400/401/404/409/500 conhecidas e confirmar que usam `IMessageProvider`/resources.

### User Story 3 - Locales sincronizados (Priority: P1)

Como mantenedor, quero que toda chave em português tenha equivalente em inglês e que os arquivos usem os nomes `pt.json` e `en.json`.

**Independent Test**: Comparar estruturalmente os dois JSONs e executar testes de i18n.

## Requirements

- **FR-001**: O front-end MUST usar `FrontEnd/src/i18n/locales/pt.json` e `FrontEnd/src/i18n/locales/en.json`.
- **FR-002**: Todo texto visível novo ou corrigido em componentes Vue MUST usar `t(...)`.
- **FR-003**: Placeholders, aria-labels, botões, títulos, badges, toasts e mensagens vazias MUST usar chaves i18n.
- **FR-004**: Mensagens de erro/resposta do back-end MUST vir de `.resx` ou estrutura equivalente.
- **FR-005**: Validators e middlewares MUST usar resources ou códigos de mensagem localizáveis.
- **FR-006**: Português MUST conter acentuação correta.

## Assumptions

- Strings técnicas como imports, nomes de eventos, rotas HTTP, nomes de colunas e valores de enum persistidos não são textos visíveis ao usuário.
- Correções funcionais devem ser mínimas e não alterar regras de negócio.
