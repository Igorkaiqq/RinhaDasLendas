# Backlog do domínio competitivo

## Uso

Histórias estão agrupadas pela primeira feature capaz de entregá-las. Cada item precisa ser refinado em especificação própria antes de implementação. Critérios abaixo são exemplos executáveis de aceitação, não tarefas técnicas.

## 023 - Fundação competitiva sazonal

### US-023-01 - Criar temporada

Como SuperAdmin ou Presidente, quero criar uma Season com período definido para organizar competições e registros oficiais.

**Critérios de aceitação**

- **Given** SuperAdmin ou Presidente e dados válidos, **When** cria uma Season, **Then** ela é registrada em estado inicial, versionada e auditada.
- **Given** Admin ou papel inferior, **When** tenta criar uma Season, **Then** recebe `403` e nenhum registro é criado.
- **Given** VicePresidente sem condição fina aprovada, **When** tenta criar uma Season, **Then** recebe `403` e nenhum registro é criado.
- **Given** período inválido, **When** a criação é enviada, **Then** a validação localizada recusa a operação.

### US-023-02 - Operar ciclo da temporada

Como SuperAdmin ou Presidente, quero abrir e encerrar uma Season para controlar quando operações oficiais são aceitas.

**Critérios de aceitação**

- **Given** uma Season válida ainda não aberta e SuperAdmin ou Presidente, **When** a abertura é confirmada, **Then** o estado muda uma vez e o evento correspondente é registrado.
- **Given** duas confirmações concorrentes, **When** a primeira conclui, **Then** a segunda converge sem duplicar evento ou recebe conflito de versão.
- **Given** uma temporada encerrada, **When** uma nova partida oficial é solicitada, **Then** a operação é recusada.

### US-023-03 - Configurar formato competitivo

Como Presidente, quero configurar o formato para que séries e eventos apliquem regras coerentes.

**Critérios de aceitação**

- **Given** uma Série `DiariaTemporaria`, `ConfrontoOficial` ou `Amistoso` direta entre dois lados e fora de Evento, **When** Fearless é habilitado, **Then** a configuração é aceita e versionada.
- **Given** uma Série `Amistoso` direta com Fearless, **When** Partidas, picks e resultado são confirmados, **Then** o bloqueio Fearless é operado sem criar votação, Score, Rating, ranking, recorde ou qualquer projeção oficial.
- **Given** um Evento com quatro times ou qualquer Série pertencente a ele, **When** Fearless é solicitado, **Then** a validação recusa a configuração sem persistência parcial.
- **Given** um Evento com quatro times, **When** suas Séries são consultadas, **Then** o Evento aparece como contexto separado e todas usam draft padrão.
- **Given** regras publicadas, **When** uma nova versão é criada, **Then** partidas anteriores preservam a versão usada.

### US-023-04 - Operar série mínima

Como organizador, quero registrar a Série, suas Partidas, resultado e picks mínimos para operar o confronto e o Fearless antes das estatísticas detalhadas.

**Critérios de aceitação**

- **Given** uma Série criada, **When** seu tipo é confirmado, **Then** o discriminador é exatamente `DiariaTemporaria`, `ConfrontoOficial` ou `Amistoso` e nunca `Evento`.
- **Given** qualquer Série oficial, **When** `competicaoId` ou `rodadaId` não pertence à Season da Série, **Then** a criação é recusada.
- **Given** uma Série `DiariaTemporaria`, **When** é criada sem `competicaoId`, `rodadaId`, data local, dois capitães, origem `DraftMontagem` ou dois lados temporários, **Then** a criação é recusada.
- **Given** Partidas válidas com placar, resultado e picks confirmados, **When** a Série é consultada, **Then** esses fatos mínimos e a versão de regras são preservados.
- **Given** campeão confirmado por qualquer lado em série Fearless, **When** um jogo posterior tenta reutilizá-lo, **Then** o pick é recusado para ambos os lados.
- **Given** nova série, **When** ela começa, **Then** não herda bloqueios Fearless da série anterior.

