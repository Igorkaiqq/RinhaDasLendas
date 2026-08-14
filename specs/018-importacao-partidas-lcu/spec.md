# Feature Specification: Importação de Partidas do League Client

**Feature Branch**: `feature/018-importacao-partidas-lcu`

**Created**: 2026-07-21

**Status**: Draft

**Input**: User description: "Permitir que o RinhaDasLendas receba dados completos de partidas sem Riot Developer API key, usando um coletor local seguro, arquivo versionado, upload autenticado e fallback manual."

## Contexto e objetivo

O RinhaDasLendas já organiza jogadores, times e drafts, mas as áreas de partidas e estatísticas ainda não possuem um fluxo real. A comunidade precisa registrar o resultado completo das partidas personalizadas sem depender de Riot Developer API key, Match-v5, RSO ou Tournament API.

A feature deve permitir que um organizador use um coletor local no Windows para ler dados pós-partida disponibilizados pelo League Client, transformar esses dados em um arquivo seguro e versionado e importá-lo no RinhaDasLendas. O backend permanece como fonte de verdade, valida o conteúdo, permite revisar e associar participantes e persiste a partida de forma relacional.

Como a integração local não é oficialmente suportada e pode falhar ou mudar entre versões, o cadastro manual de partidas e estatísticas deve continuar disponível.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Exportar uma partida do cliente local (Priority: P1)

Como organizador com o League Client aberto, quero selecionar ou informar uma partida concluída e gerar um arquivo próprio do RinhaDasLendas, para levar as estatísticas ao sistema sem precisar copiar cada valor manualmente.

**Why this priority**: Sem uma forma segura de retirar os dados do computador local, o sistema não consegue importar estatísticas completas sem uma API key oficial.

**Independent Test**: Pode ser testado com uma resposta LCU sanitizada conhecida, gerando um arquivo compatível e verificando que nenhuma credencial ou campo proibido está presente.

**Acceptance Scenarios**:

1. **Given** o League Client está aberto, autenticado e possui a partida solicitada, **When** o organizador informa o Game ID, **Then** o coletor gera um arquivo `*.rinha-match.json` com os dados pós-partida permitidos.
2. **Given** o arquivo foi gerado, **When** seu conteúdo é inspecionado, **Then** ele contém versão de schema, identificação da partida, times, participantes, resultado, estatísticas, builds, runas e bans disponíveis.
3. **Given** o League Client está fechado ou o Game ID não está disponível, **When** o organizador tenta exportar, **Then** recebe orientação clara e nenhum arquivo parcial é tratado como exportação válida.
4. **Given** o endpoint local retorna campos desconhecidos ou sensíveis, **When** o coletor mapeia a resposta, **Then** somente campos explicitamente permitidos entram no arquivo.

---

### User Story 2 - Importar e revisar a partida no Rinha (Priority: P1)

Como administrador autenticado, quero enviar o arquivo gerado e revisar uma prévia antes de confirmar, para evitar registrar uma partida incorreta ou incompleta.

**Why this priority**: A importação somente entrega valor quando os dados chegam ao backend com validação, contexto e confirmação humana.

**Independent Test**: Pode ser testado enviando um fixture válido, conferindo a prévia e confirmando a persistência de uma partida com dois times e dez participantes.

**Acceptance Scenarios**:

1. **Given** um arquivo válido e suportado, **When** um usuário autorizado realiza o upload, **Then** o sistema valida o conteúdo e apresenta uma prévia sem persistir a partida como confirmada.
2. **Given** a prévia está correta, **When** o administrador confirma, **Then** a partida e seus dados relacionados são persistidos uma única vez.
3. **Given** o arquivo é inválido, incompatível, excessivo ou adulterado, **When** o upload é processado, **Then** o sistema rejeita a importação com mensagem localizada e não persiste dados de negócio.
4. **Given** o mesmo Match ID já foi confirmado com o mesmo conteúdo, **When** o arquivo é importado novamente, **Then** o sistema retorna o registro existente sem duplicação.
5. **Given** o mesmo Match ID já existe com conteúdo materialmente diferente, **When** uma nova importação é tentada, **Then** o sistema bloqueia a substituição silenciosa e informa conflito.

