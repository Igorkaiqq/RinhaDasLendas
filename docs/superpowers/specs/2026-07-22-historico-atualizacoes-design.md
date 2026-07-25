# Histórico de Atualizações do Sistema

**Data:** 2026-07-22
**Feature:** `019-historico-atualizacoes`
**Branch:** `feature/019-historico-atualizacoes`

## Objetivo

Criar uma área autenticada de atualizações do RinhaDasLendas que comunique novidades, melhorias, correções, segurança e infraestrutura de forma editorial, pesquisável e fácil de manter. A página também consolidará o histórico já existente do produto em grandes entregas, evitando transformar commits técnicos em conteúdo visível ao usuário.

## Público E Acesso

- A página será acessível a todos os usuários autenticados.
- A rota será `/atualizacoes`.
- O item `Atualizações` ficará na navegação principal.
- A primeira versão não terá página pública nem restrição por role.
- O frontend guardará localmente a atualização mais recente visualizada para exibir ou remover o badge `Novo`.

## Fonte De Conteúdo

O histórico será curado no código e versionado junto das mudanças que descreve.

Um registro TypeScript tipado armazenará somente dados estruturais:

- ID estável;
- versão;
- data de publicação;
- categorias;
- áreas afetadas;
- destaque da release;
- chaves de tradução;
- links internos opcionais.

Todo texto visível ficará em `FrontEnd/src/i18n/locales/pt.json` e `FrontEnd/src/i18n/locales/en.json`. O registro não poderá conter títulos, resumos, descrições ou rótulos hardcoded.

Não serão criados banco, migration, endpoint backend ou painel administrativo nesta versão.

## Versionamento

As atualizações usarão versão baseada em data no formato:

```text
AAAA.MM.N
```

Exemplo:

```text
2026.07.1
```

O sufixo `N` será sequencial dentro do mês. A versão identifica uma entrega editorial e não precisa corresponder a uma única feature ou commit.

## Categorias

Categorias permitidas:

- `feature`: novidade;
- `improvement`: melhoria;
- `fix`: correção;
- `security`: segurança;
- `infrastructure`: infraestrutura.

Uma atualização pode conter mais de uma categoria. Cada item detalhado pertence a exatamente uma categoria.

## Áreas Afetadas

Áreas iniciais:

- plataforma;
- jogadores;
- times;
- usuários;
- drafts;
- Discord;
- segurança;
- infraestrutura.

As áreas serão valores tipados e terão rótulos localizados. Novas áreas exigirão atualização do tipo, dos dois catálogos e dos testes de paridade.

## Histórico Inicial

O conteúdo existente será reconstruído a partir de specs, planos, documentação e histórico Git. Não será criada uma entrada por commit.

Marcos iniciais:

1. Fundação da plataforma, padrões arquiteturais e internacionalização.
2. Cadastro de jogadores, preferências de rota e times.
3. Usuários, autenticação, RBAC e vínculo entre usuário e jogador.
4. Draft visual e montagem de times.
5. Draft em tempo real, turnos, picks e reconexão.
6. Integração Discord, vínculo de contas e confirmação de presença.
7. Segurança, deploy, observabilidade e evolução da identidade visual.
8. Confiabilidade operacional de drafts, publicações Discord, auditoria e recuperação.

Cada marco terá data e versão coerentes com o histórico real, resumo editorial e detalhes agrupados por categoria. A release mais recente será a entrega de confiabilidade operacional da feature 017.

### Detalhamento Da Release Mais Recente

A release de confiabilidade operacional terá tratamento editorial mais detalhado por já corresponder a mudanças solicitadas por usuários em produção. Ela não poderá resumir todas as alterações sob um único item genérico.

Cada melhoria abaixo será apresentada individualmente, com título curto e descrição de uma a três frases explicando o que mudou e o benefício prático:

