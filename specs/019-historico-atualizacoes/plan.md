# Implementation Plan: Histórico de Atualizações

**Branch**: `feature/019-historico-atualizacoes` | **Date**: 2026-07-22 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/019-historico-atualizacoes/spec.md` and approved design from `/docs/superpowers/specs/2026-07-22-historico-atualizacoes-design.md`

## Summary

Criar uma página autenticada e responsiva em `/atualizacoes` que apresenta oito releases editoriais compiladas, permite consultar detalhes, pesquisar conteúdo localizado, combinar filtros por categoria e perceber uma release ainda não visualizada por um badge local. O conteúdo estrutural ficará em um registro TypeScript imutável, os textos ficarão nos catálogos de i18n, e operações puras concentrarão ordenação, busca, validação de links e acesso seguro ao estado visualizado. A feature não adiciona backend, banco, endpoint ou geração baseada em commits.

## Technical Context

**Language/Version**: Vue 3.5 + TypeScript 5.9

**Primary Dependencies**: Vue Router, Vue I18n

**Storage**: registro compilado e `localStorage`; sem backend

**Testing**: Vitest, Vue Test Utils e happy-dom

**Target Platform**: navegadores desktop e mobile suportados pela aplicação web existente

**Project Type**: aplicação web frontend dentro do monorepositório existente

**Performance Goals**: filtrar e renderizar o catálogo inicial de oito releases sem atraso perceptível; nenhuma requisição de rede adicional para carregar o histórico

**Constraints**: rota autenticada; conteúdo equivalente em português e inglês; operação completa por teclado e toque; sem overflow horizontal; uso exclusivo dos tokens e componentes existentes; falhas de `localStorage` não podem interromper a interface

**Scale/Scope**: oito marcos iniciais, 15 detalhes na release mais recente, cinco categorias fechadas, oito áreas tipadas e um guia de manutenção

## Constitution Check

*GATE: Must pass before implementation and be re-checked after the design represented by this plan.*

| Gate | Status | Evidence |
|------|--------|----------|
| Simplicidade | PASS | Um registro compilado, funções puras e componentes focados resolvem o problema sem API, banco, painel administrativo ou automação por commits. |
| Uso interno | PASS | A rota aproveita autenticação e shell existentes e atende o grupo sem introduzir arquitetura de escala pública. |
| Integrações não bloqueantes | PASS | O histórico funciona integralmente sem Discord, Riot, GitHub ou qualquer serviço externo. |
| Internacionalização | PASS | O registro contém somente chaves; todo texto visível, inclusive conteúdo editorial e nomes acessíveis, será mantido com paridade em `pt.json` e `en.json`. |
| Testabilidade | PASS | Contrato do registro, operações puras, storage, rota, navegação, componentes e fluxos da view têm validações automatizadas previstas antes da implementação. |
| Ausência de persistência desnecessária | PASS | Apenas a versão visualizada é local ao navegador, com fallback em memória; não há persistência de domínio nem mudança no backend. |

**Post-design re-check**: PASS. A separação entre tipos, registro, serviço, componentes e composição mantém responsabilidades pequenas, não desloca regra crítica para componente visual e não cria violação constitucional que exija justificativa.

## Design Decisions

### Conteúdo E Contrato

- Declarar categorias, áreas, links e releases em `FrontEnd/src/types/systemUpdate.ts`.
- Armazenar a coleção imutável em `FrontEnd/src/constants/systemUpdates.ts`, contendo apenas IDs, versões, datas, classificações, áreas, destaque, chaves i18n e links internos opcionais.
- Cadastrar os oito marcos aprovados e os 15 detalhes individualizados da release mais recente; não produzir uma entrada por commit.
- Usar versão editorial `AAAA.MM.N`, com sufixo mensal sequencial, e data ISO coerente com o histórico real.
- Manter títulos, resumos, descrições, categorias, áreas e rótulos exclusivamente em `FrontEnd/src/i18n/locales/pt.json` e `FrontEnd/src/i18n/locales/en.json`.

### Operações E Estado Local

- Concentrar em `FrontEnd/src/services/systemUpdates.ts` funções puras de ordenação, identificação da release mais recente, agrupamento temporal, normalização de texto, busca localizada, combinação de categorias e validação de links internos.
- Ler e gravar `rinha:last-seen-system-update` por uma fronteira protegida que captura indisponibilidade ou exceções de `localStorage` e preserva estado em memória durante a sessão.
- Derivar o badge da comparação entre a versão mais recente do registro e a versão visualizada; ao abrir a view, registrar a versão e refletir a remoção sem recarga.
- Recalcular resultados com o conteúdo do idioma ativo, incluindo título, resumo e detalhes, sem manter índice textual duplicado.

### Interface E Acessibilidade

- Registrar `AppRoutes.Updates` e `AppRouteNames.Updates`, criar a rota com `requiresAuth: true` e expor Atualizações na navegação desktop e mobile existente.
- Compor a página em `FrontEnd/src/views/SystemUpdatesView.vue`; manter o card editorial em `FrontEnd/src/components/updates/SystemUpdateCard.vue` e extrair outros componentes apenas se a composição deixar de ser pequena e legível.
- Reutilizar componentes e tokens documentados em `docs/design/DESIGN_SYSTEM.md`, `docs/design/DESIGN_TOKENS.md` e `docs/design/UI_GUIDELINES.md`; não criar novos tokens.
- Renderizar timeline como lista semântica, datas com `time`, detalhes com botões que exponham `aria-expanded`, filtros com estado selecionado e badge com texto localizado.
- Em desktop, permitir índice lateral quando houver espaço; em mobile, manter uma coluna, filtros roláveis horizontalmente, timeline à esquerda e cards na largura disponível.

### Validação E Manutenção

- Validar IDs e versões únicos, formato de versão, datas ISO, ordem decrescente, presença de categoria, área e detalhe, conjuntos fechados, chaves nos dois catálogos e links pertencentes a `AppRoutes`.
- Escrever cada teste de comportamento antes da implementação correspondente e confirmar a falha esperada antes de produzir o código.
- Documentar o fluxo editorial em `docs/guides/ATUALIZAR_HISTORICO.md` e incluir a revisão do histórico em `docs/standards/FEATURE_CHECKLIST.md`.
- Executar testes frontend, build, auditoria de i18n, revisão de acentuação e validação responsiva por desktop e mobile antes de concluir.

## Project Structure

### Documentation (this feature)

```text
specs/019-historico-atualizacoes/
├── spec.md
├── plan.md
└── tasks.md
```

### Source Code (repository root)

```text
FrontEnd/
├── src/
│   ├── components/
│   │   ├── layout/
│   │   │   ├── SidebarNav.vue
│   │   │   └── SidebarNav.spec.ts
│   │   └── updates/
│   │       ├── SystemUpdateCard.vue
│   │       └── SystemUpdateCard.spec.ts
│   ├── constants/
│   │   ├── appRoutes.ts
│   │   ├── appRoutes.spec.ts
│   │   ├── systemUpdates.ts
│   │   └── systemUpdates.spec.ts
│   ├── i18n/
│   │   ├── i18n.spec.ts
│   │   └── locales/
│   │       ├── pt.json
│   │       └── en.json
│   ├── router/
│   │   └── index.ts
│   ├── services/
│   │   ├── systemUpdates.ts
│   │   └── systemUpdates.spec.ts
│   ├── types/
│   │   └── systemUpdate.ts
│   └── views/
│       ├── SystemUpdatesView.vue
│       └── SystemUpdatesView.spec.ts
└── package.json

docs/
├── guides/
│   └── ATUALIZAR_HISTORICO.md
└── standards/
    └── FEATURE_CHECKLIST.md
```

**Structure Decision**: tipos em `types/`, dados em `constants/`, operações puras em `services/`, card em `components/updates/` e composição em `views/`. Rota e navegação ampliam os pontos existentes; textos ficam nos catálogos existentes, e documentação de manutenção fica sob `docs/`.

## Verification Strategy

1. Executar testes unitários do registro e do serviço para contrato, ordenação, busca, filtros, links e fallback do storage.
2. Executar testes de componentes para card, rota, navegação, badge, hero, timeline, filtros, expansão e estado vazio.
3. Executar os testes de i18n para paridade estrutural, existência de todas as chaves e ausência de texto editorial no registro.
4. Executar a suíte frontend completa e o build de produção.
5. Validar manualmente `/atualizacoes` autenticado em português e inglês, por mouse, teclado e toque, em viewport desktop e mobile.
6. Auditar textos hardcoded, acentuação portuguesa, placeholders, botões, títulos, badges, estados vazios e nomes acessíveis.

## Complexity Tracking

Nenhuma violação constitucional ou complexidade adicional a justificar.
