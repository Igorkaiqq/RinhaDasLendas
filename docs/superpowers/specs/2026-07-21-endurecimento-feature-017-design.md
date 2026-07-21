# Endurecimento da Feature 017

**Data:** 2026-07-21
**Feature:** `017-robustecer-drafts-discord-jogadores`
**Branch:** `feature/016-melhorias-drafts-presenca-discord`

## Objetivo

Corrigir os riscos encontrados na auditoria da feature 017 antes de considerar a implementacao pronta para merge ou producao. O trabalho permanece dentro da feature atual porque sua implementacao ainda nao foi publicada e os achados afetam diretamente requisitos e contratos ja definidos.

## Estrategia

As correcoes serao implementadas sequencialmente. Cada incremento deve:

1. comecar por um teste falho que reproduza o risco;
2. aplicar a menor alteracao que corrija o comportamento;
3. executar a verificacao focada antes de seguir;
4. atualizar contratos, recursos e documentacao afetados;
5. gerar um commit isolado em portugues, sem push.

## Escopo

### Teste deterministico

O teste de conversao de data do bot nao dependera do relogio real. A validacao recebera um instante de referencia explicito para que datas de teste nao expirem com o calendario.

### Autorizacao dos comandos Discord

Comandos de consulta permanecem disponiveis conforme o comportamento atual. Comandos mutaveis aceitarao membros com `ManageGuild` ou com um dos cargos definidos em `DRAFT_ADMIN_ROLE_IDS`, lista separada por virgulas, e terao verificacao em duas camadas:

- `ManageGuild` como permissao segura padrao no registro do slash command, permitindo que administradores do servidor concedam visibilidade adicional aos cargos configurados;
- verificacao em tempo de execucao antes de qualquer chamada mutavel ao backend.

A negacao sera localizada, efemera e nao produzira efeitos colaterais. A identidade Discord do operador sera preservada para rastreabilidade quando o fluxo chegar ao backend.

### Token interno e rate limiting

O backend recusara inicializacao em producao quando o token interno estiver ausente, usar placeholder conhecido ou nao atender ao tamanho minimo definido. Nenhum segredo sera registrado em log.

O rate limiting sera particionado por identidade autenticada, identidade do bot ou endereco IP anonimo. Saturar uma particao nao podera bloquear as demais.

### Erros estruturados

Falhas conhecidas de dominio preservarao o codigo estavel da excecao em `messageCode`. A mensagem e os detalhes continuarao localizados. O bot mapeara apenas codigos publicos definidos pelo contrato, sem depender de busca por texto.

### Publicacao Discord idempotente

O backend sera a fonte de verdade para exclusao mutua da publicacao. Antes de enviar uma mensagem, o bot solicitara um claim atomico com identificador unico. Apenas o detentor do claim podera concluir ou registrar falha.

O estado persistido diferenciara uma solicitacao pendente, uma tentativa em andamento e uma tentativa de resultado desconhecido. Um claim ativo impedira pollings concorrentes. Se o bot cair depois de obter o claim e antes de concluir ou registrar falha, o timeout movera a tentativa para `RequerReconciliacao`, sem autorizar reenvio automatico.

A recuperacao de `RequerReconciliacao` exigira acao administrativa: registrar a mensagem existente ou solicitar republicacao depois de confirmar que ela nao existe. Essa escolha prioriza nao duplicar mensagens quando o resultado do envio ao Discord for desconhecido.

Esse protocolo substitui o conjunto em memoria como mecanismo de seguranca. Memoria local podera continuar apenas como otimizacao, nunca como fonte de verdade.

### Presenca idempotente e concorrente

Confirmar ou cancelar novamente o mesmo estado desejado retornara sucesso sem duplicar registros. Conflitos de persistencia serao traduzidos pela infraestrutura para um resultado de concorrencia conhecido, sem expor tipos do Entity Framework para Application ou Domain.

Ao detectar conflito, o caso de uso recarregara o agregado e retornara sucesso se o estado desejado ja tiver sido alcançado por outra requisicao. Conflitos com estado diferente continuarao sendo reportados.