1. **Acesso direto ao draft pelo Discord:** links de convite passam a abrir automaticamente o draft correspondente, eliminando a seleção manual na listagem.
2. **Tratamento de links inválidos:** quando o draft indicado não existir ou não estiver acessível, a página explicará o problema sem impedir que o usuário continue navegando pelos drafts disponíveis.
3. **Confirmações administrativas contextuais:** cancelamento de draft, remoção de presença e republicações usarão modais integrados à interface no lugar dos prompts nativos do navegador, mostrando o contexto e o impacto da ação.
4. **Status separado das publicações:** administradores poderão distinguir o estado da lista de presença, da chamada com menção e dos times publicados no Discord.
5. **Recuperação individual de falhas:** cada publicação com falha poderá ser recuperada separadamente, sem repetir mensagens que já foram publicadas com sucesso.
6. **Proteção contra mensagens duplicadas:** reinícios ou execução simultânea de instâncias do bot não deverão duplicar publicações no canal.
7. **Presença atualizada em tempo real:** confirmações, cancelamentos e alterações administrativas aparecerão para quem estiver com o draft aberto, inclusive após uma reconexão.
8. **Operações de presença mais consistentes:** confirmações e cancelamentos repetidos ou simultâneos não deverão duplicar jogadores nem gerar erros inesperados.
9. **Busca manual de jogadores mais precisa:** a busca mostrará somente jogadores elegíveis e ainda não confirmados, descartando resultados antigos quando o termo ou o draft ativo mudar.
10. **Mais transparência nas ações administrativas:** cancelamentos, remoções, adições manuais e republicações registrarão motivo e responsável quando aplicável.
11. **Mensagens mais claras no bot:** integração desativada, datas passadas, datas inválidas e falhas conhecidas serão explicadas com orientação específica em vez de mensagens genéricas.
12. **Diagnóstico de permissões do Discord:** o bot diferenciará problemas de acesso ao canal, envio de mensagens, embeds e menções, facilitando a correção da configuração.
13. **Chamada de presença independente:** a mensagem que menciona o cargo configurado terá estado e recuperação próprios; uma falha nela não invalidará nem repetirá a lista principal de presença.
14. **Fila de publicações mais confiável:** publicações pendentes ou em andamento continuarão sendo processadas mesmo sem guild associada, com muitos drafts ou após falhas em outros itens do ciclo.
15. **Reforços internos de segurança e estabilidade:** controles de acesso, proteção contra excesso de requisições, validações e isolamento de dados operacionais serão resumidos em linguagem clara, sem expor tokens, endpoints ou detalhes sensíveis.

Os itens de maior destaque visual serão acesso direto, modais contextuais, status de publicação, recuperação individual, presença em tempo real e busca manual. Segurança, rate limiting e mecanismos internos de concorrência aparecerão como benefícios de estabilidade, sem dominar o conteúdo da release.

## Experiência Visual

A página seguirá uma linha do tempo editorial alinhada ao design system dark-first e à linguagem visual de arena competitiva.

### Hero

- título `Atualizações do sistema`;
- resumo da atualização mais recente;
- versão e data;
- áreas afetadas;
- categorias principais;
- tratamento visual especial usando somente tokens existentes.

### Filtros

- busca textual por título, resumo e detalhes localizados;
- chips de categoria;
- opção de limpar filtros;
- quantidade de resultados;
- estado vazio com mensagem e ação localizadas.

### Timeline

- agrupamento por ano e mês;
- eixo vertical com marcadores roxo/azul derivados dos tokens oficiais;
- cards em ordem cronológica decrescente;
- card mais recente visualmente destacado;
- versão, data, título, resumo, categorias e áreas afetadas;
- detalhes expansíveis por categoria;
- links internos opcionais para as áreas relacionadas.

### Desktop

- hero em largura confortável;
- índice lateral de versões quando houver espaço;
- timeline como conteúdo principal;
- filtros visíveis sem bloquear a leitura.

### Mobile

- uma coluna;
- filtros horizontalmente roláveis;
- timeline alinhada à esquerda;
- cards ocupando a largura disponível;
- detalhes, links e ações acessíveis por toque e teclado.

## Navegação E Badge Novo

O item `Atualizações` será incluído na sidebar e na navegação mobile.

O registro determina a versão mais recente. O frontend compara essa versão com um valor salvo em `localStorage`:

```text
rinha:last-seen-system-update
```

Regras:

- sem valor salvo ou com versão diferente: mostrar badge `Novo`;
- ao abrir a página: salvar a versão mais recente;
- após salvar: remover o badge sem recarregar;
- falha ou indisponibilidade do `localStorage`: manter a navegação funcional, sem lançar erro;
- o estado é local ao navegador e não sincroniza entre dispositivos.