---

### User Story 3 - Associar participantes aos jogadores cadastrados (Priority: P1)

Como administrador, quero que participantes sejam associados automaticamente por Riot ID e que ambiguidades possam ser resolvidas manualmente, para integrar a partida ao histórico real dos jogadores do grupo.

**Why this priority**: Estatísticas por jogador exigem vínculo confiável entre o snapshot da partida e o cadastro interno, sem perder participantes externos.

**Independent Test**: Pode ser testado importando uma partida com jogadores conhecidos, desconhecidos, renomeados e com Riot IDs ambíguos.

**Acceptance Scenarios**:

1. **Given** o Riot ID normalizado corresponde de forma única a um jogador cadastrado, **When** a prévia é montada, **Then** o participante aparece associado automaticamente.
2. **Given** não existe correspondência, **When** a partida é importada, **Then** o participante permanece como snapshot não vinculado e a importação pode prosseguir.
3. **Given** há correspondência ambígua ou suspeita, **When** o administrador revisa a prévia, **Then** o sistema exige seleção manual ou manutenção do participante como não vinculado.
4. **Given** um participante não vinculado foi associado posteriormente, **When** a alteração é confirmada por usuário autorizado, **Then** as estatísticas passam a compor o histórico do jogador sem alterar o snapshot original.

---

### User Story 4 - Consultar partida completa (Priority: P1)

Como membro autenticado da comunidade, quero abrir uma partida e consultar placar, times, campeões, objetivos, danos, visão, builds, runas e bans, para analisar o resultado sem depender do cliente do LoL.

**Why this priority**: A consulta é o resultado visível do fluxo de importação e substitui os placeholders atuais de partidas.

**Independent Test**: Pode ser testado abrindo uma partida confirmada e conferindo os valores persistidos contra o fixture usado na importação.

**Acceptance Scenarios**:

1. **Given** uma partida confirmada, **When** o usuário abre os detalhes, **Then** o sistema apresenta resultado geral, duração, patch, fila, mapa, times e objetivos.
2. **Given** a partida possui estatísticas individuais, **When** o placar é exibido, **Then** o usuário consegue consultar KDA, farm, ouro, dano, cura, mitigação, visão, estruturas, itens, runas e feitiços disponíveis.
3. **Given** algum campo não foi fornecido pela fonte, **When** a tela é renderizada, **Then** o sistema diferencia dado indisponível de valor zero.
4. **Given** a partida é personalizada, **When** um usuário não autorizado ou público tenta consultá-la, **Then** o acesso respeita as regras de privacidade e consentimento.

---

### User Story 5 - Manter cadastro manual como fallback (Priority: P1)

Como administrador, quero registrar ou completar uma partida manualmente quando o cliente local estiver indisponível, para que a operação do grupo não dependa exclusivamente de uma integração instável.

**Why this priority**: A constituição do projeto exige que integrações externas não travem o produto e que exista alternativa manual.

**Independent Test**: Pode ser testado com League Client ausente, registrando manualmente os dados mínimos de uma partida e consultando o resultado.

**Acceptance Scenarios**:

1. **Given** o coletor não está disponível, **When** o administrador acessa partidas, **Then** consegue iniciar o cadastro manual.
2. **Given** uma partida importada possui campos opcionais ausentes, **When** o administrador possui permissão para complementar dados, **Then** consegue preencher campos permitidos com auditoria.
3. **Given** um dado veio de importação confirmada, **When** alguém tenta sobrescrevê-lo manualmente, **Then** o sistema exige confirmação contextual, motivo e autorização apropriada.

---

### User Story 6 - Consultar estatísticas agregadas por jogador (Priority: P2)

