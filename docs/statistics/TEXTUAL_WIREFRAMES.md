# Wireframes textuais da área competitiva

## Objetivo

Definir a hierarquia, os estados e o comportamento responsivo das telas competitivas antes da especificação de cada feature. Estes wireframes são referência de produto e não substituem os contratos de API, as regras de autorização nem o design system.

## Princípios comuns

- Manter o `AppShell`, a navegação lateral e a barra superior existentes.
- Usar somente tokens de `docs/design/DESIGN_TOKENS.md`. Nenhuma tela deste conjunto cria cor, tipografia, raio, espaçamento ou elevação.
- Priorizar dados, ações, navegação e, por último, decoração.
- Usar cards para resumos e tabelas para rankings, históricos e estatísticas densas.
- Reservar fonte mono para placares, contadores, duração, rating, versão e outros dados curtos.
- Limitar cada região a uma ação primária. Confirmações usam modal; edição rápida usa drawer.
- Não depender apenas de cor para resultado, estado, tendência ou disponibilidade.
- Usar skeleton durante carregamento. Preservar dados conhecidos durante atualização.
- Todo erro recuperável informa o que falhou e oferece a ação localizada `Tentar novamente`.
- Todo estado vazio explica o filtro ou pré-requisito aplicável e oferece uma próxima ação quando autorizada.
- Textos, rótulos, títulos, placeholders, tooltips, erros, confirmações, estados, badges, toasts e nomes acessíveis devem vir de chaves equivalentes em `pt.json` e `en.json`.
- Dados oficiais sempre exibem temporada e competição. Amistosos aparecem apenas em histórico identificado e nunca alimentam estatísticas, ranking ou rating oficiais.
- Filtros aplicados devem permanecer na URL para permitir retorno, compartilhamento e navegação previsível.
- Se nenhuma Season for escolhida, a tela usa a Season atual. O seletor aceita várias Seasons ou `Todas`, nunca as duas opções ao mesmo tempo, e todo resultado identifica Seasons e versões incluídas.
- Se não existir Season ativa, a omissão mostra estado vazio com calendário não configurado; não exibe dados históricos como fallback e oferece seleção explícita de Seasons quando aplicável.

## Estrutura responsiva comum

### Desktop, 1280 px ou mais

```text
┌──────────────┬──────────────────────────────────────────────────────────┐
│ Navegação    │ Título, contexto e ação principal                      │
│ lateral      ├──────────────────────────────────────────────────────────┤
│              │ Filtros persistentes                                    │
│              ├──────────────────────────────────────────────────────────┤
│              │ Resumo / conteúdo principal / apoio                     │
└──────────────┴──────────────────────────────────────────────────────────┘
```

### Tablet, 768 a 1279 px

- Recolher a navegação conforme o padrão existente.
- Quebrar resumos em duas colunas.
- Mover filtros secundários para um drawer acionado por botão com contagem de filtros ativos.
- Transformar relações de três colunas em duas antes de truncar conteúdo.

### Mobile, 320 a 767 px

- Usar uma única coluna e padding móvel oficial.
- Exibir seletor de temporada e filtro principal antes do conteúdo.
- Converter tabelas extensas em lista de cards ordenados; não ocultar colunas essenciais sem alternativa acessível.
- Manter ações frequentes próximas ao título, sem barra fixa que cubra conteúdo.
- Garantir alvos de toque de pelo menos 44 por 44 pixels e ausência de rolagem horizontal da página.

## Tela Estatísticas

**Objetivo do usuário:** entender rapidamente a temporada oficial e abrir rankings ou recortes detalhados.

```text
┌ Estatísticas ─ Seasons [atual/múltiplas/todas] ─ Competição ─ [Comparar]┐
│ Escopo: somente partidas oficiais encerradas                         │
├ Partidas oficiais ─ Jogadores elegíveis ─ Times ─ Atualizado em ─────┤
├ Ranking de jogadores ───────────────┬ Ranking de times ──────────────┤
│ posição | jogador | jogos | rating  │ posição | time | J-V-D | forma │
├ Líderes por indicador ──────────────┴─────────────────────────────────┤
│ visão | participação | objetivos | consistência                      │
├ Partidas recentes oficiais ───────────────────────────────────────────┤
└ Metodologia e elegibilidade ──────────────────────────────────────────┘
```

