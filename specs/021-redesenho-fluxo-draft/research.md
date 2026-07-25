# Research: Redesenho do Fluxo de Draft

## Decisão 1: preservar a orquestração em `DraftsView`

**Decision**: Manter carregamento, ID e dados do draft selecionado, permissões, chamadas, atualização ao vivo, proteção contra respostas obsoletas, notificações e diálogos de comando em `DraftsView.vue`. Destaque visual do selecionado, expansão compacta e filtros apresentados pertencem ao navegador filho. Componentes novos recebem dados prontos e emitem intenções.

**Rationale**: A view já protege concorrência com gerações, versões de requisição e cancelamento de busca. Mover esse comportamento ampliaria o risco sem contribuir para o redesenho.

**Alternatives considered**:

- Extrair store ou composable completo: rejeitado por alterar o principal limite comportamental.
- Alterar apenas CSS: rejeitado por manter responsabilidades visuais incompatíveis no mesmo template.

## Decisão 2: extrair regiões visuais coesas

**Decision**: Criar `DraftNavigator`, `DraftWorkspaceHeader`, `DraftPreparationPanel` e `DraftDiscordPublicationPanel`. Manter `DraftStateRail` e `DraftVisualBoard`, refatorando-os sem mudar seus contratos públicos essenciais.

**Rationale**: Navegação, contexto estável, preparação e integração Discord possuem responsabilidades próprias e testáveis. Presença encerrada, capitães e ordem compartilham participantes e formam um único painel de preparação.

**Alternatives considered**:

- Um componente por status: rejeitado por duplicar a apresentação dos mesmos participantes.
- Um card genérico de participante para presença, times e pool: rejeitado porque seleção de capitão, drag-and-drop, escolha e substituição possuem semânticas diferentes.
- Extrair todas as partes internas do board: adiado; a feature deve corrigir a experiência sem ampliar desnecessariamente a API entre componentes.

## Decisão 3: um único shell operacional

**Decision**: O cabeçalho do workspace concentra nome, data, status, contadores, progresso e slot de ações. O cabeçalho duplicado de `DraftVisualBoard` será removido. Cada estado terá no máximo uma ação primária.

**Rationale**: A estrutura atual exibe um painel de presença em todos os estados e depois adiciona outro cabeçalho no board, criando hierarquia conflitante.

**Alternatives considered**:

- Manter cabeçalhos por etapa: rejeitado por deslocar contexto e progresso durante transições.
- Barra global com todas as ações: rejeitada porque ações incompatíveis competem visualmente.

## Decisão 4: progresso terminal e acessível

**Decision**: Manter o progresso como lista ordenada. Etapa atual recebe `aria-current="step"`; concluída, atual, pendente e atenção usam texto/forma além de cor. `Cancelada` é terminal e status desconhecido é neutro, sem ativar presença.

**Rationale**: O cálculo atual converte qualquer status fora da sequência para a primeira etapa ativa.

**Alternatives considered**:

- Rail clicável: rejeitado porque etapas não são navegáveis.
- Rail horizontal quebrando em várias linhas: rejeitado por tornar a sequência ambígua.

## Decisão 5: responsividade e propriedade da rolagem

**Decision**: Acima de 1024px, navegador lateral e workspace. De 769px a 1024px, navegador horizontal e workspace em largura total. Até 768px, uma coluna com seleção compacta de draft. `.app-shell__content` permanece a única rolagem vertical da página; listas de participantes crescem no documento.

**Rationale**: O grid e o painel atuais criam compressão e rolagens concorrentes. `1024px` preserva a convenção do shell existente e `768px` segue o breakpoint oficial do design system; nenhum token novo é necessário.

**Alternatives considered**:

- Scroll vertical próprio no navegador: rejeitado pelo conflito com a página.
- Drawer de drafts no desktop: rejeitado por esconder contexto frequente.
- Cards horizontais no mobile: rejeitados por introduzir navegação bidirecional numa tela operacional.

## Decisão 6: board ativo preserva comportamento

**Decision**: Preservar props/emits, clone local, drag-and-drop, relógio, áudio, filtragem, regras visuais de escolha e montagem de payload do `DraftVisualBoard`. Ordenar cópias para apresentação por `time.ordem`, mostrar progresso de escolhas e ocultar affordances mutáveis em estados terminais.

**Rationale**: A lógica existente é sensível a identidade do jogador, turno e atualização ao vivo. A refatoração deve ser visual e não pode alterar payload ou permissão.

**Alternatives considered**:

- Reutilizar `DraftBoard` ou `DraftPickHistory` legados: rejeitado porque pertencem a tipos e contratos diferentes.
- Ordenar os arrays mutáveis do board: rejeitado porque mudaria o payload salvo.

## Decisão 7: componentes UI e design system existentes

**Decision**: Reutilizar `Button`, `Badge`, `Input`, `Select`, `Field`, `Empty`, `Skeleton`, `Alert`, `ToggleGroup`, `Dialog` e tokens documentados. Não adicionar dependências, tokens ou wrappers genéricos.

**Rationale**: Os componentes locais já cobrem acessibilidade básica e identidade visual. O problema está na composição e no uso de classes legadas.

**Alternatives considered**:

- Nova biblioteca visual: rejeitada por custo, inconsistência e risco.
- Novos tokens para a tela: rejeitados porque as escalas atuais são suficientes.

## Decisão 8: estratégia de teste em três camadas

**Decision**: Usar testes focados por componente para semântica e eventos, testes de `DraftsView` para orquestração e permissões, e Chromium real para reflow, overflow, foco, toque e movimento reduzido. Adicionar `lint:check` com `eslint .` ao `package.json`, mantendo o script atual com `--fix` separado.

**Rationale**: `happy-dom` não calcula layout; testes só na view seriam grandes e frágeis; validação apenas manual não protege concorrência ou eventos.

**Alternatives considered**:

- Snapshots extensos: rejeitados por baixa utilidade durante redesenho.
- Introduzir Playwright: rejeitado porque Vitest e `agent-browser` atendem ao escopo sem dependência nova.

## Decisão 9: publicação editorial em duas etapas

**Decision**: Publicar agora `2026.07.3` somente para a correção dos dias selecionados, com categoria `fix`, área `drafts`, link para Configurações e PT/EN. Publicar o redesenho em versão posterior definida na data real da entrega.

**Rationale**: A correção já está em produção e deve ser comunicada; anunciar o redesenho antes da entrega violaria o padrão editorial.

**Alternatives considered**:

- Agrupar correção e redesenho em uma única release futura: rejeitado porque omite uma mudança já disponível.
- Reservar `2026.07.4`: rejeitado porque outra entrega pode ocorrer antes.
