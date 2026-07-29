# Elencos, Escalações e Transferências

## Escopo

Estas regras aplicam-se a Times oficiais. Times temporários de `DraftMontagem` não recebem vínculos, janelas ou transferências por consequência automática.

## Composição do Time oficial

Um Time oficial validado possui:

- um capitão titular;
- cinco titulares, exatamente um por rota: Top, Jungle, Mid, ADC e Support;
- até três reservas;
- no máximo oito jogadores vinculados ao elenco;
- capitão titular incluído entre os oito e identificado na composição;
- somente jogadores elegíveis e ativos na data de vigência.

O capitão titular deve fazer parte do elenco e da escalação válida quando atuar. A perda do vínculo do capitão exige definição organizacional de substituto antes de novo `ConfrontoOficial`.

## Vínculo único

Um jogador pode manter no máximo um vínculo ativo com Time oficial no mesmo instante. A regra deve ser protegida no domínio e, quando o modelo persistido for definido, por constraint compatível no banco.

Participar de equipe temporária, amistoso agendado, agrupado ou isolado não cria outro vínculo oficial e não viola essa unicidade.

## Histórico temporal

Entradas, saídas, mudança entre titular e reserva, rota registrada e capitania devem preservar histórico de vigência. Não se reescreve uma composição antiga para refletir o elenco atual.

Cada alteração deve registrar:

- jogador e Time oficial;
- condição anterior e nova;
- início de vigência calculado;
- responsável, instante e motivo;
- janela aplicável;
- validação organizacional.

Datas de calendário, janelas e Séries `DiariaTemporaria` são avaliadas em `America/Sao_Paulo`. Instantes técnicos devem ser persistidos em UTC sem substituir a regra de calendário local por deslocamento fixo.

## Janelas de transferência

Uma janela de transferência é uma configuração publicada pela organização, com início e fim em `America/Sao_Paulo`. Não existe cadência padrão semanal, mensal, por Season ou por competição.

Cada janela deve possuir período inequívoco e estado de publicação. Criar recorrência, inferir uma janela por calendário ou abrir uma janela automaticamente são decisões futuras e não fazem parte desta fundação.

## Qualificação global por DiariaTemporaria

A participação em **duas Séries oficiais `DiariaTemporaria` válidas** concede ao jogador qualificação global para integrar Times oficiais. A qualificação é histórica, pessoal e não consumível.

Regras:

- somente `DiariaTemporaria` concluída, validada e com participação confirmada conta;
- agendamento, cancelamento, Partida isolada ou Série inválida não conta;
- a contagem independe de Time, pedido de contratação, janela ou Season;
- séries válidas podem ter ocorrido antes de qualquer negociação;
- atingir duas Séries satisfaz a qualificação permanentemente;
- contratação, saída, transferência ou escalação não consome nem reinicia a qualificação;
- a qualificação não é reduzida posteriormente por troca de Time ou abertura de nova janela.

`DiariaTemporaria` é Série oficial vinculada obrigatoriamente à Competição e à rodada do circuito diário. Essa validade alimenta projeções oficiais e qualificação global, mas não transforma suas equipes temporárias em Times oficiais.

## Entrada de jogador

A entrada em um Time oficial exige que a qualificação global já esteja satisfeita e que as demais regras de vínculo e janela sejam válidas. Não existe espera adicional de duas Séries por Time, negociação ou janela.

## Saída e contratação fora da janela

Uma saída registrada fora da janela de transferências:

- torna o jogador livre assim que a saída for confirmada e validada;
- encerra o vínculo anterior preservando o histórico;
- bloqueia nova contratação até a abertura da próxima janela publicada pela organização;
- não permite exceção silenciosa por escalação manual ou equipe temporária.

O bloqueio impede novo vínculo oficial, mas não impede participação permitida em equipe temporária ou amistoso, seja agendado, agrupado ou isolado. Qualquer exceção competitiva futura exigirá regra organizacional explícita e auditável.

`Próxima janela` significa a janela publicada com o primeiro início posterior ao bloqueio. Se nenhuma janela posterior estiver publicada, o jogador permanece bloqueado até que a organização publique uma.

## Escalação diária

Cada Time oficial pode confirmar uma escalação diária com cinco jogadores, um por rota. Reservas elegíveis podem substituir titulares antes ou entre Partidas conforme o regulamento do confronto.

Regras de resolução:

- a escalação confirmada para uma data local vale para as Partidas subsequentes daquele dia;
- na ausência de nova confirmação, usa-se a **última escalação válida** do Time oficial;
- a última escalação válida só pode ser reutilizada se todos os jogadores continuarem elegíveis na data da nova Partida;
- se ela se tornar inválida, o confronto exige nova escalação;
- cada Partida preserva a escalação efetivamente usada, mesmo que outra seja confirmada depois.

## Validação organizacional

Alterações de elenco e escalação oficial exigem validação organizacional. A validação deve verificar, no mínimo:

- autoridade do solicitante;
- vínculo único;
- capacidade máxima de oito;
- cinco titulares por rota sem duplicidade;
- capitão pertencente ao elenco;
- vigência e janela em `America/Sao_Paulo`;
- qualificação global permanente por duas Séries oficiais `DiariaTemporaria`;
- bloqueio de contratação até a próxima janela publicada;
- situação ativa dos jogadores.

O papel exato responsável e o fluxo de aprovação são propostas para uma especificação de autorização. Não se deve reutilizar uma permissão administrativa existente apenas por conveniência.

## Relação com o modelo atual

O `Time` atual limita a composição principal a cinco e possui capitão opcional. A evolução para Time oficial não deve apagar ou reinterpretar automaticamente `TimeMembro`. É necessária uma especificação de migração que classifique registros existentes, preserve histórico e trate dados incompletos.
