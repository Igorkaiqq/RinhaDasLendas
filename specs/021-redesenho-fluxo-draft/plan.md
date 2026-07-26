# Implementation Plan: Redesenho do Fluxo de Draft

**Branch**: `feature/021-redesenho-fluxo-draft` | **Date**: 2026-07-25 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/021-redesenho-fluxo-draft/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Reestruturar a tela de Draft como uma única área operacional consistente para presença, capitães, ordem, escolhas e finalização. `DraftsView.vue` permanece responsável por estado, permissões, serviços, atualização ao vivo e concorrência; componentes de apresentação recebem dados prontos e emitem intenções. O trabalho também corrige o progresso cancelado/desconhecido, elimina cabeçalhos e rolagens concorrentes, aplica responsividade de 320px em diante e publica a correção já entregue dos dias selecionados como `2026.07.3`.

## Technical Context

**Language/Version**: TypeScript 5.9, Vue 3.5

**Primary Dependencies**: Vue Composition API, Vue Router, Vue I18n, Reka UI, componentes UI locais, Lucide Vue, SignalR

**Storage**: N/A; nenhuma alteração de persistência ou contrato backend

**Testing**: Vitest 4 com Vue Test Utils e happy-dom; validação responsiva e acessível com Chromium via `agent-browser`

**Target Platform**: Navegadores modernos em desktop, tablet e mobile; produção em Nginx

**Project Type**: Aplicação web frontend dentro do monorepo existente

**Performance Goals**: Nenhuma chamada de rede adicional para renderizar o redesenho; filtros locais atualizam até o próximo frame com listas de 30 participantes

**Constraints**: Preservar contratos e regras atuais; nenhuma dependência ou token novo; uma região de rolagem vertical por vez; alvos de toque mínimos de 44px; viewports a partir de 320px; PT/EN sincronizados; movimento reduzido respeitado

**Scale/Scope**: Uma view operacional, quatro componentes existentes principais, até cinco componentes de apresentação novos, estilos responsivos, testes focados e uma entrada editorial

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Problema real do grupo**: PASS. A captura e a inspeção confirmam hierarquia, responsividade, semântica e rolagem inadequadas no fluxo principal.
- **MVP e simplicidade**: PASS. O plano preserva comportamento e integrações, sem criar fluxo paralelo, store ou abstração genérica.
- **Uso sem integração externa**: PASS. Operação manual existente permanece disponível e visualmente prioritária quando Discord está indisponível.
- **Separação de responsabilidades**: PASS. Regras, permissões e chamadas permanecem na orquestração atual; filhos são exclusivamente de apresentação.
- **Frontend responsivo**: PASS. Matriz inclui 1440px, 1024px, 768px e 320px.
- **Regras críticas testadas**: PASS. Presença, capitães, ordem, escolhas, finalização, cancelamento, concorrência e permissões têm cobertura planejada.
- **Internacionalização**: PASS. Todo conteúdo novo ou alterado será equivalente em PT/EN e a auditoria será ampliada para o fluxo de Draft.
- **Segurança**: PASS. Nenhuma credencial, dado sensível, permissão ou contrato de autorização será ampliado.

**Reavaliação após o design**: PASS. Os artefatos de pesquisa, modelo, contratos e validação mantêm os mesmos limites e não introduzem violação constitucional.

## Project Structure

### Documentation (this feature)

```text
specs/021-redesenho-fluxo-draft/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/
│   └── ui-contracts.md  # Contratos de componentes e estados visuais
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)
```text
FrontEnd/
├── src/
│   ├── views/
│   │   ├── DraftsView.vue
│   │   └── DraftsView.spec.ts
│   ├── components/drafts/
│   │   ├── DraftNavigator.vue
│   │   ├── DraftNavigator.spec.ts
│   │   ├── DraftWorkspaceHeader.vue
│   │   ├── DraftWorkspaceHeader.spec.ts
│   │   ├── DraftPreparationPanel.vue
│   │   ├── DraftPreparationPanel.spec.ts
│   │   ├── DraftDiscordPublicationPanel.vue
│   │   ├── DraftDiscordPublicationPanel.spec.ts
│   │   ├── DraftStateRail.vue
│   │   ├── DraftStateRail.spec.ts
│   │   └── visual/
│   │       ├── DraftVisualBoard.vue
│   │       └── DraftVisualBoard.spec.ts
│   ├── components/layout/
│   │   └── DraftRail.vue
│   ├── constants/
│   │   ├── draftMontagemStatus.ts
│   │   ├── systemUpdates.ts
│   │   └── systemUpdates.spec.ts
│   ├── i18n/
│   │   ├── i18n.spec.ts
│   │   └── locales/{pt,en}.json
│   └── styles/main.css
└── package.json
```

**Structure Decision**: Alterar somente `FrontEnd/`. `DraftsView.vue` fica limitada a orquestrar carregamento, ID e dados do draft selecionado, permissões, concorrência, mutações, conexão ao vivo, notificações e estado dos diálogos de comando. Destaque visual da seleção, expansão compacta, filtros apresentados e demais estados exclusivamente visuais pertencem aos filhos. Novos componentes representam regiões coesas e não importam serviços. `DraftVisualBoard.vue` preserva props, emits, lógica de turno e montagem de payload, recebendo apenas refatoração visual e cobertura direta. Componentes legados do agregado `Draft`, backend, bot e banco permanecem fora do escopo. Uma revisão pós-design verificará que a view não voltou a absorver marcação ou estado visual dos componentes extraídos.

## Complexity Tracking

Nenhuma violação constitucional requer justificativa.