Como jogador, quero consultar estatísticas calculadas a partir das partidas vinculadas, para acompanhar desempenho, campeões, rotas e evolução dentro da comunidade.

**Why this priority**: Agregações ampliam o valor dos registros, mas dependem primeiro de importação, persistência e vínculo confiáveis.

**Independent Test**: Pode ser testado com um conjunto conhecido de partidas, verificando partidas jogadas, vitórias, derrotas, win rate, KDA e médias.

**Acceptance Scenarios**:

1. **Given** um jogador possui partidas confirmadas e vinculadas, **When** abre suas estatísticas, **Then** vê partidas, vitórias, derrotas, win rate, KDA e médias de kills, deaths, assists, farm, ouro, dano e visão.
2. **Given** o jogador possui partidas com diferentes campeões e rotas, **When** consulta os agregados, **Then** vê campeões e rotas mais utilizados.
3. **Given** uma associação de participante é corrigida, **When** as estatísticas são consultadas novamente, **Then** os agregados refletem o vínculo atual sem duplicar partidas.
4. **Given** uma partida ainda está em prévia, rejeitada ou cancelada, **When** as estatísticas são calculadas, **Then** ela não compõe os agregados oficiais.

---

### User Story 7 - Vincular partida ao draft que a originou (Priority: P2)

Como administrador, quero associar uma partida importada a um draft ou montagem existente, para preservar a trajetória desde a formação dos times até o resultado.

**Why this priority**: O vínculo melhora rastreabilidade e integração entre módulos, mas a partida pode existir independentemente de um draft.

**Independent Test**: Pode ser testado vinculando uma partida a um draft compatível e rejeitando vínculos incompatíveis.

**Acceptance Scenarios**:

1. **Given** a composição importada corresponde ao draft selecionado dentro das regras de equivalência aprovadas, **When** o administrador confirma o vínculo, **Then** partida e draft ficam relacionados.
2. **Given** a composição diverge, **When** o vínculo é solicitado, **Then** o sistema mostra as diferenças e exige resolução ou manutenção sem vínculo.
3. **Given** uma partida não veio de draft do Rinha, **When** é confirmada, **Then** pode permanecer sem vínculo.

---

### User Story 8 - Enviar diretamente com autorização temporária (Priority: P3)

Como organizador, quero enviar a partida diretamente pelo coletor usando um código temporário, para eliminar o passo de salvar e selecionar o arquivo sem armazenar credenciais permanentes.

**Why this priority**: Melhora a experiência, mas o upload de arquivo já entrega o valor principal com menor risco e complexidade.

**Independent Test**: Pode ser testado gerando código de uso único, enviando um payload e verificando expiração e prevenção de reutilização.

**Acceptance Scenarios**:

1. **Given** um usuário autorizado gera um código de importação válido, **When** o coletor o usa uma vez dentro do prazo, **Then** o backend recebe a importação e invalida o código.
2. **Given** o código expirou, foi consumido ou não pertence ao contexto esperado, **When** um envio é tentado, **Then** o backend rejeita sem revelar detalhes sensíveis.
3. **Given** o backend está indisponível, **When** o envio falha, **Then** o coletor preserva a opção de gerar arquivo para upload posterior.

---

### User Story 9 - Sugerir exportação ao fim da partida (Priority: P3)

Como organizador, quero receber uma sugestão local após o fim do jogo, para reduzir o risco de esquecer a importação sem permitir envio silencioso.

**Why this priority**: É conveniência posterior; depende de compatibilidade local confiável e não pode substituir consentimento e confirmação.

**Independent Test**: Pode ser testado simulando transições de estado do cliente e verificando que apenas uma sugestão é exibida por partida.

**Acceptance Scenarios**:

1. **Given** o coletor detecta uma partida concluída, **When** os dados ficam disponíveis, **Then** oferece exportar ou enviar após confirmação explícita.
2. **Given** a mesma partida já foi exportada ou enviada, **When** o cliente retorna ao pós-jogo, **Then** o coletor não cria duplicidade automática.
3. **Given** a detecção automática falha, **When** o usuário abre o coletor, **Then** o fluxo manual por Game ID continua disponível.

