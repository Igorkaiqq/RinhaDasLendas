# Research: Fundação Competitiva Sazonal

## 1. Arquitetura e limites funcionais

**Decision**: Implementar a feature no monólito modular existente, preservando as camadas `Api`, `Application`, `Domain`, `Infrastructure` e `Tests`. Seasons, competições e rodadas pertencem à fronteira lógica Competição; séries, partidas, picks e Fearless pertencem à fronteira Partidas.

**Rationale**: A separação acompanha `docs/statistics/ARCHITECTURE.md`, mantém transações locais e não adiciona custo operacional incompatível com o produto interno. As fronteiras se comunicam por IDs estáveis e consultas explícitas, sem novos projetos ou processos.

**Alternatives considered**:

- Microsserviços por fronteira: rejeitados por custo operacional e ausência de necessidade de escala.
- Um único agregado para todo o calendário e suas séries: rejeitado porque produziria contenção, carregamento excessivo e acoplamento entre ciclos de vida distintos.

## 2. Calendário e concorrência de Season

**Decision**: Persistir um registro singleton `CalendarioCompetitivo` com versão global e `seasonAtivaId`. Criar Season altera apenas a coleção; ativar uma Season trava e atualiza o calendário e as Seasons envolvidas na mesma transação. O cliente envia `If-Match` com a versão observada do calendário.

**Rationale**: A versão global fornece uma precondição inequívoca para ativações concorrentes e permite encerrar a Season anterior atomicamente. Uma restrição parcial no PostgreSQL continua garantindo no máximo uma Season `Ativa`, e as regras de período também são validadas no domínio e no banco.

**Alternatives considered**:

- Usar apenas a versão da Season alvo: rejeitado porque duas Seasons planejadas diferentes poderiam ser ativadas com versões individualmente válidas.
- Inferir Season ativa pela data: rejeitado porque datas são administráveis e não devem reatribuir histórico.

## 3. Período e horário de negócio

**Decision**: Season usa datas locais com intervalo `[dataInicio, dataFimExclusiva)`. Agendamentos armazenam instante UTC e o frontend apresenta em `America/Sao_Paulo`; `DiariaTemporaria` também armazena sua `dataLocal` nesse fuso.

**Rationale**: Datas evitam ambiguidade para o recorte sazonal, enquanto instantes UTC preservam ordenação e interoperabilidade. O fuso de negócio permanece explícito.

**Alternatives considered**:

- Armazenar todos os horários como local sem offset: rejeitado por ambiguidade.
- Usar fim inclusivo: rejeitado por complicar Seasons consecutivas.

Toda Série usa como data competitiva a data de `agendadaPara` convertida para `America/Sao_Paulo`, que deve pertencer ao intervalo da Season. Em `DiariaTemporaria`, `dataLocal` deve ser exatamente igual a essa data derivada. A regra vale também para amistosos, pois toda Série pertence a uma Season.

## 4. Agregados de competição

**Decision**: `Season`, `Competicao` e `Serie` são raízes independentes. `Rodada` pertence à Competição e possui ordem única. `VersaoRegras` pertence sempre à Season, pode ser publicada no escopo geral ou vinculada a uma Competição, é imutável depois da publicação e é referenciada pela Série. Regra de competição é obrigatória para Série oficial; amistoso sem competição usa regra geral da Season. Nesta fatia, Competição e Rodada não recebem ciclos de vida adicionais: existem dentro da Season e a operação oficial é controlada pelo estado da Season e pela regra publicada.

**Rationale**: O modelo protege as invariantes necessárias sem criar estados não pedidos. Uma Série histórica mantém a versão exata das regras mesmo quando uma versão posterior é publicada.

**Alternatives considered**:

- Copiar apenas campos de regra na Série: rejeitado porque perderia a identidade auditável da publicação.
- Exigir Competição em amistoso apenas para obter regra: rejeitado porque contradiz o contrato de competição opcional para amistosos.
- Adicionar estados completos a Competição e Rodada: adiado por não resolver requisito desta entrega.

## 5. Série como agendamento

