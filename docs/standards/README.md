# Standards

Este diretório centraliza os padrões de desenvolvimento do RinhaDasLendas. Ele complementa o `AGENTS.md`, a Constituição em `.specify/memory/constitution.md` e os documentos de arquitetura em `docs/architecture/`.

## Leitura rápida

| Tema | Documento | Quando usar |
|------|-----------|-------------|
| Branches | [BRANCH_NAMING.md](BRANCH_NAMING.md) | Antes de iniciar qualquer feature |
| Commits | [COMMIT_MESSAGES.md](COMMIT_MESSAGES.md) | Antes de criar commits |
| Spec Kit | [SPECS_AND_PLANNING.md](SPECS_AND_PLANNING.md) | Durante Specify, Plan, Tasks e Implement |
| Pull Requests | [PR_STANDARDS.md](PR_STANDARDS.md) | Antes de abrir ou revisar PR |
| Constantes e enums | [CONSTANTS_AND_ENUMS.md](CONSTANTS_AND_ENUMS.md) | Ao adicionar valores fechados ou evitar magic strings |
| i18n | [I18N_GUIDELINES.md](I18N_GUIDELINES.md) | Ao adicionar texto visível ao usuário |

## Ordem obrigatória de trabalho

1. Ler a Constituição.
2. Criar ou atualizar a especificação.
3. Criar ou atualizar o plano.
4. Gerar e aprovar tasks.
5. Implementar somente tasks aprovadas.

## Conformidade arquitetural

- Backend deve permanecer em `BackEnd/`, separado em Api, Application, Domain, Infrastructure e Tests.
- Frontend deve permanecer em `FrontEnd/`, usando Vue 3, TypeScript e Composition API.
- Regras de negócio pertencem ao backend/domain, não a controllers ou componentes visuais.
- Mensagens de usuário devem usar catálogo central em `docs/messages/` antes de entrarem em código.
- Valores fechados devem ser centralizados como constantes, enums ou tipos nos locais definidos pelo plano.
- A UI deve seguir `docs/design/DESIGN_SYSTEM.md`, `docs/design/DESIGN_TOKENS.md` e `docs/design/UI_GUIDELINES.md`.

## Fontes de verdade relacionadas

- [AGENTS.md](../../AGENTS.md)
- [DDD Guidelines](../architecture/DDD_GUIDELINES.md)
- [API Standards](../architecture/API_STANDARDS.md)
- [Design Patterns](../architecture/DESIGN_PATTERNS.md)
- [Database Guidelines](../architecture/DATABASE_GUIDELINES.md)
- [Message Standards](../messages/README.md)
