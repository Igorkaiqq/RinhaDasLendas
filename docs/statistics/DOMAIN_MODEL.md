# Modelo de Domínio Competitivo e Estatístico

## Objetivo

Este documento organiza a linguagem do domínio e classifica conceitos. Os nomes abaixo não autorizam criação automática de classes ou tabelas.

## Regra de classificação

Cada conceito pertence a uma destas classes:

- **Existente**: já possui representação no domínio atual.
- **Extensão aprovada**: regra de produto aceita, mas ainda depende de especificação para alterar o modelo existente.
- **Conceito candidato**: nome útil para discussão, sem decisão de que será entidade, value object, enum ou projeção.
- **Projeção**: visão reconstruível, sem autoridade para alterar fatos.

## Mapa de conceitos

| Conceito | Classificação | Fronteira provável | Observação |
|---|---|---|---|
| Jogador | Existente | Compartilhado por identidade | Referência estável para participantes e vínculos |
| DraftMontagem | Existente | Partidas, em integração com o fluxo atual | Presença e montagem; não é automaticamente uma competição |
| DraftMontagemTime | Existente | Partidas | Equipe temporária restrita à montagem |
| Time | Existente | Competição | Base reutilizável, ainda sem todas as regras de Time oficial |
| Season | Conceito candidato | Competição | Recorte temático da Riot obrigatório para partidas futuras |
| Competição | Conceito candidato | Competição | Obrigatória para toda Série oficial; organiza regulamento e circuito |
| Rodada | Conceito candidato | Competição | Recorte obrigatório que posiciona uma Série oficial dentro da Competição |
| Vínculo de elenco | Conceito candidato | Competição | Histórico temporal entre jogador e Time oficial |
| Janela de transferência | Conceito candidato | Competição | Período publicado pela organização, com início e fim locais e sem cadência presumida |
| Escalação diária | Conceito candidato | Competição | Foto operacional dos cinco jogadores por rota e reservas disponíveis |
| Série | Conceito candidato | Partidas | Agrupa Partidas e possui natureza oficial ou amistosa |
| Série oficial | Conceito candidato | Partidas | Qualquer Série com validade competitiva oficial, obrigatoriamente vinculada a Competição e rodada |
| DiariaTemporaria | Conceito candidato | Partidas | Subtipo oficial originado em DraftMontagem, com equipes temporárias e vínculo obrigatório ao circuito diário |
| ConfrontoOficial | Conceito candidato | Partidas | Subtipo oficial disputado exclusivamente entre Times oficiais |
| Série amistosa | Conceito candidato | Partidas | Série sem validade competitiva oficial; Competição é opcional ou ausente e Fearless pode ser configurado quando direta e fora de Evento de quatro times |
| Partida | Conceito candidato | Partidas | Unidade individual com lados, resultado, escalações, picks, bans e estatísticas |
| Fato normalizado | Conceito candidato | Ingestão | Registro imutável aceito após validação e reconciliação |
| Rinha Score | Projeção | Analytics | Desempenho de 0 a 100, versionado e acompanhado de confiança |
| Lançamento de Rating | Conceito candidato | Analytics | Entrada imutável de ledger; o saldo é projeção |
| Elo Rinha | Projeção | Analytics | Faixa derivada do Rating atual |
| Voto MVP/SVP | Conceito candidato | Partidas | Escolha auditável de participante em um turno de votação |

## Times temporários e oficiais

Um `DraftMontagemTime` existe para organizar participantes dentro de uma montagem. Seu nome, cor, capitão e membros têm validade naquele contexto.

Um **Time oficial** representa uma organização competitiva reutilizável, com capitão titular, elenco por rota, reservas, histórico, janelas e validação organizacional.

Portanto:

- finalizar uma montagem não cria, atualiza nem funde um Time oficial;
- um time temporário pode enfrentar outro time temporário;
- um Time oficial pode fornecer um lado para uma série sem perder sua identidade;
- a escalação usada em uma Partida é um fato histórico e não deve apontar apenas para a composição atual;
- qualquer promoção de montagem para Time oficial será um caso de uso futuro, explícito e validado.

## Hierarquia competitiva proposta

```text
Season
  -> Competição obrigatória para validade oficial
     -> Rodada
        -> Série oficial
           -> DiariaTemporaria originada em DraftMontagem
           -> ConfrontoOficial entre Times oficiais
           -> Partidas
  -> Contexto amistoso
     -> Competição opcional ou ausente conforme contrato
     -> Série amistosa direta [Fearless opcional]
        -> Partidas
     -> Partida isolada
```

