# Feature Specification: Melhorar Exibição da Ordem de Picks

**Feature Branch**: `feature/027-melhorar-ordem-picks`

**Created**: 2026-07-28

**Status**: Implemented / Verified / Deployed

**Verification update (2026-07-28)**: A revisão da Task 3 confirmou o rótulo localizado diretamente no `<ol>`, a atualização reativa PT/EN de progresso, timeout e estado vazio, e repetiu a validação Chromium sem o modal de perfil nos três viewports previstos.

**Input**: Substituir a lista corrida da ordem de picks por uma grade responsiva que permita identificar rapidamente a sequência geral, o jogador, o time e a posição daquela escolha dentro do próprio time, inclusive com dez ou mais times.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Entender a sequência geral e por time (Priority: P1)

Como participante ou organizador, quero distinguir a ordem geral do draft e a ordem das escolhas de cada time, para entender rapidamente quando cada jogador foi escolhido e qual posição ocupa na montagem de seu time.

**Why this priority**: A lista atual concatena número e nome sem hierarquia visual e não informa qual escolha cada registro representa dentro do time.

**Independent Test**: Exibir uma sequência snake de três times e verificar que cada card informa, por exemplo, `#06`, `Botelho`, `Rona` e `2ª escolha`, mantendo a ordem geral correta.

**Acceptance Scenarios**:

1. **Given** escolhas registradas para vários times, **When** a ordem é exibida, **Then** cada escolha aparece em um card com sequência geral, jogador, nome do time e ordinal da escolha dentro daquele time.
2. **Given** um formato snake, **When** o mesmo time aparece novamente em posições gerais não consecutivas, **Then** seu ordinal avança somente conforme as escolhas daquele time.
3. **Given** escolhas recebidas fora de ordem, **When** a grade é montada, **Then** os cards são ordenados por `sequencia` antes do cálculo dos ordinais por time.
4. **Given** um jogador ou time com nome longo, **When** o card é exibido, **Then** a grade permanece legível e não cria overflow horizontal.

---

### User Story 2 - Consultar drafts com muitos times (Priority: P1)

Como organizador de uma rinha grande, quero consultar toda a ordem sem limite visual fixo, expansão manual ou rolagem interna, para acompanhar drafts com dez ou mais times sem perder escolhas.

**Why this priority**: Dez times de cinco jogadores geram quarenta escolhas, e a solução não pode assumir a escala atual de doze registros.

**Independent Test**: Renderizar quarenta escolhas de dez times e confirmar que todas aparecem em sequência, usando a rolagem normal da página e uma grade que se adapta do desktop ao mobile.

**Acceptance Scenarios**:

1. **Given** dez times de cinco jogadores, **When** as quarenta escolhas são concluídas, **Then** os quarenta cards ficam visíveis sem botão de expansão nem área com rolagem interna.
2. **Given** mais de dez times ou sequências com três ou mais dígitos, **When** os cards são renderizados, **Then** quantidade, numeração e nomes continuam sem sobreposição ou corte funcional.
3. **Given** viewport largo, intermediário ou mobile, **When** a grade é exibida, **Then** ela redistribui colunas e chega a uma coluna em telas estreitas sem rolagem horizontal.
4. **Given** que cores de time se repetem ou não estão disponíveis, **When** a escolha é exibida, **Then** o nome textual do time permanece a fonte de identificação e nenhuma informação depende somente de cor.

---

### User Story 3 - Preservar estados e acessibilidade (Priority: P1)

Como usuário da interface, quero que progresso, estado vazio e informações da sequência permaneçam acessíveis e localizados, para usar a ordem de picks em português, inglês e tecnologias assistivas.

**Why this priority**: A melhoria visual não pode remover a semântica da lista ordenada, o progresso já existente ou os padrões de internacionalização.

**Independent Test**: Validar a grade com escolhas, sem escolhas e com referência de time ausente em português e inglês, inspecionando semântica, leitura e equivalência dos textos.

**Acceptance Scenarios**:

1. **Given** escolhas registradas, **When** tecnologia assistiva percorre a seção, **Then** a sequência continua representada como lista ordenada com rótulo localizado.
2. **Given** nenhuma escolha, **When** a seção é exibida, **Then** o estado vazio localizado existente permanece disponível.
3. **Given** uma escolha cujo `timeId` não corresponde aos times disponíveis, **When** o card é exibido, **Then** ele apresenta fallback localizado sem quebrar a grade.
4. **Given** português ou inglês ativo, **When** progresso, ordinal ou fallback são apresentados, **Then** todo conteúdo possui significado equivalente e acentuação correta.

### Edge Cases

