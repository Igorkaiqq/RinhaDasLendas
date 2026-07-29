# Perguntas para a comunidade

## Objetivo

Validar utilidade, compreensão, confiança e governança com jogadores e organizadores antes de fechar as decisões competitivas. As entrevistas não devem prometer funcionalidades nem apresentar alternativas como já aprovadas.

## Orientação para entrevistas

- Separar sessões de jogadores e organizadores para reduzir influência hierárquica.
- Pedir exemplos recentes antes de perguntar por solução ideal.
- Mostrar dados fictícios e anonimizados.
- Registrar papel, experiência e contexto, não identidade desnecessária.
- Evitar perguntar somente “você gostou?”. Solicitar interpretação e próxima ação.
- Não exibir resultado parcial de votação real.

## Perguntas para jogadores

### Estatísticas e perfis

1. Quando você abre suas estatísticas, qual é a primeira pergunta que espera responder?
2. Quais três indicadores ajudam de fato a entender sua atuação na sua função?
3. Que informação costuma parecer importante, mas pode levar a uma conclusão errada sem contexto?
4. Como você espera distinguir partida oficial de amistoso no histórico?
5. Você prefere ver números por partida, por minuto, por rodada ou por temporada? Em quais situações?
6. O que precisa aparecer para você confiar em uma comparação entre jogadores?
7. Como a tela deveria explicar que alguém ainda não possui amostra suficiente?
8. Quais dados do seu perfil deveriam ser públicos para a comunidade e quais deveriam ficar restritos?
9. Ao não escolher uma Season, você entende que a consulta usa a Season atual? Como espera selecionar várias Seasons ou `Todas`?
10. Quando não existe Season ativa, o estado vazio deixa claro que não houve fallback para todo o histórico?

### Times, elencos e transferências

1. Que informação deixa claro qual time e função você representava em uma partida antiga?
2. Como uma transferência pendente deveria aparecer sem causar a impressão de que já foi aprovada?
3. Em quais situações uma transferência fora da janela seria justa?
4. Que histórico de mudanças de elenco você considera necessário para transparência?
5. Como reservas e substituições deveriam aparecer no perfil e nas estatísticas?
6. A composição de capitão titular, cinco titulares por rota, até três reservas e máximo de oito está clara na tela?
7. Como o progresso global histórico `0/2`, `1/2` e `2/2` deveria explicar a qualificação sem sugerir que ela será consumida por uma contratação?

### Votação MVP/SVP

1. Está claro que existe uma votação por série, somente para participantes, com MVP entre o lado vencedor e SVP entre o lado derrotado?
2. Você precisa ver estatísticas antes de votar? Quais, e por quê?
3. Você esperaria poder mudar seu voto enquanto a janela estiver aberta?
4. Que prazo é suficiente para votar sem atrasar a publicação do resultado?
5. O segundo turno, seguido por média de Score e co-vencedores em igualdade, é compreensível?
6. O autovoto permitido muda sua confiança na votação? Por quê?
7. O resultado deve mostrar somente vencedores ou também participação agregada?
8. O que faria você desconfiar da privacidade ou da justiça da votação?
9. Como a interface deveria comunicar que um voto foi invalidado por Admin+ com motivo, sem expor o eleitor?

### Rating

1. O que um rating deveria representar para ser útil: resultado, desempenho, consistência ou combinação desses fatores?
2. Como você espera que uma nova temporada afete seu rating anterior?
3. Quando uma partida anulada ou corrigida deveria devolver pontos?
4. Faixas de S a F ajudam a entender o rating ou criam estigma? Por quê?
5. Quantas partidas parecem necessárias antes de exibir uma posição confiável?
6. Que explicação mínima faria uma variação de rating parecer justa?
7. Você entende melhor o histórico quando o Rating-base de cada partida e os ajustes da série aparecem em lançamentos separados?
8. Como KDA deveria tratar uma Partida sem mortes e como deveria ser agregado em uma Série?
9. Que explicação de força do confronto e impacto individual é necessária para entender a variação?
10. Quais parâmetros internos do Rating precisam ser publicados para gerar confiança sem poluir a tela?

### Experiência e acessibilidade

1. Em qual dispositivo você provavelmente consultará estatísticas durante uma rinha?
2. Que partes de tabelas ou gráficos são difíceis de usar no celular?
3. Você usa teclado, leitor de tela, zoom ou redução de movimento? Onde encontra barreiras hoje?
4. Quais termos competitivos em português ou inglês costumam gerar confusão?

## Perguntas para organizadores

### Temporadas e competições

1. Quais estados uma temporada precisa ter no fluxo real de organização?
2. Está claro que SuperAdmin e Presidente gerenciam Seasons, enquanto Admin abaixo não possui essa capacidade?
3. Em quais condições finas o VicePresidente deveria receber capacidade temporária ou condicionada sobre Seasons?
4. Em quais ações esportivas uma segunda aprovação é necessária?
5. Como vocês organizam uma série direta entre dois lados com Fearless?
6. Quais dados mínimos precisam estar disponíveis para operar resultado e picks antes da importação estatística detalhada?
7. Está claro que Evento é um contexto separado que agrupa Séries e que todas as Séries internas ficam em draft padrão sem Fearless?
8. Para toda Série oficial, competição e rodada da Season são verificáveis?
9. Para uma Série diária, competição, rodada, data local, capitães, origem `DraftMontagem` e lados temporários são suficientes e verificáveis?
10. Que informação de regra precisa permanecer vinculada a partidas antigas?

