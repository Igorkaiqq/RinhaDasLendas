# Design: Modal Contextual para Ações do Draft

## Contexto

A tela de drafts usa `window.prompt` para solicitar motivos em quatro ações: cancelar draft, remover presença manual, republicar presença e republicar times. O prompt nativo não segue o design system, apresenta contexto insuficiente, tem comportamento visual diferente entre navegadores e não oferece controle adequado de foco, carregamento ou validação.

## Objetivo

Substituir todos os prompts nativos da tela de drafts por um modal contextual reutilizável, acessível e coerente com a linguagem visual da Mesa de Draft. O usuário deve entender o que será alterado, por que um motivo é solicitado e qual ação será executada.

## Escopo

O novo modal atende exclusivamente:

- cancelamento de draft;
- remoção manual de presença;
- republicação da lista de presença no Discord;
- republicação dos times no Discord.

Não altera regras de negócio, contratos da API ou permissões existentes. A mudança substitui somente a coleta e confirmação do motivo no frontend.

## Componente

Criar `DraftReasonDialog.vue` como componente controlado, composto pelos elementos de `FrontEnd/src/components/ui/dialog/`, baseados em Reka UI. Ele não chama serviços diretamente e não conhece a estrutura completa de `DraftsView.vue`.

### Entradas

- `open`: controla visibilidade;
- `action`: união tipada entre `cancelDraft`, `removeManualPresence`, `republishPresence` e `republishTeams`;
- `saving`: bloqueia interações durante envio;
- `context`: dados opcionais de jogador e status atual da publicação.

A própria ação determina título, descrição, rótulo, sugestão de motivo, texto de confirmação e variante visual por meio das chaves de i18n. Isso evita combinações inválidas de propriedades e reduz a configuração exigida pela view.

### Saídas

- `confirm`, contendo o motivo normalizado;
- `cancel`, sem payload.

O componente mantém apenas o valor temporário do campo. A view continua responsável por selecionar a ação, executar o serviço correto, tratar erros e atualizar o draft.

## Apresentação

O modal segue a opção visual aprovada de modal contextual:

- overlay com token `overlay`;
- superfície `surface-2`, borda `hairline-strong`, raio `xl` e sombra sutil;
- kicker em tipografia mono para `Discord` ou `Ação administrativa`;
- título, descrição do impacto e campo de motivo em hierarquia vertical;
- card contextual para publicação Discord, mostrando `Lista de presença` ou `Times definidos` e o status atual;
- contexto da remoção mostra o nome do jogador;
- botão secundário `Voltar`;
- botão primário roxo para republicações;
- botão `danger` para cancelamento e remoção;
- largura adaptável, sem ultrapassar a viewport móvel.

Nenhum token novo será criado. O componente reutiliza classes e tokens existentes.

## Comportamento

- Ao abrir, o campo recebe o motivo padrão localizado e ganha foco.
- O usuário pode editar completamente a sugestão.
- Motivo vazio ou composto apenas por espaços não pode ser enviado.
- `Enter` no formulário confirma quando o valor é válido.
- `Escape` cancela quando não há envio em andamento.
- O botão principal e o fechamento ficam bloqueados durante `saving`.
- Envios repetidos são impedidos enquanto a primeira requisição está em andamento.
- Ao cancelar ou concluir com sucesso, o estado temporário é limpo.
- Erros da API mantêm o modal aberto e usam o fluxo localizado de erros existente.

## Integração na View

`DraftsView.vue` mantém uma única ação pendente como união discriminada:

- `cancelDraft`;
- `removeManualPresence`, contendo jogador;
- `republishPresence`;
- `republishTeams`.

Os quatro pontos que hoje chamam `window.prompt` passam a configurar essa ação pendente. Um único handler de confirmação recebe o motivo e encaminha para o serviço correspondente.

O modal recebe o status atual da publicação por meio dos dados já carregados no draft. Nenhuma requisição adicional é necessária para abri-lo.

## Acessibilidade

- `role="dialog"`, `aria-modal="true"`, portal e contenção de foco fornecidos pelos componentes Reka UI existentes;
- título associado por `aria-labelledby`;
- descrição associada por `aria-describedby`;
- rótulo explícito para o campo;
- foco inicial no campo de motivo;
- foco visível conforme os tokens oficiais;
- retorno do foco ao botão que abriu o modal;
- contenção de foco dentro do modal enquanto aberto;
- suporte a teclado e viewport móvel.

## Internacionalização

Todos os novos títulos, descrições, rótulos, sugestões, botões, contextos e validações usam chaves em `FrontEnd/src/i18n/locales/pt.json` e `FrontEnd/src/i18n/locales/en.json`.

As sugestões existentes de motivo podem ser reutilizadas quando forem claras. Cada chave adicionada deve existir nos dois idiomas. Não haverá texto visível hardcoded no componente ou na view.

## Testes

### Componente

- renderiza título, descrição, contexto e sugestão da ação;
- aplica variante `discord` ou `danger`;
- rejeita motivo vazio;
- emite motivo normalizado ao confirmar;
- emite cancelamento por botão e `Escape`;
- bloqueia confirmação e fechamento durante `saving`;
- move o foco para o campo ao abrir.

### Integração da View

- cada um dos quatro botões abre o contexto correto;
- confirmação chama o serviço correspondente com o motivo;
- cancelamento não chama serviços;
- erro mantém o modal aberto;
- sucesso fecha o modal e atualiza o estado;
- não permanece nenhuma chamada a `window.prompt` em `DraftsView.vue`.

### Qualidade

- `pt.json` e `en.json` permanecem sincronizados;
- testes completos do frontend passam;
- build do frontend passa;
- comportamento responsivo é verificado em desktop e mobile.

## Critérios de Aceite

- Nenhuma das quatro ações exibe o prompt nativo do navegador.
- O modal informa claramente a ação e seu impacto antes da confirmação.
- Republicação de presença e de times mostra tipo e status atual da publicação.
- Remoção manual mostra o jogador afetado.
- Cancelamento e remoção usam tratamento visual destrutivo.
- Motivo vazio não é enviado.
- O modal funciona com mouse e teclado em desktop e mobile.