### Configuracao e permissoes do bot

Toda interacao mutavel verificara `botEnabled` antes de chamar a API. Permissoes de visualizar canal, enviar mensagens e incorporar links serao verificadas separadamente. Permissao para mencionar cargo sera exigida somente quando um cargo estiver configurado.

Uma falha de publicacao de um draft nao interrompera o processamento dos demais drafts do ciclo.

### Validacao e auditoria administrativa

Motivos obrigatorios serao validados no backend com FluentValidation, incluindo valor nulo, vazio e apenas espacos. Tipos de publicacao e limites dos identificadores operacionais tambem serao validados antes de parsing ou persistencia.

A identidade administrativa devera ser valida antes da mutacao. Adicao e remocao manual, cancelamento e republicacao registrarao executor, alvo quando aplicavel, momento e motivo. Identidades ausentes ou invalidas serao rejeitadas sem alterar o agregado.

### Realtime, exposicao de dados e metricas

Mudancas de publicacao e acoes administrativas emitirao o estado atualizado via SignalR apos persistencia bem-sucedida.

Respostas comuns nao incluirao motivos administrativos nem identificadores operacionais do Discord. Esses dados ficarao em uma projecao administrativa protegida por `CanManageDrafts`.

O cancelamento de draft registrara metrica estruturada propria, sem dados pessoais ou segredos.

### Testes comportamentais

A lista estatica de endpoints nao sera considerada evidencia suficiente de cobertura. Os testes HTTP exercitarao os endpoints e verificarao:

- chamada anonima;
- usuario sem permissao;
- administrador ou moderador autorizado;
- token interno valido e invalido;
- uso do esquema de autenticacao incorreto;
- payload invalido;
- persistencia de auditoria;
- idempotencia e concorrencia;
- transicoes e exclusao mutua de publicacao.

As migracoes ainda nao publicadas poderao ser consolidadas para manter um historico coerente. A verificacao final incluira testes e builds de backend, frontend e bot, validacao de migracoes e auditoria de internacionalizacao.

## Ordem De Execucao

1. Corrigir o teste de data dependente do calendario.
2. Restringir comandos mutaveis do Discord.
3. Validar o token interno no startup de producao.
4. Particionar o rate limiter.
5. Preservar codigos de erro de dominio.
6. Implementar claim atomico para publicacao Discord.
7. Corrigir idempotencia e concorrencia de presenca.
8. Aplicar `botEnabled` e corrigir permissoes de canal.
9. Completar validacao e autoria administrativa.
10. Validar payloads de publicacao.
11. Emitir atualizacoes SignalR.
12. Restringir dados operacionais e de auditoria.
13. Adicionar metrica de cancelamento.
14. Substituir cobertura declarativa por testes comportamentais.
15. Consolidar migracoes aplicaveis e executar verificacao completa.

## Fora De Escopo

- Redesenhar a interface da mesa de draft.
- Alterar o modelo geral de roles da aplicacao.
- Substituir Discord, SignalR, MediatR ou Entity Framework.
- Criar compatibilidade com contratos que ainda nao foram publicados.
- Fazer push, merge ou abrir pull request automaticamente.

## Criterios De Conclusao

- Nenhum comando mutavel do Discord pode ser executado por membro nao autorizado.
- Producao nao inicia com token interno ausente, fraco ou placeholder.
- Um cliente nao consegue esgotar a cota de todos os demais clientes.
- Erros conhecidos chegam ao bot com `messageCode` estavel.
- Pollings concorrentes e reinicios nao duplicam publicacoes.
- Confirmacao e cancelamento repetidos ou concorrentes nao retornam erro interno.
- Entradas administrativas invalidas retornam erro localizado de validacao.
- Clientes conectados recebem mudancas de publicacao e administracao em tempo real.
- Usuarios comuns nao recebem auditoria ou IDs operacionais.
- Testes exercitam os fluxos reais e todas as suites e builds aprovam.
- Catalogos frontend, backend e bot permanecem sincronizados nos idiomas suportados.