- O título informa Seasons, versões e escopo oficial antes de qualquer número; omissão seleciona a Season atual.
- Cards de resumo apresentam contagem, não conclusões promocionais.
- Rankings mostram posição, variação, amostra válida e motivo de inelegibilidade quando aplicável.
- A seção de metodologia abre explicação acessível sobre fontes, última consolidação e regras vigentes.
- O botão `Comparar` exige de dois a quatro jogadores elegíveis e abre a tela de Comparação.
- No mobile, rankings viram cards numerados; líderes usam abas roláveis com controles de teclado.

## Tela Jogador

**Objetivo do usuário:** consultar identidade competitiva, evolução sazonal e partidas que sustentam os números.

```text
┌ [voltar] Jogador / nome ─ rota ─ time atual ─ temporada [seletor] ┐
│ Rating atual | faixa | posição | Partidas válidas | tendência      │
├ Visão geral ─ Campeões ─ Partidas ─ Votações ─ Histórico de rating ┤
├ Indicadores por função ─────────────┬ Evolução por rodada ─────────┤
│ KDA | participação | visão | ouro   │ gráfico + tabela alternativa │
├ Campeões mais usados ───────────────┴───────────────────────────────┤
├ Partidas oficiais paginadas ────────────────────────────────────────┤
└ Observações de elegibilidade e origem dos dados ────────────────────┘
```

- Exibir time e rota por período; não reescrever partidas históricas após transferência.
- Diferenciar `sem amostra`, `não elegível` e `sem dados importados`.
- Gráficos sempre possuem tabela ou resumo textual equivalente.
- Votações mostram apenas resultados publicados; autoria individual permanece protegida.
- No mobile, identidade e rating antecedem as abas; métricas usam duas colunas somente quando couberem sem abreviações ambíguas.

## Tela Time

**Objetivo do usuário:** entender composição, campanha e desempenho coletivo na temporada.

```text
┌ [voltar] Time / nome e tag ─ temporada [seletor]                    ┐
│ Campanha | posição | rating coletivo | sequência | Partidas válidas │
├ Visão geral ─ Elenco ─ Partidas ─ Estatísticas ─ Transferências ────┤
├ Escalação vigente ────────────────┬ Desempenho por lado ────────────┤
│ titulares, reservas e capitão     │ lado A | lado B | duração       │
├ Objetivos e ritmo ────────────────┴─────────────────────────────────┤
├ Partidas oficiais paginadas ────────────────────────────────────────┤
└ Linha do tempo do elenco ───────────────────────────────────────────┘
```

- A escalação vigente usa datas de validade; o histórico preserva o time representado em cada partida.
- Transferências futuras ou pendentes não aparecem como concluídas.
- Estatísticas por lado usam nomes neutros definidos pela competição, sem inferir vantagem pela cor.
- No mobile, elenco é uma lista por função; campanha e rating aparecem antes do histórico.

## Tela Partida

**Objetivo do usuário:** conferir resultado, escalações, estatísticas e proveniência de uma partida.

```text
┌ [voltar] Partida / competição / rodada / data                       ┐
│ tipo de série | Season/versão | OFICIAL ou AMISTOSO | revisão       │
├ Time A ───────────── placar e duração ───────────── Time B ──────────┤
├ Navegação das Partidas da Série [P1] [P2] [P3...] ──────────────────┤
├ Placar por jogador ─────────────────────────────────────────────────┤
│ função | jogador | campeão | K/D/A | ouro | dano | visão | objetivos│
├ Linha do tempo de objetivos ─────────┬ Draft e Fearless ────────────┤
├ Resultado MVP/SVP da série ──────────┴ Integridade da importação ───┤
└ [Admin+: revisar dados] quando autorizado ──────────────────────────┘
```

- Amistosos exibem badge textual e aviso de exclusão dos cálculos oficiais.
- Amistoso direto pode exibir operação Fearless, mas mantém aviso explícito de que não gera votação, Score, Rating, ranking, recorde ou projeção oficial.
- Fearless só é aplicado a série direta entre dois lados e acompanha a série, não partidas isoladas.
- Evento com quatro times aparece como contexto separado que agrupa Séries; todas as Séries internas usam draft padrão e a tela não apresenta contador ou bloqueio Fearless nesse contexto.
- Toda Série oficial identifica competição e rodada da Season. Série `DiariaTemporaria` também identifica data local, capitães, origem `DraftMontagem` e snapshots dos lados temporários.
- A tela representa uma Partida individual. A navegação de Partidas irmãs vem da coleção da Série e não transforma `PartidaDetalheDto` em contêiner de jogos.
- Alterações administrativas exibem versão e trilha de revisão sem expor payload bruto ou credenciais.
- No mobile, placar mantém os dois lados visíveis em sequência; tabela de jogadores vira dez cards agrupados por time e função.

