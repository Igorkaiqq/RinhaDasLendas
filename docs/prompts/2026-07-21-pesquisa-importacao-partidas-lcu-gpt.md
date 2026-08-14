# Prompt de pesquisa — importação de partidas do League Client sem Riot API key

Copie o conteúdo abaixo para um GPT com acesso à web e capacidade de citar fontes.

---

Você é um pesquisador técnico sênior especializado em League of Legends, integrações locais no Windows, segurança de aplicações e arquitetura de software. Preciso de uma investigação profunda, atualizada e verificável para orientar uma feature do projeto **RinhaDasLendas**.

## Contexto do produto

O RinhaDasLendas é uma plataforma interna para organizar jogadores, times, drafts, partidas e estatísticas de League of Legends.

Stack atual:

- Backend: .NET 10, ASP.NET Core Web API, Entity Framework Core, PostgreSQL, MediatR, FluentValidation.
- Frontend: Vue 3, TypeScript e Composition API.
- Arquitetura: API, Application, Domain, Infrastructure e Tests; DDD, CQRS, Repository e Dependency Injection.
- Autenticação e autorização já existem.
- As rotas `/partidas` e `/estatisticas` existem, mas ainda são placeholders.
- O sistema precisa continuar funcionando manualmente se qualquer integração externa falhar.
- Não haverá Riot Developer API key, Match-v5, RSO ou Tournament API no MVP.

Foi validado experimentalmente que, com o League Client aberto, é possível ler o `lockfile` local e consultar um endpoint semelhante a:

```text
GET https://127.0.0.1:{porta}/lol-match-history/v1/games/{gameId}
```

Essa resposta forneceu participantes, times, campeões, KDA, ouro, farm, dano, visão, objetivos, itens, runas e bans. O endpoint é local, não oficialmente suportado e pode mudar.

## Problema a resolver

Queremos permitir que um organizador exporte os dados de uma partida no próprio computador e os envie com segurança ao RinhaDasLendas, sem enviar logs brutos, senha do `lockfile`, tokens, URLs assinadas ou outros segredos.

O MVP imaginado é:

1. Um coletor/exportador nativo para Windows lê o League Client local.
2. O usuário informa ou escolhe o Game ID.
3. O coletor produz um arquivo versionado `*.rinha-match.json`, contendo apenas dados permitidos.
4. Um usuário autenticado envia esse arquivo na página `/partidas`.
5. O backend valida, mostra uma prévia, permite associar participantes aos jogadores cadastrados e confirma a importação.
6. O sistema persiste a partida de forma relacional e calcula estatísticas agregadas.
7. Entrada manual permanece disponível.

Como evolução, o coletor poderá enviar diretamente ao backend usando um código temporário de uso único e, mais tarde, detectar automaticamente o fim de uma partida.

## Objetivo da pesquisa

Investigue a viabilidade, riscos, alternativas e melhores práticas desse fluxo. Não implemente código de produção e não invente garantias para endpoints não documentados.

## Perguntas obrigatórias

### 1. Fontes de dados locais

- Quais fontes locais existem no League Client para obter dados pós-partida?
- Compare LCU, End of Game, Match History, Gameflow, Live Client Data API, logs e arquivos `.rofl`.
- Quais endpoints locais são conhecidos atualmente para:
  - descobrir o jogador atual;
  - listar partidas recentes;
  - obter o Game ID;
  - obter o resumo completo de uma partida;
  - detectar transição para fim de jogo;
  - traduzir IDs de campeões, itens, runas e feitiços?
- Diferencie claramente League Client API, Game Client API e Live Client Data API.
- Indique quais endpoints são documentados, não documentados, suportados ou não suportados.
- Explique o formato do `lockfile`, ciclo de vida, autenticação Basic e certificado HTTPS local.
- Verifique se há mudança recente que exija certificado de cliente, configuração TLS específica ou tratamento especial no Windows/Schannel.

### 2. Logs e replay

- É possível reconstruir KDA, dano, ouro, visão, itens, runas, objetivos e bans somente por logs?
- Quais dados normalmente aparecem em `r3dlog`, `LeagueClient.log`, tracing e GameLogs?
- Quais segredos e dados pessoais podem aparecer nesses arquivos?
- O `.rofl` é adequado como fonte de estatísticas finais? Quais limitações de versão, criptografia, parsing e compatibilidade existem?
- Conclua se upload de log ou `.rofl` deve ou não fazer parte do MVP.

### 3. Robustez do coletor local

- Como descobrir instalações do LoL sem assumir apenas `C:\Riot Games`?
- Como lidar com cliente fechado, múltiplos processos, lockfile ausente, arquivo em troca, porta expirada e partida ainda não indexada?
- Como limitar o bypass de certificado estritamente a `127.0.0.1`/`localhost`?
- Como evitar logging de senha, Authorization header, JWTs, URLs assinadas ou payloads sensíveis?
- Como implementar retry, timeout, cancelamento e backoff sem martelar o cliente?
- Como detectar mudanças de schema entre patches?
- Quais campos devem ser permitidos, descartados ou anonimizados antes de exportar?
- Como distribuir e atualizar um executável .NET para Windows com o menor atrito possível?