**Decision**: Não criar um agregado separado de agendamento. Uma Série em `Agendada`, com `agendadaPara` e lados esperados, satisfaz o compromisso esportivo; ela pode existir sem lista de presença. Partidas são adicionadas sob demanda até o limite competitivo do formato.

**Rationale**: O estado `Agendada` já possui todos os dados e transições exigidos. Um segundo recurso duplicaria natureza, lados, horário, Season e regulamento.

**Alternatives considered**:

- `AgendamentoConfronto` separado que depois cria Série: rejeitado por duplicar estado e exigir sincronização sem benefício atual.
- Pré-criar três ou cinco Partidas: rejeitado porque remakes e encerramento antecipado tornam a quantidade real variável.

## 6. Lados e snapshots

**Decision**: Cada Série possui exatamente dois `LadoSerie` imutáveis. O snapshot registra tipo (`Temporario` ou `TimeOficial`), referência de origem, nome, tag opcional, capitão quando aplicável e participantes esperados disponíveis no momento da criação. `DiariaTemporaria` copia capitães e participantes dos dois lados do `DraftMontagem` finalizado; `ConfrontoOficial` referencia e copia identidade dos dois Times. A escalação efetiva por Partida será enriquecida pela feature 024.

**Rationale**: O histórico não muda quando Draft ou Time é alterado, sem antecipar o modelo temporal de elenco e escalação.

**Alternatives considered**:

- Referenciar somente entidades mutáveis: rejeitado porque reescreveria a apresentação histórica.
- Transformar equipe temporária em Time oficial: rejeitado pelos ADRs e pelo domínio existente.

## 7. Evento de quatro times

**Decision**: `EventoCompetitivo` é agregado próprio, vinculado a uma Season e exatamente quatro Times oficiais distintos. Séries são associadas por referência e devem usar draft padrão e `fearlessHabilitado=false` antes de possuir picks.

**Rationale**: Evento é contexto organizacional, não subtipo de Série. Restringir a Times oficiais corresponde à linguagem aceita de evento com quatro times e evita inventar um ciclo de equipes temporárias fora do Draft.

**Alternatives considered**:

- Usar `Evento` como `tipoSerie`: rejeitado porque mistura contêiner e confronto.
- Aceitar equipes temporárias no evento nesta feature: adiado até existir requisito e fluxo de origem explícitos.

## 8. Partidas, resultado e Fearless

**Decision**: Partida é entidade do agregado Série nesta fundação. Registra ordem, estado, vencedor quando aplicável, motivo de término, decisão de remake e dez picks confirmados associados aos dois lados. O identificador canônico de campeão é o `championId` numérico estável da Riot; nome e arte são resolvidos somente para apresentação.

**Rationale**: Série precisa avaliar atomicamente placar, limite MD3/MD5 e bloqueios de picks. IDs numéricos não dependem de idioma nem do patch de apresentação.

**Alternatives considered**:

- Nome localizado do campeão como chave: rejeitado por instabilidade e i18n.
- Projeção Fearless persistida como fonte da verdade: rejeitada porque pode divergir após correção.

## 9. Remake e correção

**Decision**: Um remake exige `PreservarPicks` ou `DesconsiderarPicks` antes da próxima confirmação. Correções são novos registros append-only com antes/depois, ator, instante e justificativa. Após corrigir picks, a Série reconstrói Fearless e marca partidas posteriores incompatíveis com `conflitoFearless=true`; enquanto houver conflito, novas confirmações e conclusão ficam bloqueadas. O conflito é resolvido corrigindo ou anulando a partida afetada. Em Série concluída, a correção permanece `Concluida` somente se os fatos corrigidos ainda produzirem um vencedor válido; se removerem a condição de vitória, a mesma operação exige confirmação explícita e transiciona a Série para `Anulada`. Inverter o vencedor é permitido quando o placar corrigido continua conclusivo e fica auditado.

**Rationale**: A decisão torna a inconsistência visível e impede que o sistema continue acumulando fatos inválidos sem apagar histórico.

**Alternatives considered**:

- Invalidar automaticamente partidas posteriores: rejeitado porque seria uma decisão esportiva silenciosa.
- Apenas avisar e continuar: rejeitado porque violaria Fearless.

