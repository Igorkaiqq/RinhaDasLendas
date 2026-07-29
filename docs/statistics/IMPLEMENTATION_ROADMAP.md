# Roadmap de implementação competitiva

## Direção

Construir uma base sazonal rastreável antes de estatísticas e rating. Cada feature deve seguir Constituição, Specify, Plan, Tasks e Implement, com aprovação entre fases. Este documento ordena entregas; não autoriza implementação nem substitui especificações.

## Princípios de sequência

- Dados oficiais só existem dentro de temporada e competição definidas.
- Elencos e escalações antecedem partidas para preservar identidade histórica.
- Partidas validadas antecedem votação e rating.
- Amistosos permanecem consultáveis, mas isolados de todas as projeções oficiais.
- Integrações externas são adaptadores opcionais; operação manual continua possível.
- Cada etapa entrega uma fatia utilizável e migrável, sem big bang.

## 023 - Fundação competitiva sazonal

**Objetivo:** entregar a fundação sazonal e a operação mínima necessária para registrar séries competitivas antes do modelo estatístico detalhado.

Entregas:

- Seasons, competição e rodada, sempre com versão identificável;
- Série operacional mínima com discriminadores `DiariaTemporaria`, `ConfrontoOficial` e `Amistoso`;
- Evento como contexto separado que agrupa Séries;
- `competicaoId` e `rodadaId` da mesma Season obrigatórios em toda Série oficial, inclusive `DiariaTemporaria`;
- Partidas mínimas agrupadas pela Série, placar, resultado e picks confirmados suficientes para operar Fearless;
- Série direta `DiariaTemporaria`, `ConfrontoOficial` ou `Amistoso` com bloqueio Fearless bilateral e reconstruível, sempre fora de Evento;
- Evento de quatro times impondo draft padrão a todas as Séries pertencentes;
- hierarquia atualizada de papéis e policies iniciais;
- separação entre autoridade técnica e esportiva;
- contratos básicos de Season, competição, rodada, série, resultado e picks mínimos;
- auditoria, concorrência e eventos de domínio fundamentais;
- estrutura de i18n e navegação para as telas futuras.

Critério de saída:

- SuperAdmin e Presidente podem criar, abrir e encerrar Season; VicePresidente depende apenas de sua decisão fina e Admin abaixo é negado;
- sem Season ativa, listagens por omissão retornam vazias com `calendarioConfigurado=false` e `temporadaAtual=null`, sem fallback histórico;
- uma Série mínima registra lados e agrupa Partidas individuais com placar, resultado e picks sem depender do modelo estatístico detalhado da 025;
- formatos inválidos são recusados;
- Série diária exige competição, rodada, data local, capitães, origem `DraftMontagem` e lados temporários;
- em 100% dos Eventos de quatro times, o Evento não é tratado como Série e todas as Séries internas rejeitam Fearless e usam draft padrão;
- matriz negativa de autorização passa;
- nenhum parâmetro pendente em `OPEN_DECISIONS.md` é tratado como definitivo.

## 024 - Elencos, escalações e transferências

**Objetivo:** representar quem pertence a cada time e quem participou de cada partida em um período.

Entregas:

- elenco sazonal com vigência;
- capitão titular, cinco titulares por rota, até três reservas e máximo de oito vínculos;
- vínculo oficial ativo único por jogador;
- janelas de movimentação configuradas e aplicadas em `America/Sao_Paulo`;
- escalação capturada por partida;
- solicitação, aprovação e rejeição de transferência;
- qualificação global histórica após duas Séries diárias oficiais concluídas, com progresso `0/2..2/2` permanente e não consumível;
- histórico imutável de movimentações;
- validação de sobreposição e concorrência;
- tela responsiva de elencos e transferências.

Dependência: 023.

Critério de saída:

- transferências futuras não alteram partidas anteriores;
- capitão só opera o próprio time quando autorizado;
- composição inválida, vínculo simultâneo, janela fechada ou qualificação global inferior a duas Séries são recusados, sem consumir o progresso quando uma solicitação é criada;
- exceções não aprovadas são recusadas;
- fixtures cobrem mudanças de vigência e conflitos.

## 025 - Partidas e estatísticas

**Objetivo:** detalhar Partidas já fundadas em 023, seus participantes e dados técnicos, importar fontes e produzir projeções estatísticas confiáveis sem recriar Season, Série, resultado ou Fearless.

Entregas:

- modelo detalhado de partida e participantes, com snapshots, estatísticas, objetivos, itens, runas e timeline quando disponíveis;
- enriquecimento das Partidas mínimas da 023, preservando `partidaId`, `serieId`, resultado, picks e versão de regras;
- histórico separado de partidas oficiais e amistosas;
- fluxo administrativo de importação e revisão;
- credencial Collector de escopo mínimo;
- idempotência, outbox, inbox e proveniência;
- estatísticas de jogador, time, partida e comparação;
- telas responsivas de Estatísticas, Jogador, Time, Partida, Comparação e importação;
- golden fixtures de fontes suportadas.

