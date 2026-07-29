# Rinha Score de Performance v1

## Estado da proposta

O Rinha Score é uma projeção de desempenho entre **0 e 100**. A escala, o versionamento, a confiança e a separação por rota são decisões da fundação. Pesos, indicadores internos e limiares deste documento são uma **proposta v1** e precisam ser validados com dados reais antes de se tornarem regulamento.

A apresentação visual do Score fica para fase posterior.

## Princípios

- Comparar o jogador com contexto equivalente de rota, Season e patch.
- Não usar vitória isoladamente como sinônimo de boa performance.
- Não premiar volume sem considerar duração e oportunidade.
- Não transformar campo ausente em zero.
- Expor versão, confiança e cobertura junto ao valor.
- Recalcular quando fatos, amostra de normalização ou versão mudarem.

## Categorias e pesos propostos

Cada categoria produz um valor normalizado de 0 a 100. O Score é a soma ponderada das categorias disponíveis.

| Categoria | Top | Jungle | Mid | ADC | Support |
|---|---:|---:|---:|---:|---:|
| Combate e teamfight | 25% | 20% | 25% | 35% | 15% |
| Economia e fase de rotas | 30% | 15% | 25% | 30% | 5% |
| Objetivos | 15% | 35% | 15% | 15% | 15% |
| Pressão, utilidade e sobrevivência | 20% | 20% | 20% | 15% | 30% |
| Visão | 10% | 10% | 15% | 5% | 35% |
| **Total** | **100%** | **100%** | **100%** | **100%** | **100%** |

Exemplos de sinais candidatos:

- combate: KDA, participação em abates, dano contextual e eficiência em teamfight;
- economia: ouro e farm por minuto, diferenças em marcos comparáveis e conversão de recursos;
- objetivos: participação, controle e contribuição em estruturas;
- pressão, utilidade e sobrevivência: presença de mapa, mortes evitáveis, controle de grupo, cura, escudo e pressão lateral conforme rota;
- visão: vision score, wards e remoções contextualizadas por duração e rota.

Nenhum sinal candidato deve ser usado antes de ter definição reproduzível e versionada no catálogo. Isso inclui KDA: sua fórmula interna permanece proposta para calibração e não é ratificada apenas por aparecer como sinal candidato. Consulte [Decisões pendentes do domínio competitivo](./OPEN_DECISIONS.md).

## Normalização proposta

Para cada indicador:

1. selecionar população elegível da mesma rota, Season e faixa de patch;
2. ajustar valores por duração quando a métrica exigir;
3. limitar influência de extremos por percentis robustos;
4. converter o valor para escala de 0 a 100 pela distribuição de referência;
5. combinar indicadores dentro da categoria;
6. combinar categorias pelos pesos da rota.

Quando a amostra específica for pequena, a versão do algoritmo deve usar uma população de fallback declarada, por exemplo Season e rota, antes de ampliar para multisseason. Misturar rotas como fallback silencioso é proibido.

O Score final é arredondado apenas para exibição. Cálculos derivados preservam a precisão definida pela versão.

## Confiança

Confiança é separada do Score e também usa escala de 0 a 100. Ela representa capacidade de sustentar o cálculo, não qualidade da atuação.

Proposta de composição:

| Fator | Peso na confiança |
|---|---:|
| Cobertura dos campos necessários | 50% |
| Confiabilidade da origem e resolução de identidade | 25% |
| Adequação da amostra de normalização | 15% |
| Consistência e validação dos dados | 10% |

Entrada manual validada pode produzir Score, mas normalmente terá confiança menor quando não cobrir todos os campos. O valor apresentado deve informar quais categorias foram calculadas.

## Dados ausentes

- Campo ausente permanece ausente, nunca zero presumido.
- Categoria sem indicadores mínimos não é calculada.
- Pesos das categorias disponíveis podem ser renormalizados para produzir estimativa, mas a confiança deve cair proporcionalmente.
- Uma estimativa não deve entrar em ranking quando ficar abaixo do limiar definido pelo regulamento da versão.
- O limiar numérico e os mínimos por categoria são propostas pendentes de calibração.
- Se a ausência comprometer o significado, o resultado é `Score indisponível`, acompanhado do motivo estrutural.

## Elegibilidade

O Score v1 é calculado para Partida válida de Série oficial, incluindo `DiariaTemporaria` e `ConfrontoOficial`. Amistoso agendado, agrupado ou isolado, inclusive Série amistosa com Fearless, Partida cancelada, remake sem resultado e Partida com identidade não resolvida não geram Score.

Agregados de Série ou Season usam Scores elegíveis e devem considerar confiança e quantidade de Partidas; não são simples médias sem peso definido.

## Versionamento

Cada cálculo registra:

- identificador `rinha-score-v1` e revisão de parâmetros;
- versão do catálogo de métricas;
- conjunto de normalização;
- rota efetivamente jogada;
- categorias, pesos e valores intermediários;
- campos ausentes;
- Score e confiança;
- instante de cálculo.

Alterar pesos, normalização, indicadores, fallback ou tratamento de ausência exige nova revisão versionada e reconstrução explícita. Scores de versões diferentes não devem ser comparados como se fossem equivalentes.

## Validação antes de adoção

A proposta deve ser testada com distribuição por rota, estabilidade entre patches, sensibilidade a outliers, correlação indevida com vitória, impacto de dados ausentes e revisão humana de partidas conhecidas. Somente depois dessa calibração os pesos podem ser ratificados.