## 10. Elegibilidade oficial

**Decision**: A natureza da Série determina elegibilidade no servidor. `DiariaTemporaria` e `ConfrontoOficial` são oficiais; `Amistoso` nunca alimenta Score, Rating, MVP/SVP, rankings, recordes ou qualificação, ainda que use Fearless. Série cancelada ou anulada também é inelegível.

**Rationale**: A regra deve ser uma propriedade derivada do domínio, não uma opção enviada pelo cliente.

**Alternatives considered**:

- Campo editável `oficial`: rejeitado por permitir combinações contraditórias.
- Filtragem somente no frontend: rejeitada porque o backend é fonte de verdade.

## 11. API, idempotência e concorrência

**Decision**: Expor REST em `/api/v1`, DTOs e envelopes localizados. Coleções são paginadas. Mutações usam `Idempotency-Key`; alterações de recurso versionado também exigem `If-Match`. Como Partida é entidade interna da Série, toda mutação de Partida recebe e devolve a ETag da Série, protegendo placar e Fearless entre partidas diferentes. A chave é vinculada a ator, método, rota e hash canônico e fica retida por 90 dias com status, referência do resultado e representação original mínima. Replay idêntico retorna a resposta original e `Idempotency-Replayed: true`; conteúdo divergente ou versão obsoleta retorna `409`.

**Rationale**: A combinação impede duplicação por retry e sobrescrita concorrente. Noventa dias cobre a operação interna e evita retenção indefinida; não se armazena payload bruto, credencial ou segredo.

**Alternatives considered**:

- Versão no corpo: rejeitada para os novos contratos porque `ETag/If-Match` separa metadado de concorrência do DTO de negócio.
- Idempotência apenas em criações: rejeitada porque correções e transições também podem ser repetidas após timeout.

## 12. Eventos e efeitos assíncronos

**Decision**: Agregados emitem eventos de domínio fundamentais, processados dentro do caso de uso para auditoria e consistência. Não introduzir outbox, inbox ou worker genérico nesta feature porque não existe consumidor assíncrono ou efeito externo no escopo. O primeiro consumidor externo deverá gravar evento de integração em outbox na mesma transação, conforme `docs/statistics/DOMAIN_EVENTS.md`.

**Rationale**: Eventos de domínio expressam fatos atuais sem construir infraestrutura especulativa. A ausência de Discord, Riot e Analytics nesta fatia elimina necessidade de entrega assíncrona.

**Alternatives considered**:

- Criar outbox completa sem consumidor: rejeitada como complexidade prematura.
- Publicar efeitos antes do commit: rejeitado por inconsistência.

## 13. Autorização esportiva

**Decision**: Adicionar `Presidente` e `VicePresidente` à hierarquia sem inferir autorização apenas pelo nível. A matriz inicial é explícita:

| Capability | SuperAdmin | Presidente | VicePresidente | Admin | Moderador | Capitão | Jogador |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `CanManageSeasons` | Sim | Sim | Não | Não | Não | Não | Não |
| `CanManageCompetitions` | Não | Sim | Não | Condicional | Não | Não | Não |
| `CanManageMatches` | Condicional | Sim | Não | Sim | Condicional | Não | Não |
| `CanFinalizeMatches` | Condicional | Sim | Não | Sim | Condicional | Não | Não |
| `CanViewCompetitiveAudit` | Sim | Sim | Não | Condicional | Não | Não | Não |

Condições de recurso da feature 023:

- SuperAdmin usa `CanManageMatches`/`CanFinalizeMatches` somente em correção ou anulação técnica auditada com justificativa; não agenda, inicia nem confirma resultado esportivo normal;
- Admin configura competição e rodada somente em Season `Planejada` ou `Ativa` antes de existir Série iniciada naquele escopo; não publica regras nem cria/altera Evento;
- Moderador opera e confirma somente `DiariaTemporaria` ou `Amistoso`; não opera `ConfrontoOficial`, Evento, correção ou anulação;
- Admin consulta auditoria somente de Série e Partida; calendário, Season, competição, rodada, regra e Evento permanecem com Presidente/SuperAdmin;
- Presidente possui as operações esportivas da feature; SuperAdmin conserva gestão técnica de Season conforme decisão explícita, mas não herda configuração de competição/publicação de regra;
- a resposta de cada recurso inclui `acoesPermitidas`, calculadas no servidor, para a UI representar as condições sem reproduzir a policy.

