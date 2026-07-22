# Feature Specification: Histórico de Atualizações

**Feature Branch**: `feature/019-historico-atualizacoes`

**Created**: 2026-07-22

**Status**: Draft

**Input**: Página frontend autenticada para consultar, pesquisar e manter um histórico editorial localizado das atualizações do sistema.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consultar novidades do sistema (Priority: P1)

Como usuário autenticado, quero consultar atualizações em ordem cronológica e expandir seus detalhes, para entender cada mudança disponível na plataforma.

**Why this priority**: Consultar o histórico é o valor central da feature e entrega uma experiência útil mesmo sem busca, filtros ou indicador de conteúdo novo.

**Independent Test**: Um usuário autenticado acessa `/atualizacoes`, identifica a release mais recente, percorre os oito marcos em ordem cronológica decrescente e expande os detalhes de cada categoria por mouse ou teclado.

**Acceptance Scenarios**:

1. **Given** um usuário autenticado, **When** ele acessa `/atualizacoes`, **Then** visualiza a release mais recente em destaque e os oito marcos históricos em ordem cronológica decrescente.
2. **Given** uma atualização com detalhes agrupados, **When** o usuário aciona um grupo por mouse, teclado ou toque, **Then** o conteúdo correspondente é expandido e o estado do controle é comunicado de forma acessível.
3. **Given** uma atualização com link para uma área relacionada, **When** o usuário aciona o link, **Then** navega para uma rota interna válida sem recarregar a aplicação.
4. **Given** qualquer tamanho de tela suportado, **When** o usuário consulta o histórico, **Then** consegue ler e operar cards, detalhes e links sem rolagem horizontal da página.

---

### User Story 2 - Encontrar uma mudança específica (Priority: P2)

Como usuário autenticado, quero buscar atualizações e filtrá-las por categoria, para localizar rapidamente uma novidade, melhoria ou correção.

**Why this priority**: Busca e filtros reduzem o esforço de localização conforme o histórico cresce, mas dependem do histórico consultável entregue pela primeira história.

**Independent Test**: Com o histórico disponível, o usuário pesquisa termos presentes em títulos, resumos e detalhes no idioma ativo, combina a pesquisa com categorias e recupera todos os resultados ao limpar os filtros.

**Acceptance Scenarios**:

1. **Given** o catálogo em português ou inglês, **When** o usuário busca um termo localizado presente em título, resumo ou detalhe, **Then** somente atualizações com conteúdo correspondente são exibidas.
2. **Given** uma busca preenchida, **When** o usuário seleciona uma ou mais categorias, **Then** os resultados satisfazem simultaneamente o texto e pelo menos uma categoria selecionada.
3. **Given** filtros sem correspondências, **When** a consulta é aplicada, **Then** são exibidos quantidade zero, mensagem localizada e uma ação para limpar os filtros.
4. **Given** busca e categorias ativas, **When** o usuário limpa os filtros, **Then** o histórico completo volta a ser exibido.

---

### User Story 3 - Perceber conteúdo ainda não visualizado (Priority: P2)

Como usuário autenticado, quero ver um indicador de nova atualização na navegação, para saber quando há conteúdo que ainda não consultei neste navegador.

**Why this priority**: O indicador torna novas entregas descobríveis, mas a consulta do histórico permanece funcional sem ele.

**Independent Test**: Em um navegador sem versão visualizada, o item Atualizações apresenta o badge Novo; ao abrir a página, o indicador desaparece imediatamente e permanece ausente enquanto a versão mais recente não mudar.

**Acceptance Scenarios**:

1. **Given** nenhuma versão visualizada ou uma versão diferente da mais recente, **When** a navegação autenticada é exibida, **Then** o item Atualizações apresenta um indicador textual localizado de conteúdo novo.
2. **Given** o indicador visível, **When** o usuário abre a página de atualizações, **Then** a versão mais recente é marcada como visualizada e o indicador desaparece sem recarregar a aplicação.
3. **Given** armazenamento local indisponível, **When** a aplicação tenta ler ou registrar a versão visualizada, **Then** a navegação e a página continuam funcionais sem erro visível.

---

### User Story 4 - Manter o histórico junto das entregas (Priority: P2)

Como mantenedor, quero cadastrar releases em um contrato tipado e localizado, para publicar textos consistentes sem depender de backend ou geração por commits.

