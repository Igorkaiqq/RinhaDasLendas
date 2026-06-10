# Branch Naming

Todas as features devem usar o padrão:

```text
feature/NNN-slug
```

## Componentes

| Parte | Regra | Exemplo |
|-------|-------|---------|
| Prefixo | Sempre `feature/` | `feature/` |
| Número | Sequencial com 3 dígitos | `005` |
| Slug | Kebab-case, minúsculo e descritivo | `standards-and-i18n` |

## Exemplos válidos

| Feature | Branch |
|---------|--------|
| Cadastro de jogadores e rotas | `feature/003-cadastro-jogadores-rotas` |
| Layout de jogadores | `feature/004-layout-jogadores` |
| Padrões e i18n | `feature/005-standards-and-i18n` |

## Exemplos inválidos

| Branch | Problema |
|--------|----------|
| `004-layout-jogadores` | Falta prefixo `feature/` |
| `feature/5-i18n` | Número não tem 3 dígitos |
| `feature/005_Standards` | Usa underscore e maiúsculas |
| `hotfix/005-ajuste` | Prefixo não aprovado para fluxo atual |

## Como escolher o próximo número

1. Verifique os diretórios existentes em `specs/`.
2. Use o próximo número sequencial disponível.
3. Mantenha o mesmo número no diretório da spec e na branch.

## Relação com Spec Kit

A branch `feature/005-standards-and-i18n` corresponde ao diretório `specs/005-standards-and-i18n/`. Essa relação facilita rastreabilidade entre especificação, plano, tasks, commits e PR.
