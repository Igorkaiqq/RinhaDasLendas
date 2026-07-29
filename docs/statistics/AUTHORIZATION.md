# Autorização do domínio competitivo

## Princípios

- O backend é a fonte de verdade. Ocultar controles no frontend não concede nem substitui autorização.
- A identidade do ator vem do principal autenticado ou da credencial de serviço, nunca do corpo da requisição.
- Hierarquia de papel e capacidade são conceitos diferentes. Um papel superior não recebe automaticamente toda capacidade esportiva ou técnica sem policy explícita.
- Restrições de temporada, time, escalação, voto e propriedade são verificadas no caso de uso, além da proteção do endpoint.
- Operações sensíveis usam capacidade nomeada, auditoria e testes negativos.
- Recurso inacessível pode retornar `404` quando a própria existência for sensível; falta de capacidade em área conhecida retorna `403`.

## Hierarquia

Ordem administrativa oficial:

```text
SuperAdmin > Presidente > VicePresidente > Admin > Moderador > Capitão > Jogador
```

`Admin+` significa exatamente os quatro primeiros papéis:

```text
SuperAdmin, Presidente, VicePresidente e Admin
```

### Responsabilidades

- **SuperAdmin:** autoridade técnica máxima. Administra segurança, integrações, credenciais, recuperação operacional e configurações de alto risco. Não substitui silenciosamente decisões esportivas do Presidente.
- **Presidente:** autoridade esportiva máxima. Governa Seasons, competições, decisões esportivas e publicação de resultados dentro das policies aprovadas; a distribuição fina das capacidades de Season continua pendente.
- **VicePresidente:** autoridade esportiva delegada abaixo do Presidente. O detalhamento de poderes que exigem aprovação, suplência ou dupla validação permanece pendente.
- **Admin:** operação administrativa da plataforma, cadastros, importações, partidas e suporte conforme capacidade específica.
- **Moderador:** operação cotidiana limitada, revisão e moderação sem poderes estruturais, de segurança ou de governança sazonal.
- **Capitão:** responsabilidades sobre o próprio time e escalação quando a competição permitir; não é papel administrativo por herança.
- **Jogador:** consulta dados permitidos, mantém o próprio perfil e participa de ações para as quais seja elegível.

Usuários podem ter múltiplos papéis. O maior papel determina o nível administrativo, mas capacidades contextuais como capitão e eleitor continuam exigindo vínculo e elegibilidade explícitos.

## Credencial Collector

Collector é uma identidade de serviço, não um papel humano e não participa da hierarquia.

Escopos mínimos:

- `collector:match-import:create`: enviar uma coleta para importação.
- `collector:match-import:read-own`: consultar somente o estado das importações criadas pela própria credencial.

Restrições obrigatórias:

- não cria, encerra, corrige ou consolida partida;
- não marca dado como oficial;
- não lista usuários, jogadores, elencos, times, temporadas, ratings ou importações alheias;
- não vota, transfere jogador ou administra credenciais;
- não escolhe identidade humana no request;
- usa segredo rotacionável, rate limit próprio, auditoria, expiração e revogação;
- recebe apenas o escopo necessário por origem e ambiente.

## Capacidades propostas

| Capacidade | Finalidade |
| --- | --- |
| `CanViewOfficialStatistics` | consultar estatísticas e rating publicados |
| `CanManageSeasons` | criar e alterar ciclo de vida de temporadas |
| `CanManageCompetitions` | configurar competições e regras publicadas |
| `CanManageRosters` | manter elencos dentro das regras vigentes |
| `CanManageTransfers` | solicitar e operar transferências |
| `CanApproveTransfers` | aprovar ou rejeitar transferências |
| `CanManageLineups` | registrar escalações permitidas |
| `CanManageMatches` | criar e manter partidas |
| `CanFinalizeMatches` | confirmar resultado operacional |
| `CanImportMatchData` | criar importação humana |
| `CanReviewImports` | validar correspondências, erros e diferenças |
| `CanConsolidateOfficialStats` | tornar uma versão válida fonte oficial |
| `CanManageVoting` | abrir turnos, encerrar e publicar votação de série |
| `CanVoteSeriesAwards` | votar quando participante elegível da série |
| `CanModerateSeriesVotes` | consultar votos individuais em projeção administrativa auditada |
| `CanInvalidateSeriesVotes` | invalidar voto de série com motivo e auditoria |
| `CanRecalculateRatings` | solicitar recálculo técnico auditado |
| `CanViewCompetitiveAudit` | consultar trilha competitiva sensível |
| `CanManageCollectorCredentials` | emitir, rotacionar e revogar Collector |