### 4. Contrato de exportação

Proponha um contrato `rinha-match.json` versionado e independente do formato bruto do LCU.

Inclua:

- `schemaVersion`;
- identificador externo e região/plataforma;
- data, duração, patch, fila, mapa e modo;
- dois times, resultado e objetivos;
- participantes, Riot ID em snapshot, campeão, rota, spells e estatísticas;
- itens por slot;
- runas por slot;
- bans e ordem;
- origem e data de coleta;
- checksum ou hash de conteúdo;
- campo de capabilities/presença de dados opcionais;
- estratégia de compatibilidade para versões futuras.

Defina limites de tamanho, tipos numéricos, campos obrigatórios/opcionais e regras de validação. Explique por que não devemos simplesmente armazenar o JSON bruto como modelo de domínio.

### 5. Segurança e threat model

Produza um threat model cobrindo:

- arquivo adulterado;
- path traversal e nome de arquivo malicioso;
- JSON bomb, profundidade e tamanho excessivos;
- valores negativos, overflow e IDs fora do intervalo;
- replay de importação e duplicação concorrente;
- código temporário roubado, expirado ou reutilizado;
- coletor apontado para host que não seja loopback;
- exfiltração do `lockfile`;
- SSRF no backend;
- usuário sem permissão;
- exposição pública de custom matches;
- participantes desconhecidos, renomeados ou vinculados incorretamente.

### 6. Privacidade e políticas da Riot

- Consulte somente fontes oficiais da Riot para políticas e limitações.
- Verifique as regras atuais sobre League Client API, registro do produto e histórico de custom matches.
- Explique requisitos de consentimento/opt-in, especialmente para partidas personalizadas.
- Diferencie viabilidade técnica de conformidade com políticas.
- Proponha controles adequados para uma plataforma interna e autenticada.

### 7. Arquitetura no RinhaDasLendas

Compare estas opções:

1. exportar arquivo e fazer upload;
2. envio direto com código temporário;
3. serviço residente que envia automaticamente;
4. upload de log;
5. parsing de `.rofl`;
6. entrada totalmente manual.

Para cada uma, avalie segurança, confiabilidade, manutenção, experiência do usuário, dependência de endpoints instáveis e esforço.

Recomende uma sequência incremental que preserve fallback manual e não introduza microsserviços desnecessários.

### 8. Persistência e agregações

- Proponha modelagem relacional para partida, times, participantes, bans, itens, runas, importações e vínculos com jogadores/drafts.
- Defina o que deve ser snapshot histórico e o que pode referenciar cadastro atual.
- Explique idempotência por Match ID e detecção de payload divergente.
- Compare calcular estatísticas agregadas sob demanda, atualizar projeções e usar materialized views.
- Considere renome de Riot ID e participante ainda não cadastrado.

### 9. Testes e observabilidade

- Proponha fixtures sanitizadas e golden files de diferentes patches.
- Liste testes unitários, integração, contrato, segurança, concorrência e end-to-end.
- Explique como testar sem League Client instalado no CI.
- Defina logs estruturados e métricas sem dados pessoais ou segredos.
- Proponha indicadores para importações iniciadas, validadas, rejeitadas, duplicadas e parcialmente vinculadas.

## Formato obrigatório da resposta

1. Resumo executivo.
2. Matriz comparativa das alternativas.
3. Descobertas confirmadas, com fonte e data.
4. Descobertas não confirmadas/experimentais.
5. Endpoints e comportamento conhecido, com nível de confiança.
6. Contrato recomendado para `rinha-match.json`.
7. Threat model e controles.
8. Arquitetura recomendada por fases.
9. Modelo de dados sugerido.
10. Estratégia de testes.
11. Riscos e plano de mitigação.
12. Perguntas que precisam de protótipo/spike.
13. Backlog de features adicionais, priorizado por valor versus esforço.
14. Referências com links diretos.

## Regras de qualidade

- Pesquise na web; não responda apenas por memória.
- Para Riot e políticas, use fontes oficiais como fonte primária.
- Para endpoints LCU não documentados, identifique explicitamente quando a fonte for comunitária ou experimental.
- Cite cada afirmação instável perto do texto correspondente.
- Informe datas e versões consultadas.
- Não reproduza tokens, senhas, JWTs ou URLs assinadas reais.
- Não recomende enviar logs brutos.
- Não suponha que endpoints não suportados permanecerão estáveis.
- Não proponha depender de uma Riot Developer API key no MVP.
- Separe fato, inferência e recomendação.

Ao final, produza uma lista intitulada **“Alterações sugeridas para a especificação da feature 018”**, pronta para ser aplicada ao documento de requisitos.

