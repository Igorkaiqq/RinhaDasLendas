# Matriz de disponibilidade e confiabilidade de campos

## Escopo e leitura

Esta matriz identifica a melhor evidência disponível em 2026-07-27. Ela não define nomes de propriedades, schema de upload ou tabelas. “LCU empírico” significa a observação sanitizada da partida customizada `3266170515`; não significa suporte oficial.

As classificações são definidas no [índice](README.md#vocabulário-de-confiabilidade). A comparação completa das fontes está em [Fontes de dados](DATA_SOURCES.md).

## Identificação e metadados

| Campo ou métrica | Fonte principal | Classificação | Fontes auxiliares | Observação |
|---|---|---|---|---|
| Identificador externo da partida | Match V5, Tournament API ou LCU empírico | Confirmado | Manual | O Game ID `3266170515` foi usado somente como referência técnica; idempotência futura ainda não está definida. |
| Plataforma/região | Match V5, callback de torneio ou LCU empírico | Confirmado | Manual | BR1 foi observado na amostra. Roteamento de API e plataforma não devem ser inferidos apenas pelo idioma. |
| Tipo custom | Tournament API ou LCU empírico | Confirmado | Manual | A amostra foi identificada como custom. Match V5 depende de a partida estar acessível. |
| Mapa, modo e fila | Match V5, Tournament API ou LCU | Confirmado quando presentes | Data Dragon, manual | Campo ausente não deve receber valor padrão. |
| Patch/versão do jogo | Match V5 ou LCU | Confirmado quando presente | Data Dragon | A versão do catálogo pode não ser idêntica à versão regional do cliente. |
| Data e início | Match V5, Tournament API ou LCU | Confirmado quando presente | Manual | Converter fuso somente na apresentação; preservar instante. |
| Duração | Match V5 ou LCU empírico | Confirmado | Live Client, manual | A amostra durou 31m40s. Recalcular por último frame é apenas fallback derivado. |
| Estado final, remake ou surrender | Match V5 ou LCU | Confirmado quando explicitado | Manual | Não inferir somente pela duração. |
| Vínculo com draft do Rinha | Backend do Rinha após revisão | Indisponível | Comparação de participantes | Não existe implementação nem regra de equivalência aprovada. |

## Participantes, identidade e composição

| Campo ou métrica | Fonte principal | Classificação | Fontes auxiliares | Observação |
|---|---|---|---|---|
| Quantidade de participantes | Match V5 ou LCU empírico | Confirmado | Manual | A amostra trouxe 10 participantes. |
| Lado/time da partida | Match V5, Tournament API ou LCU | Confirmado | Manual | Deve ser snapshot da partida, não referência mutável ao time cadastrado. |
| Riot ID em snapshot | Fonte oficial autorizada ou LCU quando presente | Confirmado quando presente | Manual | Não registrar PUUID em documentação; vínculo interno exige regra futura. |
| Vínculo com Jogador cadastrado | Backend após revisão humana | Indisponível atualmente | Riot ID como pista | O cadastro existe, mas não há associação com partidas. |
| Campeão | Match V5 ou LCU empírico | Confirmado | Live Client, Data Dragon, OCR, manual | Data Dragon resolve apresentação; o ID original deve ser preservado. |
| Rota automática | Match V5/LCU como sugestão | Experimental | Preferência cadastrada, revisão manual | A rota automática estava incorreta na amostra. Preferência do cadastro também não prova rota jogada. |
| Rota revisada | Confirmação humana com evidência | Confirmado após revisão | Draft, timeline, manual | Precisa preservar que a origem foi revisão, não o valor automático. |
| Capitão | Draft do Rinha ou manual | Confirmado no contexto do draft | Tournament API | Não é estatística entregue pelo LCU como fato do jogo. |

## Placar e estatísticas finais

| Campo ou métrica | Fonte principal | Classificação | Fontes auxiliares | Observação |
|---|---|---|---|---|
| Vitória/derrota por lado | Match V5 ou LCU empírico | Confirmado | Tournament API, manual | Exigir consistência entre os dois lados. |
| Abates, mortes e assistências | Match V5 ou LCU empírico | Confirmado | Live Client final, OCR, manual | Zero é valor legítimo; ausência deve ficar ausente. |
| KDA calculado | Estatísticas finais confirmadas | Derivável sob fórmula versionada | Nenhuma | A fórmula `(kills + assists) / max(1, deaths)` é uma proposta versionada, não uma decisão fechada. O tratamento semântico de zero mortes e a apresentação de “KDA perfeito” permanecem decisões pendentes. |
| Farm total (CS) | Match V5 ou LCU empírico | Confirmado | Live Client, OCR, manual | Definir se inclui tropas neutras ao comparar fontes. |
| Ouro final | Match V5 ou LCU empírico | Confirmado | Live Client, OCR, manual | Ouro corrente durante o jogo não equivale necessariamente ao total ganho. |
| Nível final e XP | LCU empírico/timeline quando presentes | Confirmado para campos presentes | Live Client | XP final ou acumulado deve manter sua unidade e semântica original. |
| Dano a campeões | Match V5 ou LCU empírico | Confirmado | OCR/manual somente se visível | Não confundir dano total, físico, mágico e verdadeiro. |
| Dano recebido e mitigado | Match V5 ou LCU empírico quando presentes | Confirmado | Manual | Ausência de uma categoria não implica zero. |
| Cura e escudo | Match V5 ou LCU empírico quando presentes | Confirmado | Manual | Os nomes e escopos variam por fonte e versão. |
| Visão, wards colocadas/removidas | Match V5 ou LCU empírico quando presentes | Confirmado | Live Client, manual | OCR geralmente não cobre todos os campos. |
| Estruturas destruídas por participante | Match V5 ou LCU quando presente | Confirmado | Eventos de estrutura | Totais individuais e eventos têm semânticas diferentes. |
| Itens finais por slot | Match V5 ou LCU empírico | Confirmado | Live Client, Data Dragon, OCR | Slot vazio presente não é o mesmo que lista ausente. |
| Runas | Match V5 ou LCU empírico | Confirmado | Live Client, Data Dragon, manual | Preservar IDs mesmo se o catálogo não resolver. |
| Feitiços de invocador | Match V5 ou LCU empírico | Confirmado | Live Client, Data Dragon, OCR | Preservar IDs e versão de catálogo. |
| Bans | Match V5, Tournament API ou LCU quando presente | Confirmado quando presente | Draft/manual | Ban ausente, sem ban e campeão de ID zero exigem distinção. |

## Timeline e métricas por minuto

| Campo ou métrica | Fonte principal | Classificação | Fontes auxiliares | Observação |
|---|---|---|---|---|
| Frames de timeline | Match V5 timeline ou LCU empírico | Confirmado | Live Client coletado | A amostra LCU trouxe 33 frames. |
| Ouro por minuto | Frames com ouro | Derivável | Live Client amostrado | A amostra permitiu a série. O primeiro e último ponto e a granularidade devem ser preservados. |
| CS por minuto | Frames com CS | Derivável | Live Client amostrado | Não interpolar lacuna como zero; informar resolução temporal. |
| XP por minuto | Frames com XP | Derivável | Live Client amostrado | A amostra permitiu a série; confirmar semântica do campo antes de agregar. |
| Diferença de ouro entre times por minuto | Ouro por participante/frame | Derivável | Nenhuma | Requer todos os participantes do frame; frame parcial é inválido para o total. |
| Pico e tendência de vantagem | Série temporal válida | Derivável | Nenhuma | Fórmula e arredondamento precisam ser documentados. |
| Rota inferida por posição inicial | Timeline posicional, se disponível | Experimental | Revisão humana | Posição, troca e roaming tornam a inferência ambígua. |
| Tempo exato de fim | Último evento/frame | Experimental como substituto | Duração final | Preferir duração final explícita. |

## Eventos e objetivos

| Campo ou métrica | Fonte principal | Classificação | Fontes auxiliares | Observação |
|---|---|---|---|---|
| Eventos totais da amostra | LCU empírico | Confirmado | Match V5 timeline | Foram observados 75 eventos. |
| Abates de campeão na timeline | LCU empírico | Confirmado | Match V5 timeline | Foram observados 48 eventos; isso não substitui validação contra placar final. |
| Destruições de estruturas | LCU empírico | Confirmado | Match V5 timeline | Foram observados 16 eventos. |
| Monstros épicos | LCU empírico | Confirmado | Match V5 timeline | Foram observados 11 eventos. |
| Instante e lado do objetivo | Match V5 timeline ou LCU | Confirmado quando coerente | Manual/replay | Validar o lado contra o estado da partida. |
| Tipo bruto `HORDE` | LCU empírico | Confirmado | Match V5 timeline | O valor bruto apareceu na amostra. |
| Nome “Vastilarvas” para `HORDE` | Mapeamento versionado | Experimental | Catálogo ou regra revisada | A associação é coerente com a amostra, mas deve preservar o código bruto e considerar patch. |
| Quantidade de Vastilarvas | Eventos `HORDE` validados | Derivável | Manual | Só derivar após validar semântica, unidade e patch. |
| Assistências em abate de campeão | Timeline quando coerente | Confirmado quando validado | Placar final | O evento individual pode ser usado, mas deve respeitar os times. |
| Assistências de objetivos | LCU empírico | Experimental e não confiável | Match V5/manual | A amostra cruzou times; excluir de estatísticas oficiais até solução comprovada. |
| Primeiro sangue | Primeiro evento de campeão validado | Derivável | Campo final explícito | Excluir eventos inválidos e confirmar ordem temporal. |
| Primeira torre | Primeiro evento de estrutura validado | Derivável | Campo final explícito | Definir quais estruturas qualificam. |
| Sequência de objetivos | Timeline validada | Derivável | Replay/manual | Empates de timestamp e eventos duplicados precisam de regra. |

## Agregações históricas

| Campo ou métrica | Fonte principal | Classificação | Fontes auxiliares | Observação |
|---|---|---|---|---|
| Partidas jogadas | Partidas confirmadas e vinculadas | Indisponível atualmente; derivável no futuro | Nenhuma | Prévia, rejeitada e participante não vinculado não devem entrar no agregado do jogador. |
| Vitórias e derrotas | Partidas confirmadas | Indisponível atualmente; derivável no futuro | Nenhuma | Requer vínculo confiável. |
| Win rate | Vitórias e partidas válidas | Indisponível atualmente; derivável no futuro | Nenhuma | Denominador deve ser explícito. |
| Médias de K/D/A | Estatísticas finais presentes | Indisponível atualmente; derivável no futuro | Nenhuma | Campo ausente não entra como zero. |
| Farm, ouro, dano e visão médios | Partidas com campo presente | Indisponível atualmente; derivável no futuro | Nenhuma | Cada média pode ter denominador diferente conforme disponibilidade. |
| Campeões mais jogados | Campeões confirmados | Indisponível atualmente; derivável no futuro | Nenhuma | Empates precisam de ordenação estável. |
| Rotas mais jogadas | Rotas revisadas confiáveis | Indisponível atualmente | Sugestão automática experimental | Não agregar a rota automática incorreta. |
| Desempenho por time cadastrado | Snapshot e vínculo de time | Indisponível | Draft/manual | O modelo atual de time é mutável e não representa histórico. |
| Estatísticas sazonais | Partida vinculada a temporada aprovada | Indisponível | Data da partida | A feature 023 possui `specs/023-fundacao-competitiva-sazonal/spec.md` em status `Draft`, aguardando revisão humana; ainda não há plano, tarefas ou implementação. |

## Disponibilidade por fonte

| Fonte | Campos diretos | Campos deriváveis | Campos que não deve afirmar |
|---|---|---|---|
| Match V5 | Metadados, participantes, placar e timeline conforme resposta | Métricas por minuto, primeiros eventos e agregados | Acesso universal a custom matches ou consentimento implícito |
| Tournament API | Contexto do código, lobby, callback e Match ID | Associação segura ao evento criado pelo código | Cobertura de partida custom comum fora do código |
| LCU | Campos retornados pelo cliente naquela sessão | Séries e totais com validação | Estabilidade futura, rota automática correta ou assistência de objetivo correta |
| Live Client | Estado disponível durante jogo | Séries se coletadas continuamente | Resultado histórico completo sem fechamento confiável |
| Data Dragon | Catálogo e ativos estáticos | Apresentação localizada | Qualquer performance ou resultado |
| ROFL | Replay visual e dados acessíveis por ferramentas específicas | Somente após validação independente | Contrato estável de estatísticas |
| OCR | Texto/ícone reconhecido | Totais simples revisados | Precisão automática ou timeline completa |
| Manual | Valores confirmados pelo operador | Agregados após confirmação | Precisão sem auditoria ou prova de origem |

## Regra para zero e ausência

Para cada campo numérico futuro, a camada de domínio e a apresentação devem conseguir distinguir:

- `0` fornecido pela fonte: valor observado e válido;
- campo omitido ou `null`: indisponível;
- campo rejeitado por inconsistência: presente na origem, mas não confiável;
- campo não aplicável ao modo: não aplicável;
- campo derivado: valor calculado, com dependências e fórmula identificáveis.

Exemplo conceitual: `0` Vastilarvas em uma partida que suporta o objetivo é diferente de ausência do evento por timeline incompleta e diferente de modo no qual Vastilarvas não existem. Esta regra deve ser mantida em importação, persistência, API, agregação e interface.