## Matriz de permissões

Legenda: `P` permitido pela policy; `C` permitido somente com condição de recurso; `N` negado; `D` depende da decisão sobre poderes finos de Presidente e VicePresidente.

| Ação | SuperAdmin | Presidente | VicePresidente | Admin | Moderador | Capitão | Jogador | Collector |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Consultar estatísticas oficiais publicadas | P | P | P | P | P | P | P | N |
| Criar Season | P | P | D | N | N | N | N | N |
| Abrir ou encerrar Season | P | P | D | N | N | N | N | N |
| Configurar competição | N | P | D | C | N | N | N | N |
| Publicar regras esportivas | N | P | D | N | N | N | N | N |
| Criar ou manter elenco | C | P | D | P | C | C próprio | N | N |
| Solicitar transferência | C | P | D | P | C | C próprio | C própria | N |
| Aprovar ou rejeitar transferência | N | P | D | C | N | N | N | N |
| Registrar escalação | C | P | D | P | C | C próprio | N | N |
| Criar ou editar partida | C | P | D | P | C | N | N | N |
| Encerrar partida | C | P | D | P | C | N | N | N |
| Importar dados manualmente | C | C | C | P | C | N | N | N |
| Revisar importação | C | C | C | P | P | N | N | N |
| Consolidar estatística oficial | C | P | D | C | N | N | N | N |
| Abrir ou encerrar votação | N | P | D | C | C | N | N | N |
| Publicar resultado MVP/SVP | N | P | D | C | N | N | N | N |
| Votar em MVP/SVP | C | C | C | C | C | C | C | N |
| Consultar votos para moderação | P | P | P | P | N | N | N | N |
| Invalidar voto com motivo | P | P | P | P | N | N | N | N |
| Solicitar recálculo de rating | P | N | N | C | N | N | N | N |
| Consultar auditoria competitiva sensível | P | P | D | C | N | N | N | N |
| Gerenciar credencial Collector | P | N | N | N | N | N | N | N |
| Enviar coleta | N | N | N | N | N | N | N | escopo próprio |
| Consultar estado da própria coleta | N | N | N | N | N | N | N | escopo próprio |

`C` nunca significa acesso por hierarquia apenas. Exemplos:

- Capitão só mantém escalação do próprio time, na temporada e janela corretas.
- Jogador só vota se estiver autenticado, elegível e dentro da janela.
- SuperAdmin executa manutenção técnica somente quando a policy autoriza e audita; governança esportiva continua com o Presidente.
- Admin só consolida dados quando a separação de funções e o estado da revisão permitirem.

`CanManageSeasons` é concedida a SuperAdmin e Presidente. Admin e papéis inferiores são negados. O valor `D` nas ações de Season representa somente o poder fino do VicePresidente: até decisão explícita, ele não recebe a capacidade por hierarquia; a policy futura pode torná-lo condicional sem alterar as permissões já definidas dos demais papéis.

## Limites por recurso

### Temporada e competição

- Mutação exige temporada em estado compatível e versão atual.
- SuperAdmin e Presidente podem gerenciar Seasons; VicePresidente depende da decisão fina e Admin abaixo permanece negado.
- Regras publicadas são versionadas; alteração não reinterpreta partidas anteriores sem fluxo de correção.
- Fearless é recusado fora de série direta de dois lados, no evento de quatro times e em todo confronto pertencente ao evento.

### Elenco e transferência

- O ator não escolhe livremente time de autoridade; vínculos são resolvidos no servidor.
- Capitão não atua sobre outro time.
- Jogador não aprova a própria transferência.
- Aprovação valida capitão titular, cinco titulares por rota, até três reservas, máximo de oito, vínculo ativo único, janela configurada e qualificação global histórica `2/2`, sem consumir a qualificação nem alterar Partidas históricas.

### Partida e importação

- Importar, revisar e consolidar são capacidades distintas.
- Collector não herda capacidade humana e não contorna revisão.
- Amistoso não pode ser consolidado em projeção oficial mesmo por Admin+.
- Habilitar Fearless em Amistoso direto e fora de Evento não concede elegibilidade para votação, Score, Rating, ranking, recorde ou outra projeção oficial.
- Correção exige justificativa, versão esperada e auditoria.

### Votação

