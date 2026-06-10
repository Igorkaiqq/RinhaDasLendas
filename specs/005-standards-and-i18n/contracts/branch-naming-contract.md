# Branch Naming Convention Contract

## Overview

All feature development follows a consistent branch naming convention to ensure clarity, organization, and traceability within the Git repository.

## Branch Naming Pattern

**Standard Format**: `feature/[NNN]-[slug]`

**Components**:
- **Prefix**: `feature/` (mandatory; all feature branches use this prefix)
- **Number**: `NNN` (3-digit sequential number, zero-padded; e.g., `001`, `042`, `005`)
- **Slug**: `[slug]` (kebab-case, lowercase, hyphens for word separation; 2-5 words describing feature)

## Examples

| Number | Feature Description | Branch Name |
|--------|---------------------|-------------|
| 001 | Initial project structure | `feature/001-estrutura-inicial` |
| 002 | MVP Rinha das Lendas | `feature/002-mvp-rinha-das-lendas` |
| 003 | Player registration and routes | `feature/003-cadastro-jogadores-rotas` |
| 004 | Player page layout | `feature/004-layout-jogadores` |
| 005 | Standardization and i18n | `feature/005-standards-and-i18n` |

## Guidelines

1. **Numbering**: Sequential starting from 001; next available number determined by examining existing feature branches in `specs/` directory
2. **Slug Guidelines**:
   - Use hyphens to separate words (kebab-case)
   - Avoid special characters, underscores, or uppercase
   - Keep slug concise but descriptive (2-5 words typical)
   - Reflect the primary user value, not implementation details
3. **No Other Branch Prefixes**: Do not use `bugfix/`, `hotfix/`, `develop/`, `release/` on main branch
   - Bugfixes and hotfixes are treated as features with their own branch numbers
4. **Branch Lifespan**: Branch deleted after PR merge to main

## Related Documentation

- Commit message format: [COMMIT_MESSAGES.md](../docs/standards/COMMIT_MESSAGES.md)
- Spec Kit workflow: [AGENTS.md](../AGENTS.md)
- PR standards: [PR_STANDARDS.md](../docs/standards/PR_STANDARDS.md)

## Alignment with Spec Kit

Each feature branch corresponds to a feature number (e.g., `005` in `feature/005-standards-and-i18n`), which also becomes the spec directory name (`specs/005-standards-and-i18n`). This creates a clear 1:1 relationship between branches, specs, and implementation.
