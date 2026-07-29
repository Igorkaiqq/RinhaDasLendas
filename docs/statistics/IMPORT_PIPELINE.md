# Pipeline de Importação de Partidas

## Objetivo

Definir uma importação idempotente, auditável e independente de um provedor específico. Entrada manual continua disponível quando Riot ou outra integração estiver indisponível.

## Etapas

### 1. Recepção

A entrada recebe um identificador da fonte, versão do contrato, instante de coleta e chave idempotente. A fonte pode ser API externa, arquivo autorizado ou preenchimento manual.

Nenhuma resposta externa é aceita diretamente como fato de domínio.

### 2. Redação e quarentena

Antes de qualquer armazenamento bruto, tokens, credenciais, cabeçalhos de autenticação e dados pessoais não necessários são removidos. Payload não redigido não deve ser persistido nem enviado a logs.

Conteúdo malformado ou de origem não confiável fica em quarentena lógica, sem produzir fatos ou projeções.

### 3. Validação estrutural

O pipeline valida contrato, tipos, identificadores, cardinalidade de lados, participantes, resultado e campos obrigatórios. Falhas recebem código estável, origem e diagnóstico técnico seguro.

### 4. Resolução de identidade

Identidades externas são reconciliadas com jogadores internos por vínculos explícitos. Correspondência por nome não deve confirmar automaticamente uma identidade ambígua.

Itens não resolvidos aguardam intervenção autorizada. A resolução registra quem vinculou, qual evidência foi usada e quando ocorreu.

### 5. Normalização

Campos aceitos são convertidos para o vocabulário interno: Season, Competição, rodada, natureza e subtipo da Série, formato de draft, rota, campeão, item, estatística, lado, duração e resultado. Fearless é normalizado separadamente da natureza oficial ou amistosa. Dados normalizados usam estrutura relacional.

Valores desconhecidos são preservados como pendência de mapeamento; não devem ser convertidos para zero ou categoria genérica que altere métricas.

### 6. Reconciliação

A chave da fonte, o identificador externo e uma impressão do conteúdo redigido detectam repetição. Reprocessar a mesma entrada deve retornar o resultado já conhecido, sem duplicar partida ou fatos.

Quando uma nova coleta divergir de uma versão confirmada:

- não sobrescrever o fato anterior;
- registrar a divergência;
- exigir política de correção e, quando necessário, revisão autorizada;
- gerar nova versão ou compensação após aprovação.

### 7. Confirmação dos fatos

Somente dados consistentes com as invariantes de Partidas são confirmados. A confirmação relaciona origem, versão normalizada e contexto competitivo.

Não se confirma:

- jogador duplicado na mesma Partida;
- jogador em ambos os lados;
- resultado incompatível com os lados;
- partida sem Season;
- Série oficial sem Competição ou rodada;
- `DiariaTemporaria` sem origem em `DraftMontagem` ou sem vínculo com o circuito diário;
- `ConfrontoOficial` cujos lados não sejam Times oficiais;
- pick de campeão já bloqueado pelo Fearless aplicável à série;
- uso de Fearless em qualquer confronto pertencente a evento de quatro times;
- identidade externa ambígua.

### 8. Projeção

Após a confirmação, Analytics reconstrói os recortes afetados. Partidas de `DiariaTemporaria` e `ConfrontoOficial` atualizam as projeções oficiais elegíveis; amistosos atualizam somente histórico e agregados amistosos separados, mesmo quando usam Fearless. Falha nessa etapa mantém os fatos válidos e marca as projeções como desatualizadas.

## Estados conceituais

Os estados `Recebido`, `Quarentena`, `Pendente de identidade`, `Normalizado`, `Em divergência`, `Confirmado` e `Rejeitado` são vocabulário proposto. Uma especificação posterior decidirá se são estados persistidos, resultados de processamento ou projeções operacionais.

## Idempotência e concorrência

- Uma origem não pode confirmar duas vezes a mesma Partida.
- Duas importações concorrentes devem convergir para uma confirmação ou uma divergência explícita.
- Claims, se usados, devem expirar e ser recuperáveis.
- Uma tentativa falha não pode deixar fatos parciais.
- Reprocessamento deve registrar a versão do normalizador e dos mapeamentos.

## Auditoria

Cada execução registra fonte, chave idempotente, versão, etapas concluídas, códigos de falha, duração e responsável quando manual. Logs e métricas não incluem payload, token, Riot ID completo desnecessário, Discord ID ou texto livre sensível.

## Operação manual

O modo manual deve validar as mesmas invariantes do modo integrado. A diferença está na procedência e no nível de confiança, não na autoridade para ignorar regras.

Correções manuais exigem motivo e auditoria. O pipeline deve permitir completar campos ausentes sem fabricar valores para aumentar confiança ou Score.
