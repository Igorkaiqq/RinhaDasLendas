# Implementation Plan: Corrigir internacionalização de textos

**Branch**: `feature/010-internacionalizacao-textos`
**Spec**: `specs/011-corrigir-internacionalizacao/spec.md`

## Technical Context

Front-end usa Vue 3, TypeScript e `vue-i18n`. Back-end usa .NET, FluentValidation e resources `.resx` com `IMessageProvider`.

## Constitution Check

- MVP simples preservado: sem novas regras de negócio.
- Separação backend/frontend/domínio preservada.
- Mensagens de usuário claras e localizadas.
- Sem dependência de integração externa.

## Implementation Strategy

1. Migrar locales do front-end para `pt.json` e `en.json`.
2. Expandir chaves i18n necessárias e substituir textos visíveis hardcoded.
3. Usar `IMessageProvider` em controllers/middlewares/validators/serviços com mensagens de API.
4. Adicionar resources faltantes em português e inglês.
5. Executar auditorias de texto, build e testes viáveis.

## Risks

- O grep bruto pode apontar falsos positivos técnicos; a auditoria final deve distinguir texto visível de strings internas.
- A substituição de mensagens de domínio por códigos exige cuidado para não acoplar domínio à infraestrutura.

## No New Architecture

Nenhuma nova biblioteca ou camada será adicionada. A correção reutiliza `vue-i18n` e `IMessageProvider` existentes.