## Componentes E Responsabilidades

### Registro De Atualizações

Responsável por declarar o conteúdo estrutural e exportar a coleção imutável de releases.

### Serviço De Atualizações

Responsável por:

- ordenar releases;
- identificar a mais recente;
- filtrar por texto e categoria;
- validar links internos;
- ler e gravar a versão visualizada com fallback seguro.

### Página De Atualizações

Responsável por estado de busca, filtros, expansão dos cards e renderização responsiva.

### Componentes Visuais

Componentes focados:

- hero da release mais recente;
- barra de filtros;
- timeline;
- card de atualização;
- grupo de detalhes por categoria;
- estado vazio.

Componentes existentes do design system serão reutilizados antes de criar novas primitivas.

## Manutenção Futura

Será criado um guia curto em `docs/` com o processo:

1. escolher ou incrementar a versão mensal;
2. adicionar estrutura ao registro;
3. adicionar todas as chaves em português e inglês;
4. classificar itens e áreas afetadas;
5. incluir links internos somente quando úteis;
6. executar testes de contrato e builds;
7. commitar a atualização junto da feature ou correção.

Toda mudança visível ao usuário deverá considerar uma entrada. Mudanças internas pequenas poderão ser agrupadas na próxima release para evitar ruído editorial.

O checklist padrão de implementação deverá perguntar explicitamente se o histórico de atualizações foi revisado.

## Validações Automatizadas

O registro será validado por testes que garantem:

- IDs únicos;
- versões únicas no formato `AAAA.MM.N`;
- datas ISO válidas;
- ordem cronológica decrescente;
- pelo menos uma categoria e uma área por release;
- pelo menos um detalhe por release;
- categorias e áreas reconhecidas;
- todas as chaves existentes em português e inglês;
- links internos pertencentes às rotas conhecidas;
- release mais recente determinada sem configuração duplicada.

## Testes De Interface

- rota exige autenticação;
- item aparece na sidebar e navegação responsiva;
- hero representa a release mais recente;
- busca considera conteúdo localizado;
- filtros podem ser combinados e limpos;
- timeline mantém ordem e agrupamento;
- detalhes são expansíveis por teclado;
- estado vazio é localizado;
- badge `Novo` aparece e desaparece corretamente;
- falha de `localStorage` não quebra a página;
- português e inglês renderizam conteúdo equivalente;
- desktop e mobile carregam sem overflow ou ações inacessíveis.

## Acessibilidade

- timeline usará estrutura semântica de lista;
- filtros terão nomes acessíveis e estado selecionado;
- detalhes expansíveis usarão controles com `aria-expanded`;
- foco será visível com tokens existentes;
- datas usarão elemento semântico `time`;
- ícones decorativos serão ignorados por leitores de tela;
- o badge `Novo` terá significado textual e não dependerá somente de cor.

## Estados De Erro

Como o conteúdo é compilado junto da aplicação, não haverá erro de rede para carregar o histórico.

Erros possíveis serão tratados assim:

- registro inválido: falha de teste/build;
- chave i18n ausente: falha de teste;
- link interno inválido: falha de teste;
- `localStorage` indisponível: fallback em memória durante a sessão;
- busca sem resultado: estado vazio localizado.

## Fora De Escopo

- painel administrativo de publicação;
- persistência backend;
- geração automática a partir de commits;
- integração com GitHub Releases;
- notificações push, e-mail ou Discord;
- comentários ou reações dos usuários;
- sincronização do badge entre dispositivos;
- imagens específicas por release.

## Critérios De Conclusão

- Todos os usuários autenticados acessam `/atualizacoes`.
- A sidebar indica quando existe atualização ainda não visualizada no navegador.
- A página apresenta os oito marcos históricos iniciais, com destaque para a release mais recente.
- Busca, filtros, expansão e links funcionam por mouse, teclado e toque.
- Desktop e mobile respeitam o design system e não apresentam overflow horizontal.
- Nenhum texto novo fica hardcoded.
- Português e inglês possuem estrutura e conteúdo equivalentes.
- Registro, rotas, versões, datas e chaves são validados automaticamente.
- O guia de manutenção explica como atualizar o histórico em features futuras.
