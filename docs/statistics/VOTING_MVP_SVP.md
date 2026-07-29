# Votação de MVP e SVP

## Categorias

- **MVP**: destaque entre participantes do lado vencedor da série.
- **SVP**: destaque entre participantes do lado derrotado da série.

A votação oficial ocorre por Série oficial concluída e validada, seja `DiariaTemporaria` ou `ConfrontoOficial`. Série empatada, cancelada ou sem vencedor não abre as categorias até decisão competitiva explícita. Nenhum amistoso, mesmo agendado, agrupado ou configurado com Fearless, abre votação oficial de MVP/SVP.

## Eleitores e candidatos

Somente jogadores que participaram de ao menos uma Partida válida da Série podem votar. A elegibilidade é verificada pela participação histórica, não pelo elenco atual.

Regras:

- candidato a MVP deve ter participado pelo lado vencedor;
- candidato a SVP deve ter participado pelo lado derrotado;
- eleitor pode votar nas duas categorias se ambas estiverem abertas;
- autovoto é permitido;
- substitutos que efetivamente participaram são elegíveis;
- integrante do elenco que não jogou não vota nem recebe voto.

## Turnos e prazo

O organizador define o prazo de cada turno. O prazo e o instante de abertura devem ser registrados de forma inequívoca.

Cada eleitor possui **uma escolha por categoria e por turno**. Uma tentativa adicional não cria outro voto. A especificação futura deve decidir se a escolha pode ser substituída antes do prazo; até essa decisão, não se presume edição.

Votos após o prazo são recusados. O organizador pode encerrar antecipadamente apenas por ação autorizada e auditada; não pode incluir voto retroativo.

## Apuração inicial

Ao encerrar o primeiro turno:

- vence o candidato com mais votos válidos em cada categoria;
- empate no primeiro lugar abre segundo turno apenas entre os candidatos empatados;
- votos do primeiro turno permanecem no histórico, mas não são transportados como votos do segundo;
- eleitores elegíveis recebem novamente uma escolha na categoria empatada.

MVP e SVP são apurados separadamente. Empate em uma categoria não bloqueia a conclusão da outra.

## Empate persistente

Se o segundo turno terminar empatado:

1. comparar a média do Rinha Score elegível de cada candidato nas Partidas da Série;
2. usar a mesma versão de Score e apenas resultados com confiança aceita pelo regulamento;
3. vence a maior média;
4. se as médias forem exatamente iguais na precisão oficial da versão, declarar co-vencedores.

Ausência de Score confiável para um candidato impede comparação assimétrica. Nesse caso, o desempate por Score é considerado inconclusivo e os candidatos empatados tornam-se co-vencedores.

## Invalidação administrativa

Todo integrante de **Admin+**, isto é, SuperAdmin, Presidente, VicePresidente ou Admin, pode invalidar um voto com motivo obrigatório. Moderador, Capitão e Jogador não podem invalidar. A ação deve registrar:

- voto e turno afetados;
- categoria;
- responsável;
- instante;
- motivo;
- estado anterior;
- impacto na apuração.

O voto invalidado permanece no histórico e deixa de contar. Não é permitido apagar fisicamente, alterar candidato ou criar voto em nome do eleitor. Invalidação após a publicação do resultado exige reapuração auditável e nova versão do resultado.

Invalidar um turno inteiro, reabrir prazo ou substituir todos os votos são ações distintas e não ficam autorizadas automaticamente por esta regra.

## Imutabilidade e projeções

Voto registrado, invalidação e encerramento de turno são fatos. Contagem, candidatos empatados, média de Score e vencedores são projeções reconstruíveis.

Cada resultado deve informar:

- série e categoria;
- turno decisivo;
- total de eleitores elegíveis;
- votos válidos e invalidados;
- prazo;
- critério de desempate;
- versão de Score usada, quando aplicável;
- vencedor ou co-vencedores;
- versão da apuração.

## Casos-limite

- Jogador que atuou pelos dois lados por correção excepcional exige resolução administrativa da participação antes da votação.
- Invalidação que cria novo empate reabre a apuração segundo as mesmas regras, sem inventar votos.
- Correção da série ou de um Score usado no desempate provoca reconstrução e nova versão do resultado.
- Nenhum amistoso, agendado, agrupado ou isolado, possui votação oficial de MVP/SVP. Se houver votação social futura, ela deverá usar regulamento próprio, histórico separado e não afetará Score, Rating, rankings ou recordes oficiais.

## Classificação conceitual

São regras aceitas: categorias por lado, participantes como únicos eleitores, autovoto, prazo do organizador, uma escolha por categoria e turno, invalidação por Admin+ com motivo e auditoria, segundo turno, desempate por média de Score e co-vencedores em igualdade exata.

Permanecem para especificação: nomes de entidades e tabelas, permissão do organizador, edição antes do prazo, notificações e apresentação visual.