### US-023-05 - Aplicar nova hierarquia

Como responsável pela plataforma, quero policies explícitas para separar autoridade técnica e esportiva.

**Critérios de aceitação**

- **Given** os papéis cadastrados, **When** a hierarquia é consultada, **Then** a ordem é `SuperAdmin > Presidente > VicePresidente > Admin > Moderador > Capitão > Jogador`.
- **Given** referência a `Admin+`, **When** a policy é avaliada, **Then** somente SuperAdmin, Presidente, VicePresidente e Admin são incluídos.
- **Given** uma capacidade esportiva, **When** o SuperAdmin não a possui explicitamente, **Then** a hierarquia técnica não concede acesso.

### US-023-06 - Consultar recortes sazonais

Como usuário, quero consultar a Season atual, várias Seasons ou todas sem perder o contexto das versões.

**Critérios de aceitação**

- **Given** uma consulta sazonal sem `temporadaIds` e sem `todas`, **When** ela é executada, **Then** usa somente a Season atual.
- **Given** calendário sem Season ativa, **When** uma listagem sazonal omite `temporadaIds` e `todas`, **Then** retorna coleção vazia, `calendarioConfigurado=false` e `temporadaAtual=null`, sem consultar todas as Seasons.
- **Given** `temporadaIds` repetido, **When** a consulta é executada, **Then** inclui somente as Seasons informadas e identifica Season e versões em cada resultado.
- **Given** `todas=true` junto com `temporadaIds`, **When** a consulta é enviada, **Then** retorna validação sem executar a busca.
- **Given** `/temporadas/{temporadaId}/competicoes` ou `/temporadas/{temporadaId}/elencos`, **When** `temporadaIds` ou `todas` é informado, **Then** retorna validação porque a rota é exclusivamente single-season.
- **Given** consulta multiseason de competições ou elencos, **When** o usuário informa a seleção sazonal, **Then** usa `/competicoes` ou `/elencos` e identifica cada Season e versão.
- **Given** detalhe ou auditoria por ID de recurso histórico, **When** é consultado por ator autorizado, **Then** retorna a Season do recurso sem aplicar a Season atual como filtro implícito.

## 024 - Elencos, escalações e transferências

### US-024-01 - Registrar elenco sazonal

Como organizador autorizado, quero registrar titulares e reservas por vigência para saber quem representa o time.

**Critérios de aceitação**

- **Given** capitão titular, cinco titulares com uma rota cada e até três reservas, **When** o elenco com no máximo oito jogadores é confirmado, **Then** composição, funções e vigência são registradas.
- **Given** capitão que não é titular, quantidade diferente de cinco titulares, mais de três reservas ou mais de oito jogadores, **When** o elenco é confirmado, **Then** a operação é recusada.
- **Given** sobreposição para o mesmo jogador, **When** a inclusão é tentada, **Then** a operação é recusada e o elenco anterior permanece íntegro.
- **Given** consulta em data passada, **When** o elenco é exibido, **Then** a composição vigente naquela data é retornada.

### US-024-02 - Confirmar escalação

Como Capitão, quero confirmar a escalação do meu time para uma partida.

**Critérios de aceitação**

- **Given** Capitão vinculado ao lado e janela aberta, **When** confirma jogadores elegíveis, **Then** a escalação é capturada para a partida.
- **Given** escalação sem cinco jogadores, com rota duplicada ou jogador fora do elenco vigente, **When** a confirmação é enviada, **Then** a operação é recusada.
- **Given** Capitão de outro time, **When** tenta alterar a escalação, **Then** recebe bloqueio sem modificar dados.
- **Given** transferência posterior, **When** a partida histórica é consultada, **Then** a escalação original permanece igual.

### US-024-03 - Transferir jogador

Como autoridade autorizada, quero movimentar um jogador com vigência para manter elencos corretos.

**Critérios de aceitação**