- Escolhas com a mesma sequência devem preservar uma ordenação estável e não compartilhar chave de renderização inválida.
- Uma escolha de timeout sem jogador deve continuar mostrando o estado localizado já existente.
- Times sem escolhas não geram cards artificiais.
- A contagem por time considera cada registro da sequência, inclusive timeout, pois ele consumiu a vez daquele time.
- A sequência pode superar `99` sem alterar a largura funcional do identificador.
- Cores desconhecidas, repetidas ou ausentes não impedem identificar o time pelo nome.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST ordenar as escolhas por `sequencia` antes de exibi-las e antes de calcular ordinais por time.
- **FR-002**: Cada card MUST mostrar a sequência geral, o nome ou estado da escolha, o nome do time e o ordinal daquela escolha dentro do time.
- **FR-003**: O ordinal por time MUST começar em um e avançar somente para registros com o mesmo `timeId`.
- **FR-004**: O cálculo MUST funcionar independentemente do modo de ordem, incluindo sequencial, sorteado e snake.
- **FR-005**: A interface MUST renderizar todas as escolhas recebidas sem limite fixo, paginação, expansão manual ou rolagem interna.
- **FR-006**: A grade MUST usar colunas responsivas determinadas pelo espaço disponível e MUST chegar a uma coluna em viewport estreito.
- **FR-007**: A identificação do time MUST usar seu nome textual; cor pode ser reforço, mas MUST NOT ser a única identificação.
- **FR-008**: A numeração MUST acomodar sequências de três ou mais dígitos sem sobreposição.
- **FR-009**: A seção MUST preservar o progresso `{current} / {total}`, o estado vazio e a semântica de lista ordenada.
- **FR-010**: Referências de time ausentes MUST usar fallback localizado e MUST NOT interromper a renderização das demais escolhas.
- **FR-011**: Nomes longos MUST permanecer identificáveis e MUST NOT causar overflow horizontal no painel ou na página.
- **FR-012**: Textos novos ou alterados MUST existir em português e inglês com significado equivalente e acentuação portuguesa revisada.
- **FR-013**: A implementação MUST usar somente tokens visuais existentes e MUST respeitar foco, contraste e redução de movimento aplicáveis.
- **FR-014**: O cálculo e a apresentação MUST ocorrer no frontend a partir de `escolhas` e `times` já disponíveis, sem alterar contratos ou regras backend.

### Key Entities

- **Escolha do Draft**: Registro com sequência geral, time, jogador ou timeout e instante de registro.
- **Time do Draft**: Fonte do nome textual associado a cada escolha por `timeId`.
- **Ordem Apresentada**: Projeção de interface que combina escolha ordenada, time resolvido e ordinal calculado por time.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Em 100% das escolhas válidas, usuários identificam no mesmo card a posição geral e a posição dentro do time.
- **SC-002**: Uma sequência com dez times e quarenta escolhas exibe 100% dos cards sem expansão ou rolagem interna.
- **SC-003**: Viewports desktop, tablet e mobile exibem a grade sem overflow horizontal.
- **SC-004**: Em 100% dos formatos de ordem, o ordinal de cada time corresponde à contagem acumulada de registros daquele `timeId`.
- **SC-005**: Português e inglês apresentam conteúdo equivalente para progresso, ordinal e fallback.
- **SC-006**: A melhoria mantém todos os testes atuais do draft e adiciona regressão para 40+ escolhas, 10+ times, nomes longos e associação ausente.

## Design Decision

- Usar cards compactos dentro do `<ol>` existente, mantendo uma única sequência global.
- Exibir o número geral como acento primário e `Time · Nª escolha` como metadado secundário textual.
- Usar grade automática baseada em largura mínima do card, sem quantidade fixa de colunas ou times.
- Calcular uma projeção ordenada em uma única passagem, mantendo um contador por `timeId`; não agrupar por time porque isso destruiria a ordem geral.
- Não adicionar avatar, elo ou rota nesta seção para preservar densidade e foco.

## Assumptions

- `escolhas` contém um registro para cada vez consumida, inclusive timeout.
- `times` contém nome e identificador suficientes para resolver o metadado; ausência é tratada por fallback.
- A quantidade esperada continua derivada de `quantidadeTimes * (tamanhoEquipe - 1)`.
- Quarenta ou mais cards são uma escala pequena o suficiente para renderização direta no Vue, sem virtualização.

## Out of Scope

- Alterar a ordem produzida pelo backend ou o formato snake.
- Agrupar escolhas por time e perder a sequência cronológica global.
- Paginar, recolher ou virtualizar a lista.
- Adicionar avatares, elo, rota ou detalhes completos do jogador.
- Criar novas cores ou exigir uma cor exclusiva para cada time.
- Alterar API, banco de dados, domínio ou contratos de tempo real.