**Why this priority**: Um processo verificável de manutenção preserva a qualidade do histórico após a entrega inicial e evita divergências entre idiomas ou conteúdo técnico acidental.

**Independent Test**: Um mantenedor adiciona uma release conforme o guia, classifica categorias e áreas, inclui as traduções nos dois idiomas e obtém validação automática de estrutura, ordem, links e paridade antes da entrega.

**Acceptance Scenarios**:

1. **Given** uma nova release válida e localizada, **When** o mantenedor executa as validações documentadas, **Then** o registro é aceito e a release mais recente é determinada sem configuração duplicada.
2. **Given** ID ou versão duplicada, data ou versão inválida, categoria ou área desconhecida, tradução ausente ou link interno inválido, **When** as validações são executadas, **Then** a entrega falha com indicação objetiva da inconsistência.
3. **Given** uma mudança visível ao usuário, **When** o mantenedor segue o checklist da feature, **Then** ele revisa explicitamente a necessidade de atualizar o histórico.

### Edge Cases

- Versões ou datas repetidas, fora do formato definido ou fora da ordem cronológica impedem a entrega do registro.
- Uma release sem categoria, área afetada ou detalhe é considerada inválida.
- Categorias ou áreas fora dos conjuntos reconhecidos são rejeitadas antes da entrega.
- Termos de busca com diferenças de caixa ou acentuação encontram o conteúdo localizado equivalente.
- Alterar o idioma com uma busca ativa recalcula os resultados usando o novo catálogo.
- Links ausentes não deixam controles vazios; links presentes precisam pertencer às rotas internas conhecidas.
- Falhas de leitura ou escrita no armazenamento local usam estado temporário da sessão e não bloqueiam a navegação.
- Um histórico sem correspondência para filtros apresenta estado vazio; o registro inicial completo não pode estar vazio.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST disponibilizar `/atualizacoes` somente para usuários autenticados, sem restrição adicional por papel.
- **FR-002**: O sistema MUST apresentar exatamente oito marcos históricos iniciais coerentes com as grandes entregas aprovadas, sem gerar entradas automaticamente por commit.
- **FR-003**: A release mais recente MUST detalhar individualmente os 15 itens de confiabilidade operacional definidos no design aprovado e destacar acesso direto, modais contextuais, status de publicação, recuperação individual, presença em tempo real e busca manual.
- **FR-004**: Cada release MUST possuir ID estável, versão única no formato `AAAA.MM.N`, data de publicação ISO válida, título, resumo, pelo menos uma categoria, pelo menos uma área afetada e pelo menos um detalhe.
- **FR-005**: O sistema MUST reconhecer somente as categorias `feature`, `improvement`, `fix`, `security` e `infrastructure`; uma release MAY reunir várias categorias, e cada detalhe MUST pertencer a exatamente uma delas.
- **FR-006**: O sistema MUST reconhecer como áreas iniciais plataforma, jogadores, times, usuários, drafts, Discord, segurança e infraestrutura, todas identificadas por valores controlados e rótulos localizados.
- **FR-007**: O histórico MUST ser exibido em ordem cronológica decrescente, agrupado por ano e mês, com versão e data visíveis; a release mais recente MUST ser determinada a partir do próprio registro.
- **FR-008**: A página MUST destacar a release mais recente em um hero com resumo, versão, data, áreas afetadas e categorias principais.
- **FR-009**: Cada atualização MUST exibir título, resumo, versão, data, categorias, áreas afetadas e detalhes agrupados por categoria, expansíveis por controles acessíveis.
- **FR-010**: O sistema MUST permitir busca sem distinção de caixa ou acentuação sobre título, resumo e detalhes conforme o idioma ativo.
- **FR-011**: O sistema MUST permitir selecionar categorias, combinar o filtro com a busca textual, exibir a quantidade de resultados e limpar todos os filtros.
- **FR-012**: Quando nenhum resultado corresponder à consulta, o sistema MUST apresentar estado vazio localizado com ação para restaurar o histórico completo.
- **FR-013**: Releases MAY conter links opcionais somente para rotas internas conhecidas; todos os links presentes MUST ser validados e navegáveis por mouse, teclado e toque.
- **FR-014**: O item Atualizações MUST aparecer na navegação autenticada de desktop e mobile e MUST apresentar o badge localizado Novo quando não houver versão visualizada ou ela diferir da versão mais recente.
- **FR-015**: Ao abrir a página, o sistema MUST registrar a versão mais recente como visualizada neste navegador e remover o badge sem recarregar; o estado não MUST ser sincronizado entre dispositivos.
- **FR-016**: A indisponibilidade ou falha do armazenamento local MUST usar fallback temporário em memória e MUST NOT impedir acesso à navegação ou ao histórico.
- **FR-017**: Todos os textos visíveis, inclusive conteúdo editorial, categorias, áreas, filtros, badge, estados vazios e nomes acessíveis, MUST existir em português e inglês com estrutura equivalente e sem texto hardcoded na interface ou no registro estrutural.
- **FR-018**: A experiência MUST ser responsiva em desktop e mobile, usar os tokens e componentes existentes, evitar overflow horizontal e manter filtros, detalhes, links e ações alcançáveis.
- **FR-019**: Timeline, filtros e detalhes MUST usar semântica adequada, foco visível, nomes acessíveis, estados selecionado e expandido comunicados, datas semânticas e significado textual independente de cor.
- **FR-020**: O projeto MUST fornecer um guia de manutenção que cubra versionamento mensal, registro, localização, classificação, links, validações e commit, e o checklist padrão MUST solicitar revisão explícita do histórico em mudanças visíveis.