A hierarquia é conceitual. Uma especificação posterior decidirá cardinalidades e nomes persistidos.

No contrato conceitual, `Partida` é sempre a unidade individual que possui resultado e estatísticas. `Série` agrupa Partidas e deriva delas placar e vencedor. Linguagem coloquial equivalente não cria outra entidade, outro identificador nem um nível adicional na hierarquia.

## Invariantes aceitas

- Toda partida pertence a exatamente uma Season temática da Riot.
- Consultas usam a Season atual por padrão e oferecem recorte multisseason ou todas as Seasons.
- Uma Partida possui dois lados; um jogador não ocupa ambos nem aparece duas vezes na mesma Partida.
- Uma escalação registra a rota efetivamente jogada, sem alterar preferências do perfil.
- Um time temporário e um Time oficial nunca são tratados como o mesmo tipo por inferência.
- Toda Série oficial pertence a exatamente uma Competição e a uma rodada dessa Competição.
- `DiariaTemporaria` é Série oficial, nasce de `DraftMontagem`, usa equipes temporárias e alimenta projeções oficiais.
- `ConfrontoOficial` é somente o subtipo de Série oficial entre Times oficiais.
- Competição opcional ou ausente é admitida apenas para amistoso conforme seu contrato.
- Toda Série direta de dois lados pode configurar Fearless, seja oficial ou amistosa, desde que não pertença a Evento de quatro times.
- Uma Série amistosa entre Times oficiais permanece amistosa; não se torna `ConfrontoOficial` e não adquire elegibilidade oficial por usar Fearless.
- Fatos confirmados são imutáveis; correções são rastreáveis.
- Score, Rating atual, elo e rankings são projeções reconstruíveis.
- Nenhum amistoso, agendado, agrupado ou isolado, produz Score, Rating, MVP/SVP ou efeito em rankings e recordes oficiais.
- Histórico e agregados amistosos permanecem separados dos equivalentes oficiais.
- Cada partida oficial elegível movimenta Rating entre +10 e +20 por vitória ou entre -5 e -20 por derrota.
- A faixa de derrota descreve `variacaoNominal`; `variacaoAplicada` pode ter magnitude menor quando o piso zero limita o lançamento.
- Evento com quatro times é integralmente sem Fearless, inclusive em seus confrontos internos.
- Vínculo ativo de jogador com Time oficial é único.
- A qualificação por duas Séries oficiais `DiariaTemporaria` é global, histórica, permanente e não consumível; independe do Time e de janela de transferência.

## Sobreposições que devem ser resolvidas por especificação

### Time existente e Time oficial

O `Time` atual possui nome, tag, status, capitão e até cinco membros principais. O conceito oficial acrescenta cinco titulares por rota, até três reservas, máximo de oito, escalações diárias, vínculo temporal, janelas e validação organizacional. A evolução deve preservar histórico e compatibilidade dos dados existentes, sem apenas reinterpretar registros antigos.

### DraftMontagem e série

`DraftMontagem` forma times e mantém presença, capitães e escolhas. Quando seu resultado competitivo é confirmado, origina uma `DiariaTemporaria`, que usa aquelas equipes temporárias, mas precisa pertencer à Competição e à rodada do circuito diário. Séries oficiais entre Times oficiais usam o subtipo `ConfrontoOficial`. Ambos são subtipos de Série oficial e alimentam projeções oficiais. Amistosos podem ser agendados ou agrupados sem validade oficial e só neles Competição pode ser opcional ou ausente. Um amistoso entre Times oficiais continua amistoso, com ou sem Fearless.

### Métricas de jogador e Rinha Score

Estatísticas básicas existentes, como kills, mortes, assistências e farm, são fatos ou medidas. KDA é uma métrica derivada cuja fórmula permanece proposta e versionada para calibração. O Rinha Score é uma composição normalizada e versionada dessas medidas. Não deve substituir nem regravar os valores de origem.

## Itens deliberadamente não definidos

- nomes finais das entidades e tabelas;
- estrutura interna entre Season, Competição e rodadas, sem remover o vínculo obrigatório da Série oficial;
- formato de chave de `DiariaTemporaria`;
- estratégia de promoção de Time existente para oficial;
- parâmetros finais dos algoritmos de Score, força, impacto, soft reset e KDA;
- interface visual, reservada para fase posterior.

As decisões de fórmula e calibração são acompanhadas em [Decisões pendentes do domínio competitivo](./OPEN_DECISIONS.md).