## Edge Cases

- League Client fechado, deslogado, atualizando ou reiniciando durante a leitura.
- `lockfile` ausente, incompleto, substituído durante a leitura ou apontando para porta expirada.
- League of Legends instalado fora do caminho padrão.
- Mais de uma instalação ou sessão detectada.
- Certificado local autoassinado ou falha específica do provedor TLS do Windows.
- Game ID inexistente, de outra região, ainda não indexado ou removido do histórico local.
- Partida encontrada no log, mas sem resumo completo disponível no cliente.
- Endpoint LCU renomeado, removido ou com schema alterado após patch.
- Campos opcionais ausentes, desconhecidos ou com tipo alterado.
- Partida com menos ou mais participantes que o formato padrão esperado.
- Remake, abandono, early surrender, surrender normal ou resultado indefinido.
- Participante sem Riot ID, com Riot ID renomeado ou com capitalização diferente.
- Dois jogadores cadastrados com Riot ID conflitante.
- Mesmo participante aparecendo duas vezes no arquivo.
- Mesmo Match ID importado simultaneamente por dois administradores.
- Mesmo Match ID com hash diferente.
- Arquivo truncado, editado, excessivamente grande, profundamente aninhado ou com extensão enganosa.
- IDs numéricos negativos, overflow, `NaN`, valores impossíveis ou estatísticas inconsistentes.
- Item, campeão, runa ou feitiço desconhecido no catálogo local do Rinha.
- Upload interrompido após validação, mas antes da confirmação.
- Usuário perde permissão entre prévia e confirmação.
- Código temporário expirado, roubado, reutilizado ou usado para Match ID diferente.
- Backend indisponível durante envio direto.
- Partida customizada sem consentimento aplicável para exibição.
- Partida vinculada a draft com jogadores ou lados divergentes.
- Correção posterior de vínculo altera agregados concorrentes.
- Dados zero legítimos precisam ser diferenciados de dados ausentes.

## Requirements *(mandatory)*

### Functional Requirements

#### Coletor e exportação local

- **FR-001**: O sistema MUST oferecer um coletor local para Windows que funcione sem Riot Developer API key.
- **FR-002**: O coletor MUST operar somente com serviços locais do League Client e MUST NOT enviar credenciais do cliente a terceiros.
- **FR-003**: O coletor MUST permitir informar um Game ID e SHOULD permitir selecionar uma partida recente quando a fonte local disponibilizar essa informação com segurança.
- **FR-004**: O coletor MUST gerar um arquivo com extensão identificável pelo RinhaDasLendas e conteúdo JSON UTF-8.
- **FR-005**: O arquivo MUST possuir `schemaVersion`, origem, data de coleta, Match ID externo, plataforma e dados da partida disponíveis.
- **FR-006**: O coletor MUST usar allowlist explícita de campos exportáveis.
- **FR-007**: O coletor MUST NOT exportar senha do `lockfile`, cabeçalho de autorização, tokens, JWTs, URLs assinadas, endereço/porta da sessão de jogo ou credenciais de chat.
- **FR-008**: O coletor MUST restringir qualquer exceção de certificado ao serviço LCU em loopback e MUST NOT desabilitar validação TLS globalmente.
- **FR-009**: O coletor MUST apresentar erro localizado e acionável quando o cliente estiver fechado, a sessão expirar, a partida não existir ou os dados ainda não estiverem prontos.
- **FR-010**: O coletor MUST aplicar timeout, cancelamento e tentativas limitadas, sem polling agressivo.
- **FR-011**: O coletor MUST validar minimamente a resposta local antes de gerar uma exportação.
- **FR-012**: O coletor MUST produzir o arquivo de forma atômica para evitar que arquivos parciais aparentem sucesso.
- **FR-013**: O coletor MUST registrar apenas informações operacionais necessárias e MUST redigir segredos e dados pessoais desnecessários.
- **FR-014**: O coletor SHOULD informar versão própria, versão do schema e compatibilidade conhecida com o patch detectado.
- **FR-015**: Falha de tradução de um ID estático MUST NOT apagar o ID original nem invalidar automaticamente todas as demais estatísticas.