`CanManageMatches` preserva Admin/Moderador dos fluxos atuais e passa a incluir Presidente, mas suas condições são verificadas no caso de uso. A concessão de qualquer capability esportiva ao VicePresidente fica negada e fora desta implementação até decisão explícita.

**Rationale**: Cumpre a decisão já fechada de Season e não resolve silenciosamente os poderes finos ainda pendentes. Policies, não comparações de nível, separam autoridade técnica e esportiva.

**Alternatives considered**:

- Dar ao VicePresidente todas as capabilities do Presidente: rejeitado porque a governança está pendente.
- Reutilizar somente `CanManageMatches`: rejeitado por conceder operações distintas a papéis atuais de forma ampla.

## 14. Auditoria consultável

**Decision**: Além da consulta contextual por Série, expor uma consulta administrativa de auditoria competitiva filtrável por tipo e ID de recurso. Season, calendário, competição, rodada, versão de regras, evento, Série e Partida usam a mesma projeção protegida por `CanViewCompetitiveAudit`.

**Rationale**: Registrar auditoria sem meio autorizado de recuperação não atende a resolução de disputas. O filtro explícito evita aplicar Season ativa ou enumerar recursos por engano.

**Alternatives considered**:

- Criar endpoint diferente para cada agregado: rejeitado por duplicação de contrato e projeção.

## 15. Interface e seleção sazonal

**Decision**: Entregar telas operacionais de Seasons/competições e séries/partidas mínimas, substituindo o placeholder de Partidas e adicionando navegação para Seasons/Séries conforme o shell atual. O seletor sazonal mantém o recorte na URL, permite atual, múltiplas ou todas e exibe calendário não configurado. Componentes reutilizam os tokens e padrões existentes; não são criados novos tokens.

**Rationale**: Os cenários independentes e o objetivo de cadastro em cinco minutos exigem fluxo utilizável, responsivo e não apenas API.

**Alternatives considered**:

- API apenas: rejeitada porque não satisfaz a operação pelo membro.
- Nova identidade visual competitiva: adiada, pois permanece decisão pendente.

## 16. Internacionalização

**Decision**: Todo texto de API/domínio usa `MessageCodes` e resources `Messages*.resx`; toda string da UI usa chaves espelhadas em `pt.json` e `en.json`. Enums trafegam como códigos estáveis e são traduzidos apenas na borda.

**Rationale**: Evita persistir texto localizado, atende PT/EN e preserva contratos independentes do idioma.

**Alternatives considered**:

- Mensagens hardcoded nos handlers ou componentes: rejeitadas pelo padrão obrigatório do projeto.

## 17. Testes e operação

**Decision**: Cobrir regras de domínio, validators, handlers, persistência/constraints, autorização negativa, contratos HTTP e componentes/serviços Vue. Backend roda no devcontainer; frontend usa Vitest e build TypeScript. A validação manual cobre PT-BR, EN-US, desktop e 320 px.

**Rationale**: Seasons concorrentes, Fearless e correções são invariantes críticas e precisam de evidência automatizada antes da entrega.

**Alternatives considered**:

- Testes apenas end-to-end: rejeitados por diagnóstico lento e cobertura insuficiente das transições.

## Decisões pendentes preservadas

Os itens de `docs/statistics/OPEN_DECISIONS.md` não recebem valor definitivo nesta feature. Em especial, o VicePresidente não recebe capabilities esportivas por inferência. Fórmulas, retenção de payload bruto, backfill, integração e identidade visual permanecem fora do escopo.

Correção retroativa significa somente corrigir ou anular Série/Partida já existente e vinculada à Season encerrada. Criar Série ausente, importar fatos históricos ou preencher lacunas é backfill e permanece proibido até a decisão correspondente.