## Tela Comparação

**Objetivo do usuário:** comparar jogadores no mesmo recorte sem produzir conclusões enganosas.

```text
┌ Comparação ─ temporada [seletor] ─ competição ─ função ─────────────┐
│ [Jogador 1] [Jogador 2] [+ adicionar, até 4] [limpar]               │
├ Amostra comparável e alertas de elegibilidade ──────────────────────┤
├ Indicador ─ J1 ─ J2 ─ J3 ─ J4 ─ referência da função ──────────────┤
├ Evolução comum por rodada + tabela equivalente ─────────────────────┤
├ Campeões e diversidade ─────────────────────────────────────────────┤
└ Metodologia: normalização, período e partidas consideradas ─────────┘
```

- O sistema bloqueia comparação entre recortes incompatíveis e explica como corrigir.
- Valores absolutos e por minuto são identificados explicitamente.
- Destaque de maior valor não significa automaticamente melhor desempenho; indicadores inversos devem ter semântica própria.
- No mobile, cada indicador forma uma linha vertical com todos os jogadores, preservando a comparação no mesmo viewport.

## Tela administrativa de importação

**Objetivo do usuário:** importar, validar, revisar e consolidar dados sem contaminar estatísticas oficiais.

```text
┌ Importação de partidas ─ ambiente/origem ─ [Nova importação]        ┐
├ Etapas: Envio > Validação > Correspondência > Revisão > Consolidação├
│ Arquivo ou referência externa | tipo | competição | temporada       │
├ Resumo de validação ─────────────────┬ Pendências de correspondência ┤
│ válidos | avisos | erros | duplicados│ jogadores, times, campeões    │
├ Prévia normalizada e diferenças ─────┴───────────────────────────────┤
├ [Descartar] [Salvar rascunho] [Consolidar dados oficiais]            │
└ Histórico paginado: chave, fonte, estado, ator, instante, resultado  │
```

- A consolidação oficial é a única ação primária e exige confirmação com resumo das mudanças.
- Repetir a mesma chave com conteúdo idêntico mostra o resultado original sem criar segunda importação; reutilizá-la com conteúdo divergente mostra conflito e preserva a operação original.
- Erros por linha oferecem localização, código traduzível e orientação; payload bruto não é renderizado sem sanitização.
- A credencial Collector acessa somente o endpoint técnico de ingestão autorizado, nunca esta tela.
- No mobile, cada etapa ocupa uma tela lógica; a prévia usa cards de diferenças e mantém o resumo antes da confirmação.

## Tela Temporadas

**Objetivo do usuário:** consultar temporadas e, quando autorizado, operar seu ciclo de vida.

```text
┌ Seasons ─ [Criar Season] para SuperAdmin/Presidente autorizados     ┐
├ Atual ─ nome | período | estado | competições | rodada              │
├ Próximas e anteriores, paginadas ───────────────────────────────────┤
│ nome | início | fim | estado | partidas | ações permitidas          │
├ Drawer de detalhes ─────────────────────────────────────────────────┤
│ calendário | regras publicadas | participantes | auditoria           │
└ Confirmações de abrir, encerrar ou reabrir conforme policy ─────────┘
```

- Estados de ciclo de vida têm texto e ícone, não apenas cor.
- SuperAdmin e Presidente veem ações de Season. Admin e papéis inferiores não veem; VicePresidente só vê quando a condição fina for formalmente aprovada e devolvida pela capacidade.
- Sem Season ativa, a tela mostra `calendarioConfigurado=false`, nenhuma Season atual e orientação para configuração, sem listar todas automaticamente.
- Encerramento mostra impactos em escalações, votações e consolidação antes da confirmação.
- Parâmetros ainda pendentes de decisão aparecem como `não configurado`, nunca com valor presumido.
- No mobile, temporada atual é o primeiro card e ações secundárias ficam no menu contextual acessível.

## Tela Elencos e transferências

**Objetivo do usuário:** consultar vínculos por período e executar movimentações autorizadas com rastreabilidade.

