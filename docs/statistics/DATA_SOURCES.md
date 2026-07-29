# Fontes de dados para partidas e estatísticas

## Visão geral

Nenhuma fonte isolada atende a todos os objetivos do RinhaDasLendas. Fontes oficiais oferecem maior previsibilidade e conformidade, mas exigem credenciais, registro e, em alguns casos, RSO ou fluxo de torneio. O LCU demonstrou riqueza de dados pós-partida sem chave de desenvolvedor, porém a própria Riot o classifica como não oficialmente suportado para terceiros. Entrada manual continua necessária por regra constitucional.

As classificações detalhadas por métrica estão em [Matriz de disponibilidade](FIELD_AVAILABILITY_MATRIX.md). Regras de política e segurança estão em [Conformidade Riot e LCU](RIOT_LCU_COMPLIANCE.md).

## Comparativo

| Fonte | Natureza e suporte | Momento | Dados úteis | Limitações principais | Papel recomendado |
|---|---|---|---|---|---|
| Riot API | Oficial e documentada | Conta, catálogo e dados conforme endpoint | Identidade, dados de invocador e serviços do ecossistema | Chave, rate limit, registro e políticas | Integração oficial futura quando aprovada |
| Match V5 | Oficial e documentada | Pós-partida e timeline | Metadados, participantes, placar, desafios, objetivos, itens, runas e eventos de timeline conforme resposta | Chave; roteamento regional; custom match tem restrições de acesso e exibição | Fonte preferencial futura para partidas elegíveis |
| Tournament API | Oficial e documentada | Lobby, callback de fim e consulta associada ao código | Código de torneio, eventos de lobby, callback com Match ID e recuperação de resultado | Exige chave/registro; operação via tournament code; regras próprias; não cobre partida custom comum criada fora do fluxo | Melhor opção oficial para competição organizada que use códigos |
| League Client API (LCU) | Reconhecida pela Riot, mas explicitamente não suportada para terceiros; endpoints específicos são em grande parte comunitários | Cliente aberto, pré/pós-jogo e histórico local disponível | Estado do cliente e, experimentalmente, resumo rico de partidas | Schema e endpoints podem mudar ou desaparecer; credencial local sensível; disponibilidade depende da sessão | Adaptador experimental, opt-in e com fallback |
| Live Client Data API | Documentada pela Riot e servida localmente pelo game client | Durante a partida ativa | Jogadores, placar corrente, itens, runas, eventos e estado do jogo disponível no momento | Não é histórico pós-partida; processo local; risco de uso que afete integridade competitiva | Telemetria técnica controlada, não fonte primária do histórico |
| Data Dragon | Oficial e documentado | Catálogo estático por versão | Nomes, imagens e metadados de campeões, itens, runas e feitiços | Pode atrasar em relação ao patch; não contém resultado nem performance | Enriquecimento e tradução de IDs, preservando o ID original |
| ROFL/replay | Replay oficial como artefato, mas sem contrato público suportado para parsing de estatísticas | Pós-partida, enquanto compatível com patch | Reprodução visual e eventos internos dependendo de ferramenta não oficial | Formato e compatibilidade por patch; parsing comunitário; não é fonte contratual de métricas | Evidência auxiliar, fora do MVP |
| OCR | Técnica externa, sem suporte Riot como API | Tela ao vivo ou pós-jogo | Valores visíveis no placar ou tela final | Erros de reconhecimento, escala, idioma, resolução e dados ocultos | Fallback assistido e sempre revisado |
| Manual | Fonte do próprio produto, sem dependência externa | Pós-partida | Resultado e qualquer campo permitido que um operador possa comprovar | Erro humano; menor granularidade; exige auditoria | Fallback obrigatório e complemento autorizado |

## Riot API e Match V5

### O que é confirmado oficialmente

- Match V5 faz parte da referência oficial de APIs do League of Legends.
- A API usa roteamento regional para consultas de partidas.
- Respostas de partida e timeline podem disponibilizar fatos finais e eventos, conforme o modo, versão e campos presentes.
- Riot ID e PUUID cumprem papéis diferentes; PUUID é identificador técnico, enquanto Riot ID é identificador voltado à apresentação. Esta documentação não reproduz nenhum PUUID real.

### Limites para o RinhaDasLendas

- O repositório não possui chave nem integração Match V5.
- Não se pode presumir que toda partida personalizada esteja acessível pelo mesmo fluxo de uma partida comum.
- A política oficial proíbe exibir publicamente histórico da fila customizada sem opt-in específico do jogador. Sem esse opt-in, a Riot indica acesso ao próprio jogador por RSO.
- Match V5 não deve ser apresentado como solução já disponível nem como garantia para a partida usada no experimento.

## Tournament API

### Capacidades oficiais

- Criação de provedor e torneio, geração de tournament codes e configuração de lobby.
- Callback ao término de partida criada com tournament code.
- Uso do Match ID do callback para consultar estatísticas completas.
- Recuperação de dados de fim de jogo e consulta de eventos de lobby associados ao código.

### Adequação

É a alternativa oficial mais alinhada a partidas organizadas, desde que a comunidade passe a criar os jogos com tournament codes e cumpra as condições de acesso e política. Não recupera retroativamente uma partida custom comum que não tenha usado esse fluxo. O projeto não possui essa integração hoje.

## League Client API (LCU)