- **Given** solicitação válida, **When** é aprovada, **Then** origem e destino mudam atomicamente na data de vigência.
- **Given** janela configurada fechada, **When** uma contratação sem exceção aprovada é solicitada, **Then** ela é recusada e nenhum vínculo muda.
- **Given** mesma chave idempotente e mesmo conteúdo, **When** a solicitação é repetida, **Then** o resultado original é retornado sem nova movimentação.
- **Given** mesma chave idempotente e conteúdo divergente, **When** a solicitação é repetida, **Then** recebe `409` e a movimentação original permanece intacta.
- **Given** regra de exceção ainda não aprovada, **When** a transferência depende dela, **Then** a operação é recusada.

### US-024-04 - Acompanhar qualificação global

Como organizador, quero acompanhar as duas Séries diárias exigidas para saber se o jogador possui qualificação global histórica.

**Critérios de aceitação**

- **Given** nenhuma Série diária válida, **When** o progresso do jogador é consultado, **Then** exibe `0/2` e qualificação pendente.
- **Given** a primeira série `DiariaTemporaria` oficial concluída e confirmada, **When** ela é processada, **Then** o progresso passa uma única vez para `1/2`.
- **Given** a segunda Série diária oficial concluída e confirmada em qualquer período histórico, **When** ela é processada, **Then** o progresso passa para `2/2` e a qualificação permanece verdadeira.
- **Given** jogador qualificado em `2/2`, **When** uma solicitação de contratação, transferência ou vínculo é criada, aprovada, rejeitada ou cancelada, **Then** o progresso não é reservado, decrementado nem consumido.
- **Given** amistoso, Série cancelada ou anulada, **When** é processada, **Then** o progresso não avança.

## 025 - Partidas e estatísticas

### US-025-01 - Detalhar partida e participantes

Como analista, quero enriquecer a Partida mínima da feature 023 com participantes e dados detalhados sem duplicar sua Série ou resultado.

**Critérios de aceitação**

- **Given** Partida mínima existente na Série, **When** participantes e estatísticas são importados, **Then** `partidaId`, `serieId`, placar, resultado, picks e versão de regras são preservados.
- **Given** payload detalhado, **When** é consolidado, **Then** snapshots, estatísticas, objetivos e demais campos disponíveis ficam vinculados aos participantes corretos.
- **Given** tentativa de criar outra série ou resultado durante o enriquecimento, **When** o comando é validado, **Then** a operação é recusada.
- **Given** partida amistosa, **When** seus detalhes são consolidados, **Then** permanece em recorte amistoso e não altera projeção oficial.
- **Given** uma Série amistosa com várias Partidas consolidadas, **When** estatísticas e resultados agregados são processados, **Then** nenhuma Partida nem o agregado da Série altera Score, Rating, MVP/SVP, rankings ou recordes oficiais.

### US-025-02 - Importar dados com idempotência

Como Admin, quero importar dados de partida para reduzir digitação sem criar duplicidades.

**Critérios de aceitação**

- **Given** payload válido e chave nova, **When** a importação é enviada, **Then** um rascunho normalizado é criado para revisão.
- **Given** mesma chave e mesmo payload, **When** o envio é repetido, **Then** a importação original é retornada como repetida.
- **Given** mesma chave e payload diferente, **When** o envio é repetido, **Then** recebe `409` e nenhum dado é sobrescrito.

### US-025-03 - Coletar com credencial mínima

Como serviço Collector, quero enviar uma coleta e acompanhar seu estado sem acessar o restante da plataforma.

**Critérios de aceitação**

- **Given** credencial com `collector:match-import:create`, **When** envia coleta válida, **Then** recebe somente identificador e estado.
- **Given** a mesma credencial, **When** consulta importação própria, **Then** recebe o estado permitido.
- **Given** importação de outra credencial, **When** o Collector tenta consultá-la, **Then** o recurso não é exposto.

### US-025-04 - Consolidar estatísticas oficiais