### Key Entities

- **Release editorial**: Marco publicado, identificado por ID, versão e data, com categorias, áreas afetadas, destaque, referências localizadas e links internos opcionais.
- **Detalhe de release**: Mudança editorial individual vinculada a uma release e a exatamente uma categoria, com referências localizadas para título e descrição.
- **Categoria**: Classificação fechada de uma release ou detalhe entre novidade, melhoria, correção, segurança e infraestrutura.
- **Área afetada**: Parte reconhecida do produto impactada pela release e apresentada por rótulo localizado.
- **Versão visualizada**: Versão mais recente consultada no navegador atual, usada somente para determinar o indicador de conteúdo novo.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos usuários autenticados conseguem abrir `/atualizacoes` e identificar a release mais recente, sua versão e data em até 10 segundos.
- **SC-002**: Os oito marcos históricos aparecem em ordem cronológica decrescente, e a release mais recente apresenta os 15 detalhes aprovados individualmente.
- **SC-003**: Em testes de tarefa, pelo menos 90% dos usuários localizam uma atualização por busca e categoria em até 30 segundos e restauram o histórico em uma única ação.
- **SC-004**: 100% das funções interativas da página podem ser operadas por teclado e toque, sem ações inacessíveis ou overflow horizontal nas larguras suportadas.
- **SC-005**: O indicador de conteúdo novo aparece em 100% dos cenários com versão ausente ou divergente e desaparece sem recarga ao abrir a página, inclusive com fallback funcional quando o armazenamento local falha.
- **SC-006**: As validações detectam 100% dos casos exercitados de IDs ou versões duplicados, formatos inválidos, desordem cronológica, classificações desconhecidas, traduções ausentes e links internos inválidos.
- **SC-007**: Português e inglês apresentam a mesma estrutura editorial e todos os controles, estados e conteúdos sem texto visível fora dos catálogos de tradução.
- **SC-008**: Um mantenedor consegue adicionar e validar uma release seguindo somente o guia em até 15 minutos, sem alterar backend ou consultar commits para gerar conteúdo.

## Assumptions

- A autenticação, o shell responsivo, a navegação interna e os catálogos português/inglês existentes serão reutilizados.
- O histórico inicial será curado a partir do design aprovado e das fontes já versionadas no repositório.
- O registro será entregue junto da aplicação; não haverá carregamento de rede, painel administrativo ou persistência no backend nesta versão.
- O indicador Novo representa apenas o navegador atual e usa a chave `rinha:last-seen-system-update`.
- Datas editoriais seguem o histórico real disponível, e a versão mensal identifica uma entrega editorial, não uma feature ou commit individual.

## Out of Scope

- Painel administrativo, endpoint, banco de dados ou migration para publicar releases.
- Geração automática por commits ou integração com GitHub Releases.
- Notificações push, por e-mail ou Discord, comentários, reações ou imagens específicas por release.
- Sincronização da versão visualizada entre navegadores ou dispositivos.