#### Contrato de importação

- **FR-016**: O contrato MUST ser independente do payload bruto do LCU e possuir versionamento explícito.
- **FR-017**: O contrato MUST representar metadados da partida, dois times, participantes, resultado, objetivos, bans, itens, runas e feitiços quando fornecidos.
- **FR-018**: O contrato MUST distinguir campo ausente de valor numérico zero.
- **FR-019**: O contrato MUST definir limites de tamanho, profundidade, quantidade de times, participantes, bans, itens e runas.
- **FR-020**: O contrato MUST preservar IDs externos necessários para auditoria e tradução posterior.
- **FR-021**: O contrato MUST incluir hash canônico ou informação equivalente para detectar conteúdo divergente do mesmo Match ID.
- **FR-022**: O backend MUST rejeitar versões de schema não suportadas com orientação localizada.
- **FR-023**: O backend MUST NOT resolver URLs ou caminhos fornecidos pelo arquivo importado.
- **FR-024**: O backend MUST validar tipos, intervalos, invariantes e consistência entre totais, times, participantes e resultado.
- **FR-025**: Fixtures e exemplos versionados no repositório MUST ser sintéticos ou irreversivelmente sanitizados.

#### Upload, prévia e confirmação

- **FR-026**: Apenas usuários autenticados com permissão específica MUST poder iniciar importação.
- **FR-027**: O upload MUST impor limite de tamanho e processar o JSON com limites seguros de profundidade e memória.
- **FR-028**: Uma importação válida MUST criar uma prévia isolada antes da confirmação da partida.
- **FR-029**: A prévia MUST exibir Match ID, data, duração, resultado, times, participantes, vínculos sugeridos, campos ausentes e alertas.
- **FR-030**: Apenas usuários autorizados MUST poder confirmar, rejeitar ou corrigir vínculos da prévia.
- **FR-031**: Importações rejeitadas ou expiradas MUST NOT compor histórico ou estatísticas.
- **FR-032**: A confirmação MUST ser atômica e persistir uma única partida com seus dados relacionados.
- **FR-033**: Importações concorrentes do mesmo Match ID MUST resultar em no máximo uma partida confirmada.
- **FR-034**: Reimportar o mesmo Match ID com conteúdo equivalente MUST ser idempotente.
- **FR-035**: Reimportar o mesmo Match ID com conteúdo materialmente diferente MUST gerar conflito e exigir fluxo administrativo explícito.
- **FR-036**: O sistema MUST registrar origem, versão do schema, hash, usuário, momento e resultado de cada tentativa de importação sem armazenar segredos.
- **FR-037**: Erros de importação MUST usar códigos estáveis e mensagens localizadas, sem expor payload técnico ao usuário comum.

#### Domínio e persistência

