# Branch Naming Convention Contract

## Overview

All feature development follows a consistent branch naming convention to keep Git history, specs, plans, tasks, implementation, and pull requests traceable.

## Branch Naming Pattern

**Standard Format**: `feature/[NNN]-[slug]`

**Components**:

- **Prefix**: `feature/` is mandatory.
- **Number**: `NNN` is a zero-padded 3-digit sequence.
- **Slug**: lowercase kebab-case, usually 2-5 words.

## Examples

| Number | Feature Description | Branch Name |
|--------|---------------------|-------------|
| 001 | Initial project structure | `feature/001-estrutura-inicial` |
| 002 | MVP Rinha das Lendas | `feature/002-mvp-rinha-das-lendas` |
| 003 | Player registration and routes | `feature/003-cadastro-jogadores-rotas` |
| 004 | Player page layout | `feature/004-layout-jogadores` |
| 005 | Standardization and i18n | `feature/005-standards-and-i18n` |

## Guidelines

1. Determine the next number by checking `specs/` and existing feature branches.
2. Keep the branch number and spec directory number identical.
3. Use `feature/` for all feature work in the current workflow.
4. Treat bug fixes and hotfixes as numbered features unless project governance explicitly changes this standard.
5. Delete feature branches after merge to `main`.

## Invalid Examples

| Branch | Reason |
|--------|--------|
| `004-layout-jogadores` | Missing `feature/` prefix |
| `feature/5-i18n` | Number is not zero-padded |
| `feature/005_i18n` | Uses underscore |
| `feature/005-I18N` | Uses uppercase |

## Related Documentation

- Commit message format: `docs/standards/COMMIT_MESSAGES.md`
- Spec Kit workflow: `docs/standards/SPECS_AND_PLANNING.md`
- Pull request standards: `docs/standards/PR_STANDARDS.md`
- Agent instructions: `AGENTS.md`

## Alignment with Spec Kit

The branch `feature/005-standards-and-i18n` maps to `specs/005-standards-and-i18n/`. That 1:1 mapping is required for traceability.