Como autoridade autorizada, quero consolidar uma versão validada para alimentar estatísticas oficiais.

**Critérios de aceitação**

- **Given** partida oficial encerrada e dados validados, **When** a consolidação é confirmada, **Then** projeções oficiais são atualizadas uma vez.
- **Given** partida amistosa, **When** a consolidação é processada, **Then** ela aparece apenas no histórico não oficial e não altera ranking.
- **Given** falha antes do commit, **When** a operação termina, **Then** partida, auditoria, outbox e projeções permanecem no estado anterior.

### US-025-05 - Consultar estatísticas

Como jogador, quero consultar desempenho por temporada para entender resultados comparáveis.

**Critérios de aceitação**

- **Given** temporada com partidas oficiais, **When** a tela é aberta, **Then** mostra amostra, atualização e metodologia do recorte.
- **Given** histórico contendo amistosos, **When** o ranking oficial é calculado, **Then** nenhum amistoso participa dos valores.
- **Given** ausência de amostra suficiente segundo regra publicada, **When** o jogador é exibido, **Then** aparece como não elegível, sem posição enganosa.

### US-025-06 - Comparar jogadores

Como usuário, quero comparar de dois a quatro jogadores no mesmo recorte.

**Critérios de aceitação**

- **Given** jogadores e recortes compatíveis, **When** a comparação é solicitada, **Then** valores, amostras e referência da função aparecem juntos.
- **Given** recortes incompatíveis, **When** a comparação é solicitada, **Then** o sistema explica o ajuste necessário.
- **Given** viewport móvel, **When** indicadores são consultados, **Then** todos os jogadores permanecem comparáveis sem rolagem horizontal da página.

### US-025-07 - Corrigir partida consolidada

Como revisor autorizado, quero corrigir dados preservando proveniência e efeitos anteriores.

**Critérios de aceitação**

- **Given** versão consolidada e correção válida, **When** a revisão é confirmada, **Then** nova versão e justificativa são registradas.
- **Given** versão antiga no request, **When** a correção é enviada, **Then** recebe `409` sem sobrescrita.
- **Given** projeções derivadas, **When** a correção conclui, **Then** elas são reconstruídas ou compensadas de forma idempotente.

## 026 - Votação MVP/SVP

### US-026-01 - Abrir votação elegível

Como organizador autorizado, quero abrir uma única votação após série oficial concluída e validada.

**Critérios de aceitação**

- **Given** série oficial concluída, validada e com vencedor, **When** a votação é aberta, **Then** cria uma única votação, fixa participantes eleitores, candidatos a MVP do lado vencedor e candidatos a SVP do lado derrotado.
- **Given** outra partida da mesma série, **When** uma votação própria é solicitada, **Then** a operação é recusada.
- **Given** série amistosa, **When** a abertura é solicitada, **Then** a votação oficial é recusada.
- **Given** votação já aberta, **When** o comando é repetido, **Then** nenhum segundo evento é criado.

### US-026-02 - Registrar voto privado

Como eleitor elegível, quero escolher MVP e SVP sem revelar meu voto.

**Critérios de aceitação**

- **Given** eleitor que participou de ao menos uma Partida válida e janela aberta, **When** confirma uma escolha por categoria e turno, **Then** o voto é registrado uma vez.
- **Given** participante escolhe a si próprio em categoria para a qual é candidato, **When** confirma o voto, **Then** o autovoto é aceito.
- **Given** integrante do elenco que não participou, **When** tenta votar, **Then** recebe bloqueio e nenhum voto é criado.
- **Given** candidato do lado incompatível com MVP ou SVP, **When** o voto é enviado, **Then** a validação recusa a escolha.
- **Given** ator informa outro `userId`, **When** envia o voto, **Then** a identidade do principal prevalece e impersonation é impedida.
- **Given** usuário não elegível, **When** tenta votar, **Then** nenhum voto ou parcial é exposto.

### US-026-03 - Resolver turnos e empate

Como participante, quero uma apuração previsível para entender como empate se transforma em segundo turno ou co-vencedores.