```text
┌ Elencos e transferências ─ temporada ─ time ─ estado                ┐
├ Elenco vigente ─ 5 titulares | até 3 reservas | máximo 8 ────────────┤
│ capitão titular | vínculo único | janela | qualificação global 0/2..2/2│
├ Linha do tempo ──────────────────────┬ Solicitações pendentes ───────┤
│ entrada, saída, função, validade     │ origem > destino | prazo      │
├ Drawer de movimentação ─────────────────────────────────────────────┤
│ jogador | origem | destino | função | vigência | justificativa       │
├ Validação de conflitos e prévia de impacto ─────────────────────────┤
└ Histórico imutável e paginado ──────────────────────────────────────┘
```

- A interface não oferece exceções de janela até que a decisão correspondente seja registrada.
- A composição identifica um capitão titular, exatamente cinco titulares por rota e até três reservas.
- A ficha do jogador mostra qualificação global histórica pelas duas Séries `DiariaTemporaria` oficiais exigidas; amistosos, eventos, cancelamentos e anulações não avançam a barra.
- A qualificação em `2/2` não é consumida, reservada ou reduzida quando uma solicitação ou transferência é criada.
- Movimentações não alteram retrospectivamente escalações ou estatísticas consolidadas.
- O estado pendente não concede elegibilidade ao destino.
- No mobile, linha do tempo e pendências são abas; o formulário permanece vertical.

## Tela Votação MVP/SVP

**Objetivo do usuário:** votar durante a janela válida e consultar o resultado após publicação.

```text
┌ Votação única da série ─ turno 1 ou 2 ─ encerra em [instante]       ┐
│ Participou da série | autovoto permitido | privacidade | janela      │
├ MVP ─ candidatos do lado vencedor ──────────────────────────────────┤
│ card: jogador | time | função | resumo factual | selecionar          │
├ SVP ─ candidatos do lado derrotado ─────────────────────────────────┤
├ Revisão do voto ─ MVP escolhido | SVP escolhido ─ [Confirmar voto]  │
└ Resultado: turno | média Score | vencedor(es) ou co-vencedores       │
```

- A interface não revela votos individuais, tendência parcial nem autoria.
- O estado informa se o usuário participou, já votou naquela categoria e turno ou está fora da janela.
- Empate do primeiro turno abre segundo turno só com empatados. Empate persistente usa média de Score; igualdade exata ou comparação inconclusiva mostra co-vencedores.
- Admin+ com `CanInvalidateSeriesVotes` vê a ação de invalidar voto, exige motivo e recebe a versão reapurada; demais papéis não veem o controle.
- Em área administrativa separada, Admin+ com `CanModerateSeriesVotes` consulta votos por `votoId` opaco e identidade mínima do eleitor. A tela identifica o acesso sensível, registra finalidade e não reutiliza esses dados em resultado público ou cache compartilhado.
- Enquanto a regra de mudança de voto estiver pendente, a confirmação não promete alteração posterior.
- No mobile, candidatos são cards de seleção única com rótulo programático; a revisão antecede o envio.

## Tela Rating

**Objetivo do usuário:** compreender posição, evolução e fatores publicados do rating sazonal.

```text
┌ Rating ─ temporada ─ competição ─ função ─ elegibilidade            ┐
├ Minha posição ou líder da visão ─ rating | faixa | jogos | variação  │
├ Ranking paginado ────────────────────────────────────────────────────┤
│ posição | jogador | função | rating | faixa | jogos | tendência      │
├ Evolução por rodada ─────────────────┬ Distribuição por faixa ───────┤
├ Lançamentos por partida ─────────────┬ Ajustes de série separados ───┤
├ Histórico de alterações e versão ────┴───────────────────────────────┤
└ Metodologia publicada e aviso de parâmetros pendentes ──────────────┘
```

- Nunca exibir thresholds S-F, amostra mínima ou fórmula de soft reset como definitivos antes da decisão formal.
- Lançamento-base aponta para `partidaId`; ajuste de Série aponta para `serieId`. Ambos mostram versão do algoritmo e instante de consolidação sem misturar as origens.
- Correções produzem novo lançamento compensatório; não apagam o histórico apresentado.
- No mobile, posição, rating e elegibilidade aparecem primeiro; ranking usa cards ordenados e paginação explícita.

## Estados obrigatórios por tela

Cada tela deve especificar e testar:

- carregamento inicial com skeleton;
- atualização preservando conteúdo conhecido;
- sucesso com dados completos;
- dados parciais identificados;
- vazio por ausência de registros;
- vazio causado por filtros;
- não elegível;
- proibido sem revelar recurso sensível;
- falha recuperável com nova tentativa;
- conflito de versão com recarregamento seguro;
- conteúdo em português e inglês sem overflow;
- navegação por teclado, foco visível e movimento reduzido.