- **FR-038**: Uma partida confirmada MUST possuir identificador interno UUID e Match ID externo único quando disponível.
- **FR-039**: Uma partida MUST conter exatamente dois times para o fluxo padrão desta feature.
- **FR-040**: Cada participante MUST pertencer a exatamente um time da partida.
- **FR-041**: O mesmo participante externo ou jogador interno MUST NOT aparecer duas vezes na mesma partida.
- **FR-042**: O domínio MUST validar resultado, duração, data e estatísticas não negativas ou dentro de limites aprovados.
- **FR-043**: O sistema MUST persistir dados de negócio de forma relacional.
- **FR-044**: Payload externo opcional retido para auditoria MUST NOT ser a única fonte de verdade do domínio.
- **FR-045**: A partida MUST preservar snapshots de Riot ID, campeão, rota, itens, runas e estatísticas usados no momento da importação.
- **FR-046**: Um participante MAY permanecer sem vínculo com Jogador.
- **FR-047**: Vínculos posteriores MUST preservar o snapshot original e registrar auditoria da alteração.
- **FR-048**: Associação automática MUST ocorrer somente quando houver correspondência única e normalizada segundo regra aprovada.
- **FR-049**: Associações ambíguas MUST exigir decisão manual.
- **FR-050**: A partida MAY ser vinculada a um draft ou montagem existente, mas MUST poder existir sem esse vínculo.
- **FR-051**: O sistema MUST impedir vínculo silencioso quando a composição divergir do draft e MUST apresentar diferenças relevantes.
- **FR-052**: Correções manuais de dados importados MUST exigir permissão, confirmação contextual, motivo e auditoria.

#### Consulta e estatísticas

- **FR-053**: O sistema MUST substituir o placeholder de partidas por listagem paginada e consulta de detalhes.
- **FR-054**: A listagem MUST permitir identificar data, resultado, duração, times e origem da partida.
- **FR-055**: Os detalhes MUST apresentar todos os dados de usuário disponíveis sem expor campos operacionais internos.
- **FR-056**: A interface MUST diferenciar dados indisponíveis de valores zero.
- **FR-057**: O sistema MUST calcular estatísticas agregadas somente com partidas confirmadas e participantes vinculados.
- **FR-058**: Agregados MUST incluir ao menos partidas, vitórias, derrotas, win rate, KDA, médias de kills, deaths, assists, farm, ouro, dano e visão.
- **FR-059**: Agregados SHOULD incluir campeões e rotas mais usados quando houver dados suficientes.
- **FR-060**: Alterações de vínculo ou estado da partida MUST refletir nos agregados sem duplicação.
- **FR-061**: A estratégia de agregação MUST permitir reconstrução a partir das partidas confirmadas.

#### Fallback manual

- **FR-062**: O cadastro manual de partida MUST continuar disponível sem League Client ou coletor.
- **FR-063**: O sistema MUST permitir completar manualmente campos opcionais ausentes quando autorizado.
- **FR-064**: Dados manuais MUST registrar origem, usuário e momento.
- **FR-065**: A indisponibilidade do LCU MUST NOT impedir consulta, cadastro manual ou estatísticas já persistidas.

#### Envio direto e automação incremental

- **FR-066**: Em incremento posterior, o sistema MAY permitir envio direto mediante código temporário de uso único.
- **FR-067**: Código temporário MUST ter validade curta, escopo, consumo atômico e armazenamento seguro.
- **FR-068**: O coletor MUST NOT armazenar credencial permanente do Rinha para o fluxo de código temporário.
- **FR-069**: Falha no envio direto MUST preservar a opção de exportar arquivo.
- **FR-070**: Em incremento posterior, detecção de fim de jogo MAY sugerir exportação, mas MUST exigir confirmação antes do envio.
- **FR-071**: Automação MUST deduplicar sugestões e MUST NOT remover o fluxo por Game ID.

#### Segurança, privacidade, observabilidade e i18n