- O eleitor vem do principal autenticado.
- Existe uma votação por série; nenhuma partida abre votação independente.
- Eleitor deve ter participado de ao menos uma Partida válida da Série.
- Candidato a MVP pertence ao lado vencedor e candidato a SVP ao lado derrotado; substituto que jogou é elegível e integrante que não jogou não é.
- Autovoto é permitido e cada participante possui uma escolha por categoria e turno.
- Admin+ com `CanModerateSeriesVotes` pode consultar a projeção administrativa de votos da série. O acesso é sensível, exige finalidade de moderação, retorna `votoId` opaco e identidade mínima do eleitor, e registra ator, série, instante, correlação e finalidade na auditoria.
- Admin+ pode invalidar um voto com `CanInvalidateSeriesVotes`, motivo obrigatório e auditoria. A capacidade não autoriza alterar candidato, apagar voto, reabrir turno ou votar por terceiro.
- Fora da projeção administrativa auditada, nenhum papel, resultado público, endpoint de participante, log geral ou cache compartilhado expõe voto individual ou identidade do eleitor.
- A invalidação usa somente o `votoId` opaco da projeção de moderação; identidade do eleitor não pode ser usada para localizar ou invalidar voto.
- Resultado parcial não é público antes do encerramento e publicação.

### Rating

- Leitura pública autenticada acessa somente metodologia e resultados publicados.
- Recálculo não aceita rating final fornecido pelo cliente.
- Cada lançamento é derivado no servidor e vinculado a versão do algoritmo.

## Testes negativos obrigatórios

| Cenário | Resultado esperado |
| --- | --- |
| Anônimo acessa qualquer endpoint protegido | `401` sem dados |
| Jogador chama endpoint Admin+ diretamente | `403` |
| VicePresidente usa ação de Season ainda marcada `D` | negado até decisão e condição explícita |
| Admin tenta criar, abrir ou encerrar Season | `403`; nenhum estado alterado |
| SuperAdmin tenta publicar regra esportiva sem capacidade | `403` |
| Capitão altera elenco de outro time | `403` ou `404` sem revelar vínculo sensível |
| Jogador altera `actorId`, `teamId` ou `userId` no corpo | identidade controlada pelo cliente é ignorada ou request rejeitado |
| Jogador vota por outro usuário | `403`; nenhum voto criado |
| Integrante do elenco que não participou tenta votar | `403`; nenhum voto criado |
| Participante vota em candidato do lado incompatível com MVP ou SVP | validação rejeita; nenhum voto criado |
| Participante envia segunda escolha na mesma categoria e turno | repetição idêntica retorna resultado original; conteúdo divergente retorna `409` |
| Usuário inelegível vota | `403`; nenhum parcial exposto |
| Voto é enviado após fechamento | `409`; estado permanece encerrado |
| Moderador consulta votos individuais da série | `403`; nenhuma identidade ou `votoId` exposto |
| Admin+ consulta votos para moderação | retorna projeção mínima com `votoId` opaco e cria auditoria de acesso sensível |
| Consulta pública ou de participante tenta incluir identidade do eleitor | campo ausente; nenhum voto individual exposto |
| Moderador tenta invalidar voto | `403`; voto permanece válido |
| Admin+ tenta invalidar por identidade do eleitor em vez de `votoId` | `400`; nenhum voto alterado |
| Admin+ invalida voto sem motivo | `400`; voto e apuração permanecem inalterados |
| Admin+ invalida voto válido | voto permanece no histórico, deixa de contar e auditoria registra motivo e ator |
| Moderador consolida dados oficiais | `403` |
| Admin tenta incluir amistoso no rating oficial | `400` ou `409`; nenhuma projeção alterada |
| Collector lista importações de outra credencial | `403` ou `404` |
| Collector chama endpoint humano de consolidação | `403` |
| Collector usa escopo de leitura para enviar coleta | `403` |
| Credencial Collector revogada ou expirada envia coleta | `401` |
| Mesma chave idempotente chega com payload diferente | `409`; nenhuma segunda operação |
| Mutação usa versão antiga | `409`; nenhuma sobrescrita silenciosa |
| VicePresidente usa poder de Season não aprovado | negado até decisão e policy explícita |
| Evento de quatro times ou confronto pertencente solicita Fearless | validação rejeita antes da persistência |

## Auditoria

Registrar, no mínimo:

- ator confiável e tipo de identidade;
- capacidade avaliada;
- recurso e limite contextual;
- decisão permitida ou negada para ação sensível;
- instante, correlação e resultado;
- versão anterior e posterior quando houver mutação;
- justificativa quando exigida.

Não registrar token, segredo Collector, voto individual em log geral, payload bruto irrestrito ou dado pessoal desnecessário.
