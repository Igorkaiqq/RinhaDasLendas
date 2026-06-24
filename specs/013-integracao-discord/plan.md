# Implementation Plan: Integração Discord

**Branch**: `feature/013-integracao-discord` | **Date**: 2026-06-24 | **Spec**: [spec.md](./spec.md)

## Summary

Implementar suporte a OAuth2 Discord como camada externa opcional sobre a autenticação existente. O backend continuará sendo a fonte oficial de identidade, emitindo JWT e refresh token pelo mecanismo atual. Quando o login Discord não encontrar vínculo, o backend criará usuário interno com role `Jogador` e vínculo externo. O frontend adicionará entrada com Discord e uma área de integrações para vincular/desvincular conta.

## Technical Context

**Backend**: .NET 10, ASP.NET Core Web API, EF Core, PostgreSQL, Identity, JWT, MediatR, FluentValidation.

**Frontend**: Vue 3, TypeScript, Composition API, Axios, Vue Router, vue-i18n.

**Storage**: PostgreSQL com migrations EF Core, UUID, snake_case e índices únicos filtrados.

**Constraints**: Não recriar autenticação; criação via Discord deve usar `ApplicationUser`/Identity existente; não modificar roles/autorização; não quebrar Draft.

## Constitution Check

- **MVP Primeiro**: PASS. Escopo limitado a login/vínculo/desvínculo.
- **Uso Interno**: PASS. Fluxo adequado para comunidade interna.
- **Simplicidade de Uso**: PASS. Botões explícitos no login e integrações.
- **Integrações Não Devem Travar**: PASS. Login tradicional permanece independente.
- **Arquitetura e Qualidade**: PASS. OAuth em Infrastructure, casos de uso via Application/CQRS, controllers apenas orquestram.
- **Internacionalização**: PASS. Todas as mensagens novas por resources/i18n.

## Structure Decision

Usar a estrutura existente `BackEnd/` e `FrontEnd/`. Criar entidade genérica `ExternalAccount` em `Infrastructure/Identity` por estar ligada ao Identity atual, mantendo domínio independente de provedores externos. Criar tabela de state OAuth em Infrastructure por ser detalhe de autenticação externa.

## Implementation Phases

1. Persistência: `external_accounts`, `external_auth_states` e migration.
2. Serviços Discord: geração de autorização, callback, token exchange e consulta `/users/@me`.
3. CQRS/endpoints: start login, start link, callback, status e unlink.
4. Frontend: login com Discord e tela de configurações/integrações.
5. Testes, build e auditoria i18n.