- **FR-072**: O backend MUST tratar arquivos importados como entrada não confiável.
- **FR-073**: O sistema MUST proteger contra payload excessivo, profundidade abusiva, overflow, valores inválidos e conteúdo adulterado.
- **FR-074**: O backend MUST NOT realizar SSRF, leitura de caminho local ou download baseado em valores do arquivo.
- **FR-075**: Logs e métricas MUST NOT conter senha do LCU, tokens, arquivo completo, URL assinada ou PII desnecessária.
- **FR-076**: Partidas personalizadas MUST permanecer restritas ao público autorizado e exposição ampliada MUST respeitar consentimento aplicável.
- **FR-077**: O sistema MUST registrar a confirmação de que a partida pertence ao contexto da comunidade ou possui autorização adequada para compartilhamento.
- **FR-078**: A feature MUST NOT coletar ou exibir informação durante a partida que gere vantagem competitiva.
- **FR-079**: O sistema MUST possuir métricas de importações iniciadas, validadas, confirmadas, rejeitadas, duplicadas, conflitantes e parcialmente vinculadas.
- **FR-080**: Eventos técnicos MUST possuir correlation ID entre upload, validação e confirmação.
- **FR-081**: Todo texto novo do frontend MUST usar chaves sincronizadas em `pt.json` e `en.json`.
- **FR-082**: Toda mensagem nova do backend e coletor MUST usar recursos localizados em português e inglês.
- **FR-083**: Português brasileiro MUST usar acentuação correta.
- **FR-084**: Fluxos de upload, prévia, confirmação, erro e vazio MUST funcionar por teclado e respeitar o design system existente.

### Key Entities *(include if feature involves data)*

- **Partida**: agregado que representa um jogo concluído, sua identificação externa, origem, estado, metadados, times e resultado.
- **PartidaTime**: lado participante da partida, com resultado, objetivos e indicadores de primeiros eventos.
- **PartidaParticipante**: snapshot de uma pessoa na partida, com Riot ID, jogador interno opcional, campeão, rota, spells e estatísticas.
- **PartidaBan**: campeão banido por um time e sua ordem no draft.
- **PartidaParticipanteItem**: item de um participante em determinado slot ao final da partida.
- **PartidaParticipanteRuna**: runa de um participante em determinado slot/estilo.
- **ImportacaoPartida**: tentativa de importação, versão de schema, hash, origem, estado, usuário, timestamps e falhas sanitizadas.
- **PreviaImportacaoPartida**: representação temporária validada que aguarda revisão e confirmação.
- **VinculoParticipanteJogador**: associação auditável entre snapshot externo e jogador cadastrado.
- **CodigoImportacao**: autorização temporária e de uso único para envio direto futuro.
- **EstatisticaJogador**: projeção ou consulta derivada de partidas confirmadas e participantes vinculados; não substitui os fatos históricos da partida.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Um organizador com cliente compatível consegue gerar uma exportação válida em até 60 segundos após informar o Game ID.
- **SC-002**: 100% dos arquivos exportados por fixtures conhecidas omitem credenciais, tokens, URLs assinadas e campos proibidos.
- **SC-003**: 100% dos uploads válidos apresentam prévia antes de confirmar a partida.
- **SC-004**: Uploads duplicados e concorrentes do mesmo Match ID produzem no máximo uma partida confirmada.
- **SC-005**: 100% dos payloads inválidos, excessivos, incompatíveis ou adulterados são rejeitados sem persistência parcial de negócio.
- **SC-006**: Participantes com correspondência única de Riot ID são sugeridos corretamente em 100% dos fixtures aprovados.
- **SC-007**: Participantes sem correspondência podem ser preservados em 100% das importações válidas sem bloquear a partida.
- **SC-008**: Usuários autorizados conseguem revisar e confirmar uma partida válida em até 3 minutos.
- **SC-009**: A tela de detalhes reproduz 100% dos campos de usuário presentes nos fixtures aprovados ou os marca explicitamente como indisponíveis.
- **SC-010**: Estatísticas agregadas de um conjunto controlado de partidas correspondem aos cálculos esperados em 100% dos testes.
- **SC-011**: O cadastro manual continua operacional quando o League Client, coletor ou integração local estão indisponíveis.
- **SC-012**: Nenhum segredo do LCU ou payload real aparece no repositório, logs de teste ou mensagens ao usuário.
- **SC-013**: 100% dos endpoints mutáveis da feature possuem testes negativos de autenticação, autorização, schema, tamanho e payload inválido.
- **SC-014**: 100% dos textos novos possuem versões PT/EN sincronizadas e português revisado.
- **SC-015**: Builds e suítes automatizadas do backend, frontend e coletor passam sem exigir League Client instalado no ambiente de CI.
- **SC-016**: Uma mudança simulada de schema LCU falha de modo controlado, preserva fallback manual e produz diagnóstico sem segredo.