Dependências: 023 e 024.

Critério de saída:

- amistoso não aparece em nenhum total ou projeção oficial;
- Amistoso direto pode operar Fearless sem habilitar votação, Score, Rating, ranking ou recorde oficial;
- dados detalhados preservam o Fearless já validado na 023 sem recalculá-lo por regra paralela;
- importação repetida não duplica partida;
- correção preserva histórico e reconstrói projeções afetadas.

## 026 - Votação MVP/SVP

**Objetivo:** permitir uma votação de reconhecimento por série concluída e validada, com elegibilidade, turnos, moderação e publicação controlada.

Entregas:

- uma única votação vinculada à série, nunca uma votação independente por partida;
- eleitor participante de ao menos uma Partida válida, candidatos separados por lado e autovoto permitido;
- uma escolha por categoria e turno, voto idempotente e privado;
- segundo turno entre empatados, desempate por média de Score e co-vencedores quando a igualdade persistir;
- invalidação de voto por Admin+ com motivo obrigatório, auditoria e reapuração versionada;
- consulta administrativa auditada por Admin+, com `votoId` opaco e identidade mínima do eleitor somente para moderação;
- tela responsiva de votação e resultado;
- auditoria sem exposição de autoria individual.

Dependência: 025.

Critério de entrada adicional: decisão sobre mudança de voto enquanto a janela estiver aberta.

Critério de saída:

- votos parciais e individuais não são expostos;
- concorrência não duplica voto;
- invalidação não apaga o voto nem permite votar em nome de terceiro;
- votos e identidades individuais nunca aparecem em contratos públicos ou caches compartilhados;
- somente resultado publicado alimenta telas e eventos posteriores.

## 027 - Score e rating

**Objetivo:** criar classificação sazonal explicável, versionada e corrigível.

Entregas:

- lançamentos-base imutáveis por partida oficial;
- ajustes de série em lançamentos separados dos lançamentos por partida;
- cálculo versionado e reproduzível;
- compensação por correção ou invalidação;
- elegibilidade e faixas publicadas;
- ranking e histórico de jogador;
- tela responsiva de Rating e metodologia;
- golden fixtures e propriedades matemáticas.

Dependências: 025 e, quando o algoritmo utilizar resultado de votação, 026.

Critérios de entrada adicionais:

- soft reset exato;
- bônus e reembolso;
- thresholds S-F;
- amostra mínima.

Critério de saída:

- todo valor é reproduzível por versão do algoritmo e conjunto de partidas oficiais;
- Score respeita os extremos 0 e 100; variações nominais de vitória `+10..+20`, derrota `-5..-20`, aplicação no piso, saldo líquido de Série perdida `<= 0`, todos os limites de elo e queda automática possuem testes explícitos;
- correção não apaga lançamento anterior;
- amistosos nunca alteram score ou rating oficial.

## Evolução posterior

### Perfis e times

- enriquecer perfil competitivo e página de time com conquistas, recortes e histórico;
- preservar separação entre conta, jogador e vínculo sazonal;
- melhorar compartilhamento sem expor dados sensíveis.

### Coleta

- ampliar adaptadores de fontes após validar a operação manual;
- automatizar correspondências com revisão humana;
- definir retenção, backfill e eventual Tournament API antes de aumentar dependência externa.

### Inteligência e visão global

- tendências, scouting, equilíbrio e insights somente sobre dados consolidados e explicáveis;
- comparações globais com recorte, amostra e metodologia visíveis;
- nenhum modelo automatizado se torna fonte de verdade para resultado, sanção ou autorização.

## Marcos transversais

| Marco | Evidência necessária |
| --- | --- |
| Arquitetura | eventos, contratos e boundaries revisados |
| Segurança | matriz de papéis, recursos e Collector aprovada |
| Dados | migrations, constraints, proveniência e reconstrução testadas |
| UX | wireframes, estados, responsividade e acessibilidade validados |
| i18n | PT/EN sincronizados e resources backend completos |
| Operação | observabilidade, retries, auditoria e runbook de recuperação |
| Qualidade | suites críticas e golden fixtures aprovadas |

## Fora da sequência

- Não implementar rating antes de partidas oficiais consolidadas.
- Não automatizar coleta antes de existir importação manual auditável.
- Não incluir amistosos para aumentar artificialmente amostra oficial.
- Não ativar Fearless no Evento de quatro times nem em qualquer Série interna.
- Não resolver parâmetros listados em `OPEN_DECISIONS.md` por valor padrão oculto.
