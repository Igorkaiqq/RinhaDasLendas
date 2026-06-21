# Implementation Plan: Usuários, Autenticação, Autorização e RBAC

**Branch**: `feature/009-usuarios-auth-rbac` | **Date**: 2026-06-20 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/009-usuarios-auth-rbac/spec.md`

## Summary

Implementar autenticação local, sessão renovável, RBAC hierárquico, gerenciamento administrativo de usuários, vínculo entre usuário e jogador, auditoria de ações sensíveis e preparação futura para Discord OAuth2.

A abordagem recomendada é usar ASP.NET Core Identity com usuário customizado por UUID para autenticação/senhas/roles, JWT curto para chamadas da API, refresh token rotacionável em cookie HttpOnly, policies para autorização e regras hierárquicas testáveis na camada de aplicação/domínio. Discord fica modelado como vínculo futuro, sem OAuth real nesta etapa.

## Technical Context

**Language/Version**: .NET 10 no backend; Vue 3 com TypeScript no frontend.

**Primary Dependencies**: ASP.NET Core Web API, ASP.NET Core Identity, JWT Bearer Authentication, Entity Framework Core, PostgreSQL, FluentValidation, MediatR, Vue Router, Axios, Vitest.

**Storage**: PostgreSQL relacional via EF Core migrations, UUIDs, snake_case, FKs explícitas e índices para buscas/constraints de segurança.

**Testing**: xUnit, FluentAssertions e Moq no backend; Vitest e Vue Test Utils no frontend.

**Target Platform**: Aplicação web interna com API HTTP e SPA Vue, executável no ambiente atual do projeto.

**Project Type**: Aplicação web com backend API e frontend SPA.

**Performance Goals**: Login percebido como imediato em uso interno; listagem administrativa deve permitir localizar usuário por nome/e-mail em menos de 30 segundos com até 100 usuários; refresh automático deve ser transparente para o usuário.

**Constraints**: Funcionar sem Discord; não expor senha/token em logs/respostas; controllers sem regra de negócio; autorização validada no backend; frontend apenas melhora UX ocultando ações; preservar jogadores legados sem usuário vinculado.

**Scale/Scope**: Uso interno do grupo, dezenas a poucas centenas de usuários, múltiplas sessões por usuário, auditoria administrativa simples e extensível.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- MVP primeiro: PASS. Login local e RBAC funcionam sem Discord ou Riot.
- Uso interno: PASS. Escopo evita complexidade de aplicação pública de larga escala, mas mantém segurança básica.
- Simplicidade de uso: PASS. Cadastro, login, perfil e gerenciamento são fluxos diretos.
- Regras de jogo claras: PASS. Role `Capitão` explícita controla elegibilidade de capitães em drafts.
- Integrações não travam: PASS. Discord é preparado como vínculo futuro, não obrigatório.
- Arquitetura e qualidade: PASS. Regras críticas ficam fora de controllers e UI; CQRS/MediatR/validators são mantidos.
- Regras de domínio e evolução: PASS. Usuário e jogador ficam separados, permitindo evolução de autenticação e jogo independentemente.

## Project Structure

### Documentation (this feature)

```text
specs/009-usuarios-auth-rbac/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── auth-api.md
│   └── usuarios-api.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
BackEnd/
├── src/RinhaDasLendas.Api/
│   ├── Controllers/AuthController.cs
│   ├── Controllers/UsuariosController.cs
│   ├── Authorization/
│   └── Filters/ApiExceptionMiddleware.cs
├── src/RinhaDasLendas.Application/
│   ├── Commands/Auth/
│   ├── Commands/Usuarios/
│   ├── Queries/Auth/
│   ├── Queries/Usuarios/
│   ├── Handlers/Auth/
│   ├── Handlers/Usuarios/
│   ├── Dtos/
│   ├── Interfaces/
│   └── Validators/
├── src/RinhaDasLendas.Domain/
│   ├── Constants/
│   ├── Enums/
│   ├── Services/
│   └── Entities/Jogador.cs
├── src/RinhaDasLendas.Infrastructure/
│   ├── Identity/
│   ├── Persistence/RinhaDasLendasDbContext.cs
│   ├── Repositories/
│   └── Migrations/
└── tests/RinhaDasLendas.Tests/
    ├── Auth/
    ├── Usuarios/
    ├── Authorization/
    └── Integration/

FrontEnd/
├── src/router/
├── src/services/api.ts
├── src/services/auth.ts
├── src/services/users.ts
├── src/views/
├── src/components/users/
├── src/components/layout/
├── src/constants/
└── src/types/
```

**Structure Decision**: Usar exclusivamente `BackEnd/` e `FrontEnd/`, preservando camadas existentes e adicionando autenticação/autorização como evolução da arquitetura atual. Não criar pastas raiz alternativas.

## Complexity Tracking

Nenhuma violação constitucional identificada. ASP.NET Core Identity adiciona complexidade, mas reduz risco de implementar autenticação, hashing, lockout, reset token e roles manualmente.

## Phase 0: Research

See [research.md](./research.md).

## Phase 1: Design

See [data-model.md](./data-model.md), [contracts/auth-api.md](./contracts/auth-api.md), [contracts/usuarios-api.md](./contracts/usuarios-api.md), and [quickstart.md](./quickstart.md).

## Post-Design Constitution Check

- MVP primeiro: PASS. O fluxo local é completo e Discord permanece futuro.
- Uso interno: PASS. O modelo cobre o grupo atual e evita permissões granulares excessivas.
- Simplicidade de uso: PASS. A UI mantém login/cadastro/perfil/admin direto.
- Regras de jogo claras: PASS. Capitão exige role explícita e jogador ativo.
- Integrações não travam: PASS. Vínculo Discord é opcional e modelado para evolução.
- Arquitetura e qualidade: PASS. Controllers chamam casos de uso; regras críticas têm serviços/testes; frontend não concentra autorização.
- Regras de domínio e evolução: PASS. Usuários, jogadores, sessões, auditoria e Discord têm responsabilidades separadas.