## Assumptions

- O primeiro coletor suportará Windows, ambiente no qual o League Client está instalado para o grupo.
- O usuário executa o coletor sob sua própria sessão e já está autenticado no League Client.
- O coletor não solicita nem armazena senha da conta Riot.
- O fluxo de arquivo é o MVP; envio direto e detecção automática são incrementos posteriores.
- O RinhaDasLendas continua sendo uma aplicação interna e autenticada.
- O cadastro atual de Jogador possui Riot ID suficiente para sugerir vínculos, mas a normalização e unicidade serão refinadas no plano.
- Catálogos estáticos podem estar atrasados em relação ao patch; IDs originais permanecem preservados.
- O backend atual permanece fonte de verdade para autorização, confirmação, vínculos e agregados.
- O conteúdo bruto do LCU é tratado como dado não confiável mesmo vindo do computador de um membro.
- A comunidade obterá consentimento adequado para compartilhar estatísticas de partidas personalizadas no ambiente permitido.
- Nenhuma garantia de estabilidade é assumida para endpoints locais não suportados.

## Dependencies

- Autenticação e autorização existentes no RinhaDasLendas.
- Cadastro de jogadores com Riot ID.
- Estrutura existente de backend .NET, frontend Vue, PostgreSQL, i18n e mensagens localizadas.
- Acesso local ao League Client para exportação; não necessário para consulta ou cadastro manual.
- Definição, na fase de planejamento, do contrato versionado e das permissões específicas.

## Out of Scope

- Riot Developer API key, Match-v5, RSO ou Tournament API no MVP.
- Overlay, análise ao vivo, recomendação durante partida ou informação que gere vantagem competitiva.
- Upload de diretórios de logs, logs brutos, tracing, crash dumps ou `lockfile`.
- Parsing de `.rofl` como fonte primária.
- Compatibilidade inicial com macOS ou Linux.
- Publicação pública irrestrita de histórico de custom matches.
- Alteração do cliente do League of Legends ou automação de ações dentro do jogo.
- Armazenamento de credencial permanente do Rinha no coletor MVP.
- Microsserviço separado para importação.
- Garantia de compatibilidade com todo patch futuro sem atualização do coletor/mapeador.

## Risks

- Endpoints locais podem mudar ou desaparecer sem aviso.
- Certificado/TLS local pode variar conforme versão do Windows e cliente.
- Riot IDs podem mudar, divergir em capitalização ou não ser únicos na base atual.
- Payloads podem variar entre custom game, remake, surrender e modos diferentes.
- Exposição inadequada de custom matches pode violar expectativa de privacidade ou política aplicável.
- Estatísticas agregadas podem ficar incorretas se vínculos ou idempotência forem mal definidos.
- Um coletor distribuído amplia superfície de suporte e atualização.

## Open Questions for Clarification

- Qual papel/permissão poderá importar, confirmar e corrigir partidas?
- A prévia terá expiração automática? Em quanto tempo?
- O arquivo será assinado pelo coletor ou apenas validado/hasheado pelo backend no MVP?
- Quais modos além de `CUSTOM_GAME` e mapa 11 serão aceitos inicialmente?
- Partidas com menos de dez jogadores devem ser aceitas como remake ou rejeitadas?
- Qual regra exata vinculará Riot ID ao Jogador existente?
- O vínculo com DraftMontagem deve exigir composição idêntica ou aceitar substituições auditadas?
- Quais campos manuais poderão corrigir dados importados após confirmação?
- Por quanto tempo manter registros de importações rejeitadas e payload bruto sanitizado, se houver?
- A página de estatísticas ficará restrita aos membros autenticados ou terá algum modo público opt-in?
- O envio direto com código temporário fará parte da primeira entrega ou de uma segunda entrega da feature?

