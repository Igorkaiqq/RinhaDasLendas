# Armazenamento de Dados Brutos

## Princípio

O modelo oficial é relacional para dados normalizados. Armazenar payload bruto é uma **exceção auditável**, usada somente quando necessário para diagnóstico, reconciliação ou reprocessamento autorizado.

Payload bruto nunca é:

- fonte direta de regras de negócio;
- substituto para modelagem relacional;
- retornado por endpoints comuns;
- incluído em logs ou métricas;
- requisito para que o fluxo manual funcione.

Armazenar ou reprocessar conteúdo bruto não altera a natureza oficial ou amistosa da Partida, nem cria uma Série ausente. Um amistoso continua restrito a histórico e agregados amistosos separados, independentemente da qualidade ou riqueza do payload.

## Condições para armazenamento

Antes de habilitar armazenamento bruto para uma fonte, deve existir decisão registrada com:

- finalidade específica;
- campos necessários;
- processo de redação;
- responsáveis com acesso;
- prazo de retenção e descarte;
- risco de privacidade;
- procedimento de auditoria e incidente.

Sem essa decisão, conserva-se apenas metadado técnico mínimo e impressão criptográfica do conteúdo redigido.

## Redação obrigatória

A redação ocorre antes da persistência. Devem ser removidos ou irreversivelmente mascarados:

- tokens, cookies, secrets e cabeçalhos de autorização;
- credenciais e chaves de API;
- dados pessoais sem finalidade estatística;
- identificadores externos não necessários para reconciliação;
- conteúdo livre potencialmente sensível;
- metadados de transporte sem utilidade operacional.

Falha na redação impede o armazenamento. Não existe modo de contingência que grave primeiro e redija depois.

## Metadados mínimos

Quando autorizado, o registro bruto deve possuir metadados relacionais para:

- fonte e versão do contrato;
- instante de coleta e de expiração;
- chave idempotente;
- versão da política de redação;
- hash do payload redigido;
- classificação de acesso;
- motivo de retenção;
- vínculo com a execução de importação;
- estado de descarte.

Os nomes de tabelas e campos são deliberadamente deixados para a especificação.

## Acesso e auditoria

O acesso é excepcional, mínimo e auditado. Cada leitura ou exportação registra responsável, finalidade, instante e escopo. Uma permissão administrativa genérica não concede automaticamente acesso ao conteúdo bruto.

Exportação deve aplicar a mesma ou uma redação mais restritiva. Cópias locais, anexos em chamados e compartilhamento por canais externos são proibidos sem processo aprovado.

## Retenção e descarte

O prazo deve ser o menor compatível com a finalidade e definido por fonte antes da ativação. Ao expirar:

- o payload é descartado;
- metadados de auditoria não sensíveis podem permanecer;
- fatos normalizados e suas origens continuam válidos;
- projeções não passam a depender da existência do bruto.

Definir um número único de dias nesta fundação seria prematuro. A política de cada fonte precisa considerar finalidade, risco e capacidade real de reprocessamento.

## Correções e reprocessamento

Reprocessar payload bruto usa uma nova versão do normalizador e produz nova tentativa rastreável. Não altera silenciosamente fatos confirmados. Divergências seguem o processo de correção por versão ou compensação.

## Alternativas rejeitadas

- **Guardar tudo indefinidamente**: aumenta exposição sem benefício proporcional.
- **Guardar sem redação para facilitar depuração**: incompatível com segurança e auditoria.
- **Usar JSON bruto como banco estatístico**: prejudica integridade, consulta e reconstrução determinística.
- **Descartar toda procedência**: impede idempotência, reconciliação e investigação de divergências.