**Critérios de aceitação**

- **Given** empate no primeiro lugar de uma categoria, **When** o primeiro turno é encerrado, **Then** abre segundo turno somente entre empatados e não transporta os votos anteriores.
- **Given** empate persistente no segundo turno, **When** a apuração ocorre, **Then** compara a média de Score elegível dos candidatos nas Partidas da Série usando a mesma versão.
- **Given** médias exatamente iguais ou comparação inconclusiva, **When** a apuração termina, **Then** todos os candidatos empatados são publicados como co-vencedores.

### US-026-04 - Consultar votos para moderação

Como integrante de Admin+, quero consultar votos individuais da Série para moderar ocorrências com acesso mínimo e auditado.

**Critérios de aceitação**

- **Given** SuperAdmin, Presidente, VicePresidente ou Admin com `CanModerateSeriesVotes`, **When** consulta os votos da Série, **Then** recebe `votoId` opaco, categoria, turno, candidato, estado e identidade mínima do eleitor.
- **Given** consulta administrativa concluída, **When** a resposta é emitida, **Then** ator, Série, instante, correlação e finalidade de moderação são auditados.
- **Given** Moderador, Capitão, Jogador ou participante comum, **When** tenta consultar votos individuais, **Then** recebe `403` sem identidade do eleitor ou `votoId`.
- **Given** endpoint público de resultado ou votação, **When** é consultado, **Then** não contém voto individual nem identidade do eleitor.

### US-026-05 - Invalidar voto com auditoria

Como integrante de Admin+, quero invalidar um voto indevido com motivo para corrigir a apuração sem apagar o histórico.

**Critérios de aceitação**

- **Given** SuperAdmin, Presidente, VicePresidente ou Admin e motivo válido, **When** invalida um voto, **Then** o voto permanece no histórico, deixa de contar e ator, motivo, turno, categoria e impacto são auditados.
- **Given** Moderador, Capitão ou Jogador, **When** tenta invalidar voto, **Then** recebe `403` e a apuração não muda.
- **Given** Admin+ sem motivo, **When** solicita invalidação, **Then** recebe validação e o voto permanece válido.
- **Given** Admin+ informa identidade do eleitor no lugar do `votoId` opaco, **When** solicita invalidação, **Then** recebe validação e nenhum voto é alterado.
- **Given** invalidação que altera empate ou resultado publicado, **When** a reapuração conclui, **Then** aplica as mesmas regras de turno e publica nova versão auditável.

### US-026-06 - Publicar resultado

Como autoridade autorizada, quero publicar o resultado após o encerramento.

**Critérios de aceitação**

- **Given** turnos encerrados, **When** o resultado da série é publicado, **Then** vencedores ou co-vencedores, critério e participação agregada ficam disponíveis.
- **Given** janela aberta, **When** a publicação é solicitada, **Then** a operação é recusada.
- **Given** consulta pública autenticada, **When** o resultado é exibido, **Then** autoria e escolhas individuais não aparecem.

## 027 - Score e rating

### US-027-01 - Calcular rating por partida

Como jogador, quero que partidas oficiais válidas atualizem meu rating de forma reproduzível.

**Critérios de aceitação**

- **Given** Partida oficial consolidada e parâmetros publicados, **When** o cálculo ocorre, **Then** cada lançamento-base referencia `partidaId` e não usa `serieId` como origem substituta.
- **Given** uma Série com ajuste aplicável, **When** o cálculo conclui, **Then** o ajuste referencia `serieId`, mantém `partidaId` vazio e é separado dos lançamentos-base de cada Partida.
- **Given** partida amistosa, **When** consumidores processam sua consolidação, **Then** nenhum lançamento oficial é criado.
- **Given** mesmo evento processado novamente, **When** o consumidor executa, **Then** nenhum lançamento é duplicado.

### US-027-02 - Compensar correção

Como responsável técnico, quero compensar rating após correção sem apagar histórico.