### Status

A Riot descreve a League Client API como comunicação local usada pela interface do League Client, mas declara que o serviço não é oficialmente suportado para aplicações de terceiros e não oferece garantia de documentação completa, disponibilidade ou aviso de mudanças. Catálogos comunitários ajudam a descobrir endpoints, porém não os transformam em contrato oficial.

### Evidência empírica: partida 3266170515

Uma consulta local pós-partida ao histórico do cliente retornou, para uma partida personalizada na plataforma BR1:

- tipo custom e plataforma BR1;
- 10 participantes;
- duração de 31 minutos e 40 segundos;
- estatísticas finais por participante;
- campeões, itens, runas e feitiços;
- 33 frames de timeline;
- 75 eventos no total;
- 48 eventos de abate de campeão;
- 16 eventos de destruição de estrutura;
- 11 eventos de monstro épico;
- séries temporais suficientes para calcular ouro, CS e XP por minuto;
- eventos identificados como `HORDE`, associados a Vastilarvas no contexto observado.

Os 75 eventos correspondem à soma observada de 48 + 16 + 11. Isso é uma consistência interna da amostra, não uma garantia de que toda timeline terá somente esses tipos.

### Falhas observadas

- A rota atribuída automaticamente estava incorreta para pelo menos um participante.
- Assistências de objetivos cruzaram times, produzindo associação incompatível com o lado que realizou o objetivo.
- Por isso, rota automática e assistências de objetivos são **Experimentais** e não podem alimentar estatística oficial sem regra adicional e revisão.
- A presença de `HORDE` confirma o valor bruto observado. A tradução semântica para “Vastilarvas”, sua agregação e compatibilidade entre patches exigem mapeamento versionado; não se deve substituir ou descartar o valor original desconhecido.

### Alcance da evidência

A observação confirma viabilidade técnica em uma sessão e uma partida. Ela não confirma estabilidade do endpoint, cobertura de remakes, outros mapas, outros modos, clientes futuros ou disponibilidade após expiração do histórico local.

## Live Client Data API

A Live Client Data API é servida pelo game client durante a partida e possui documentação oficial, endpoint agregado e endpoints por subconjunto. Ela pode entregar placar corrente, jogadores, itens, runas, estatísticas e eventos disponíveis até aquele instante.

Para o RinhaDasLendas, não é fonte primária de histórico porque:

- depende de coleta durante a partida;
- uma interrupção perde intervalos ou o fechamento;
- campos correntes não equivalem necessariamente ao resultado consolidado;
- a coleta e exibição ao vivo precisam respeitar integridade competitiva;
- o objetivo atual é registro pós-partida, não overlay ou recomendação em tempo real.

Se estudada futuramente, deve ser opt-in, local, limitada ao necessário e nunca revelar informação que dê vantagem competitiva.

## Data Dragon

Data Dragon deve enriquecer IDs, não criar fatos de partida. É apropriado para:

- nome e imagem de campeão;
- nome e imagem de item;
- metadados de runas e feitiços;
- localização em português brasileiro quando disponível;
- associação do catálogo a uma versão conhecida.

Como a atualização pode atrasar em relação ao patch, um ID desconhecido deve permanecer armazenado e ser exibido como não resolvido. Ausência no catálogo não invalida automaticamente a partida.

## ROFL

O replay é útil para revisão visual, mas não há contrato público oficial estável para tratar o arquivo ROFL como banco de estatísticas. Compatibilidade costuma depender do patch, e parsers comunitários podem quebrar. Consequências:

- não usar ROFL como fonte primária;
- não exigir upload de replay no MVP;
- não declarar métricas extraídas como confirmadas sem validação independente;
- usar, no máximo, como evidência auxiliar manual.

## OCR

OCR pode reduzir digitação quando apenas o placar ou a tela de fim de jogo está disponível. Todo valor deve nascer como **Experimental**, conservar a imagem somente se houver base de consentimento e retenção aprovada, e exigir confirmação humana.

Riscos incluem confusão entre caracteres, colunas, ícones e zeros, recorte incorreto, escala, localização e sobreposição visual. OCR não consegue provar, sozinho, eventos de timeline ou assistências de objetivos.

## Entrada manual

A Constituição exige operação manual quando uma integração não estiver disponível. A entrada manual deve:

- registrar origem manual, autor e momento;
- exigir confirmação de resultado e composição;
- permitir deixar campo como ausente em vez de preencher `0`;
- auditar correções posteriores;
- impedir que um dado manual silenciosamente substitua um dado confirmado por outra fonte;
- permitir registro mínimo mesmo sem LCU, Match V5 ou Tournament API.

## Zero, ausente e não aplicável

Toda fonte deve ser normalizada em pelo menos três estados semânticos:

| Estado | Exemplo | Tratamento |
|---|---|---|
| Presente com zero | jogador terminou com 0 abates | Contabilizar como zero. |
| Ausente | a fonte não forneceu visão | Não incluir como zero em média; exibir “indisponível”. |
| Não aplicável | métrica de Vastilarvas em modo sem esse objetivo | Excluir do denominador pertinente e sinalizar não aplicável. |

Um campo omitido no JSON, um valor `null` e um `0` não devem ser normalizados automaticamente para o mesmo resultado. A decisão exata de persistência precisa ser definida em especificação e modelo de dados futuros.
