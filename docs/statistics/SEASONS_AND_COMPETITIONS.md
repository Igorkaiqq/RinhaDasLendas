# Seasons, Competições e Formatos de Disputa

## Season temática da Riot

Toda partida deve pertencer a uma Season temática da Riot. A Season fornece o recorte temporal e de contexto para patches, balanceamento, métricas e rankings.

Regras de consulta:

- a Season atual é o filtro padrão;
- o usuário pode selecionar várias Seasons;
- a opção `Todas` remove o recorte de Season, sem misturar versões de algoritmo silenciosamente;
- resultados multisseason devem identificar as Seasons incluídas;
- a Season de uma partida confirmada não muda por simples alteração da Season atual.

A definição de início, fim, nome temático e critério de Season será administrada no sistema. Inferir Season apenas pela data é uma conveniência de cadastro, não uma correção silenciosa de histórico.

## Competição e natureza da Série

Uma Série pode ser:

- **Série oficial**: possui validade competitiva oficial, pertence obrigatoriamente a uma Competição e a uma rodada e alimenta projeções oficiais.
- **Série amistosa**: organizada formalmente, mas sem validade competitiva oficial; Competição é opcional ou ausente conforme o contrato.
- **Amistoso isolado**: Partida avulsa sem Série oficial; Competição é opcional ou ausente conforme o contrato.

Séries oficiais e amistosos podem ser agendados fora de uma lista de presença. A presença continua sendo forma de organizar participantes, não pré-requisito para o agendamento. O que concede validade oficial é o vínculo obrigatório da Série com Competição, rodada e regulamento aplicável.

Uma Série amistosa pode ter equipes temporárias ou Times oficiais como lados. Mesmo entre Times oficiais, ela não se torna `ConfrontoOficial` sem validade competitiva oficial.

## Subtipos de Série oficial

- **DiariaTemporaria**: origina-se de `DraftMontagem`, usa equipes temporárias formadas por capitães e pertence obrigatoriamente à Competição e à rodada do circuito diário.
- **ConfrontoOficial**: representa exclusivamente uma Série entre dois Times oficiais e também pertence obrigatoriamente à Competição e à rodada correspondente.

Os subtipos diferem pela natureza dos lados, não pela validade competitiva. Ambos alimentam estatísticas, Score, Rating, MVP/SVP, rankings e recordes oficiais quando suas Partidas forem válidas.

## DiariaTemporaria por capitães

`DiariaTemporaria` é uma Série oficial direta entre duas equipes temporárias formadas em `DraftMontagem`, identificada operacionalmente pelos capitães e pela data local em `America/Sao_Paulo`.

Regras:

- os dois capitães devem estar definidos antes da confirmação da série;
- cada Partida pertence a no máximo uma Série;
- uma série não atravessa silenciosamente a mudança da data local;
- substituições e escalações são registradas por Partida;
- resultado da Série deriva das Partidas válidas, sem apagar resultados individuais;
- a Série possui validade oficial e vínculo obrigatório com a Competição e a rodada do circuito diário, sem transformar suas equipes temporárias em Times oficiais.

Cada participação do jogador em uma Série oficial `DiariaTemporaria` válida conta uma vez para sua qualificação global. Ao completar duas Séries desse subtipo, a qualificação fica permanentemente satisfeita, não é consumida por contratação e independe de Time, janela ou Season posterior.

Séries oficiais diretas entre Times oficiais usam o subtipo `ConfrontoOficial`. Elas não são `DiariaTemporaria`, mesmo quando todas as Partidas ocorrem no mesmo dia. Os dois subtipos oficiais podem adotar Fearless quando forem disputas diretas entre dois lados e não pertencerem a Evento de quatro times. A mesma possibilidade existe para Série amistosa direta.

## Fearless em série direta

O modo Fearless pode ser configurado em qualquer Série direta de dois lados, incluindo `DiariaTemporaria`, `ConfrontoOficial` e Série amistosa, inclusive amistoso entre Times oficiais. Evento de quatro times constitui exceção integral e obrigatória.

Fearless define somente o bloqueio bilateral de campeões ao longo da Série. Ele não concede validade oficial, não transforma amistoso em `ConfrontoOficial` e não altera elegibilidade estatística.

Regra global da série:

- quando qualquer lado usa um campeão em uma Partida válida, esse campeão fica bloqueado para **ambos os lados** nas Partidas seguintes da mesma Série;
- o bloqueio considera picks efetivamente confirmados, não apenas intenção ou hover;
- um remake sem resultado competitivo deve seguir a decisão administrativa registrada sobre preservação ou reversão dos picks;
- a lista bloqueada é derivada do histórico de Partidas válidas da Série;
- o bloqueio é reiniciado ao fim da Série e não atravessa para outra Série, oficial ou amistosa.

Uma correção de pick em Partida anterior deve reconstruir a lista de bloqueios e sinalizar qualquer Partida posterior que tenha se tornado inconsistente.

## Evento com quatro times

Um evento com quatro times usa draft padrão durante todo o seu ciclo. O evento é **integralmente sem Fearless**.

Nenhum confronto, chave, semifinal, final, revanche ou série pertencente ao evento pode habilitar ou reativar Fearless. Separar dois times do evento em um confronto direto não altera essa regra. Fearless somente volta a ser elegível em outra série que não pertença ao evento de quatro times.

## Elegibilidade estatística

| Contexto | Fearless configurável | Histórico e agregados | Rinha Score | Rating | MVP/SVP | Rankings e recordes oficiais |
|---|---:|---:|---:|---:|---:|---:|
| Série oficial `DiariaTemporaria` válida e fora de Evento de quatro times | Sim | Oficiais | Sim | Sim | Sim | Sim |
| Série oficial `ConfrontoOficial` válida e fora de Evento de quatro times | Sim | Oficiais | Sim | Sim | Sim | Sim |
| Série amistosa direta fora de Evento de quatro times | Sim | Amistosos separados | Não | Não | Não | Não |
| Amistoso isolado, sem Série | Não | Amistosos separados | Não | Não | Não | Não |
| Série pertencente a Evento de quatro times | Não | Conforme natureza oficial ou amistosa | Conforme natureza | Conforme natureza | Conforme natureza | Conforme natureza |
| Partida cancelada, inválida ou sem resultado confirmado | Não aplicável | Não consolidar | Não | Não | Não | Não |

Amistosos podem manter histórico e agregados próprios, sempre identificados como amistosos. Não se pode ativar Score, Rating, MVP/SVP, rankings ou recordes oficiais para um amistoso, nem retroativamente ou por configuração de Fearless.

## Estado das definições

São decisões aceitas: vínculo obrigatório com Season; Competição e rodada obrigatórias para toda Série oficial; `DiariaTemporaria` e `ConfrontoOficial` como subtipos oficiais; projeções oficiais para ambos; Competição opcional ou ausente somente para amistoso conforme contrato; Season atual como padrão; filtros multisseason e todas; todo amistoso inelegível para projeções e recordes oficiais mesmo com Fearless; Fearless bilateral configurável em qualquer Série direta de dois lados; e ausência integral de Fearless em Evento de quatro times.

Permanecem propostas para especificação: estrutura persistida de Competição e rodada, chave de `DiariaTemporaria`, regulamentos configuráveis e tratamento exato de remake. Essas decisões técnicas não tornam opcional o vínculo competitivo de Série oficial.
