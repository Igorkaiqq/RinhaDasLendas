# Pull Request Standards

Pull requests devem ser pequenos o suficiente para revisão objetiva e grandes o suficiente para entregar uma história ou checkpoint validável.

## Título

Use português e prefixo semântico quando possível:

```text
docs: documentar padrões de mensagens
feat: adicionar infraestrutura de mensagens backend
fix: corrigir validação de rotas
```

## Descrição mínima

Cada PR deve informar:

- Feature ou spec relacionada, por exemplo `specs/005-standards-and-i18n`.
- Tasks concluídas, com IDs quando existirem.
- Tipo de mudança: docs, backend, frontend ou combinação.
- Validações executadas.
- Riscos, limitações ou pendências conhecidas.

## Critérios de revisão

- Respeita Constituição e AGENTS.md.
- Não implementa feature fora da branch `feature/NNN-slug`.
- Não mistura regra de negócio em controller ou componente visual.
- Não adiciona texto visível ao usuário sem catálogo ou tradução planejada.
- Não cria novos tokens de design sem aprovação.
- Mantém compatibilidade de API quando o plano exige transição gradual.

## Checklist antes de abrir PR

- `tasks.md` atualizado com `[X]` nas tasks concluídas.
- Documentação alterada quando a decisão for significativa.
- Testes e builds executados conforme área alterada.
- `git status --short` revisado.
- Commit messages em português.