**Critérios de aceitação**

- **Given** partida corrigida com efeitos anteriores, **When** o recálculo é executado, **Then** lançamentos compensatórios preservam os originais.
- **Given** falha parcial, **When** o processamento é retomado, **Then** participantes já concluídos não recebem duplicidade.
- **Given** histórico do jogador, **When** a correção é consultada, **Then** origem e versão do algoritmo são rastreáveis.

### US-027-03 - Consultar ranking e metodologia

Como usuário, quero entender posição e critérios do rating sazonal.

**Critérios de aceitação**

- **Given** classificação publicada, **When** o ranking é aberto, **Then** posição, valor, amostra, elegibilidade e versão são exibidos.
- **Given** parâmetro ainda pendente, **When** a metodologia é consultada, **Then** nenhum valor presumido é apresentado como oficial.
- **Given** conteúdo em português e inglês, **When** a tela é renderizada, **Then** significado, pluralização e layout permanecem equivalentes.

### US-027-04 - Respeitar extremos, elos, piso e queda

Como jogador, quero que Score e Rating respeitem limites publicados para que minha classificação seja previsível.

**Critérios de aceitação**

- **Given** normalização nos extremos, **When** o Score é calculado, **Then** retorna exatamente `0` ou `100` sem ultrapassar a escala.
- **Given** Rating em `99`, `199`, `299`, `399`, `499`, `599`, `699`, `799` ou `899`, **When** o elo é resolvido, **Then** permanece na faixa inferior correspondente.
- **Given** Rating em `100`, `200`, `300`, `400`, `500`, `600`, `700`, `800` ou `900`, **When** o elo é resolvido, **Then** entra na faixa superior correspondente.
- **Given** perda maior que o saldo, **When** o lançamento é aplicado, **Then** o Rating termina em zero e registra limitação pelo piso.
- **Given** vitória elegível, **When** a variação nominal é calculada, **Then** permanece entre `+10` e `+20` inclusive.
- **Given** derrota elegível, **When** a variação nominal é calculada, **Then** permanece entre `-20` e `-5` inclusive.
- **Given** variação nominal negativa maior que o saldo disponível, **When** ela é aplicada, **Then** a variação aplicada é limitada pelo piso sem alterar o valor nominal auditado.
- **Given** Série perdida com ajuste de Série, **When** todos os lançamentos são somados, **Then** o saldo líquido da Série para o jogador é menor ou igual a zero.
- **Given** queda de qualquer limite para um ponto abaixo, **When** o lançamento é aplicado, **Then** o elo inferior é atribuído sem proteção implícita.

## Evolução posterior

### US-FUT-01 - Perfil competitivo consolidado

Como jogador, quero um perfil que reúna temporadas, times e marcos sem misturar identidades de conta e jogo.

**Critérios de aceitação**

- **Given** jogador com múltiplas temporadas, **When** o perfil é consultado, **Then** cada recorte preserva time, função e regras históricas.
- **Given** usuário sem jogador vinculado, **When** acessa perfil competitivo, **Then** recebe orientação localizada sem dados de terceiros.

### US-FUT-02 - Ampliar fontes de coleta

Como operador, quero adicionar adaptadores de coleta sem acoplar o domínio ao provedor.

**Critérios de aceitação**

- **Given** nova fonte, **When** seu adaptador normaliza uma partida, **Then** o domínio recebe o mesmo contrato canônico das fontes existentes.
- **Given** provedor indisponível, **When** a coleta falha, **Then** operação manual continua disponível.

### US-FUT-03 - Inteligência global explicável

Como organizador, quero tendências sobre dados confiáveis para apoiar decisões sem automatizar autoridade esportiva.

**Critérios de aceitação**

- **Given** insight produzido, **When** é exibido, **Then** recorte, amostra, fonte e limitações são visíveis.
- **Given** recomendação automatizada, **When** um resultado oficial é operado, **Then** a recomendação não substitui autorização nem fonte de verdade.