### Elencos e transferências

1. Como vocês definem janela, vigência e aprovação de transferências hoje?
2. Quais exceções são realmente necessárias e como evitar favorecimento?
3. Quem pode confirmar escalação e até quando?
4. Como tratar jogador sem time, reserva temporário e substituição emergencial?
5. Que conflitos devem bloquear automaticamente uma movimentação?
6. Qual trilha de auditoria é necessária para resolver contestação?
7. Como as janelas configuradas devem aparecer antes de solicitar uma transferência?
8. Que evidência confirma cada uma das duas Séries diárias da qualificação global e deixa claro que elas não são consumidas por solicitações futuras?

### Partidas, importação e coleta

1. Qual fonte de dados é usada hoje e quais campos exigem correção manual com frequência?
2. Como confirmar que duas importações representam a mesma partida?
3. Quem pode importar, revisar e consolidar dados oficiais?
4. Essas três ações precisam ser separadas entre pessoas diferentes?
5. Por quanto tempo dados brutos precisam permanecer disponíveis e com qual finalidade?
6. Qual período histórico vale a pena importar por backfill?
7. Como comunicar incerteza em dados históricos incompletos?
8. A Tournament API resolveria qual problema concreto que o fluxo manual não resolve?
9. Quanto tempo uma falha de coleta pode aguardar antes de exigir intervenção?

### Estatísticas, score e rating

1. Quais indicadores são necessários para decisões esportivas e quais são apenas curiosidade?
2. Como evitar que jogadores manipulem amostra, função ou adversário para melhorar posição?
3. Qual amostra mínima parece justa para ranking e comparação?
4. Como deve funcionar o soft reset entre temporadas?
5. Em quais situações deve existir bônus ou reembolso?
6. Como thresholds S-F deveriam ser definidos e revisados?
7. Quem pode solicitar recálculo e quem aprova sua publicação?
8. Como comunicar uma correção que altere ranking já publicado?

### Votação e governança

1. A regra de eleitores participantes e candidatos separados por lado é verificável com os dados da série?
2. Mudança de voto deve ser permitida enquanto o turno estiver aberto?
3. Segundo turno, média de Score e co-vencedores são auditáveis com os dados disponíveis?
4. Quem abre, encerra e publica o resultado?
5. Como Admin+ deve justificar e auditar a invalidação de um voto?
6. Como prevenir pressão, combinação de votos ou exposição indevida, inclusive com autovoto permitido?
7. Que dados agregados podem ser publicados sem comprometer privacidade?
8. Em uma investigação de moderação, quais dados mínimos do eleitor são realmente necessários e como o acesso deveria ser comunicado e auditado?

### Operação e segurança

1. Quem precisa acessar a tela administrativa de importação?
2. Quais incidentes exigem atuação técnica do SuperAdmin e quais permanecem decisão esportiva do Presidente?
3. Como credenciais de coleta devem ser emitidas, rotacionadas e revogadas?
4. Que alertas e relatórios ajudam a detectar duplicidade, atraso ou dados inconsistentes?
5. Qual procedimento manual deve continuar disponível quando integração externa falhar?

### Interface

1. Que informação deve aparecer primeiro nas telas de Estatísticas, Partida e Time?
2. Quais ações precisam estar disponíveis no celular durante a organização?
3. A área competitiva precisa de assinatura visual adicional ou o design atual já comunica o contexto?
4. Quais estados vazios, avisos ou confirmações evitariam erros operacionais?

## Exercícios de validação

### Ordenação de prioridades

Entregar cartões com: temporada, elenco, escalação, partida, importação, estatísticas, votação e rating. Pedir que a pessoa ordene pelo que precisa existir primeiro e explique dependências.

### Leitura de wireframe

Mostrar um wireframe por vez e perguntar:

1. O que aconteceu?
2. O que você pode fazer?
3. Qual é a próxima ação?
4. Que informação está faltando?

### Cenários de confiança

Apresentar três casos fictícios: amistoso no histórico, partida oficial corrigida e transferência pendente. Pedir que a pessoa identifique o que conta para estatística, quem representa qual time e qual dado ainda não é definitivo.

## Registro dos resultados

Para cada sessão, consolidar:

- problemas observados, não apenas pedidos de solução;
- frequência e impacto;
- diferenças entre jogadores e organizadores;
- termos que exigem revisão de i18n;
- riscos de privacidade, coerção ou favorecimento;
- evidências relacionadas às decisões de `OPEN_DECISIONS.md`.

Decisões finais devem ser registradas em especificação ou ADR apropriado, com data, responsáveis e consequências.
