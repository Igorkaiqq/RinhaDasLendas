# Prompt para OpenCode — Codex 5.6 + Superpowers + Spec Kit

Copie o conteúdo abaixo no OpenCode a partir da raiz do repositório.

---

Trabalhe no repositório:

```text
C:\Users\Igor Kaique\Documents\Programacao\git\RinhaDasLendas
```

Você está atuando como agente principal de engenharia com Codex 5.6 e Superpowers. A feature é **018 — Importação de partidas do League Client sem Riot Developer API key**.

## Regras inegociáveis

1. Leia `AGENTS.md` integralmente antes de qualquer ação.
2. Leia `.specify/memory/constitution.md` e os documentos em `docs/architecture/`.
3. Respeite o fluxo SDD obrigatório:

   ```text
   Constitution → Specify → Clarify → Plan → Tasks → Analyze → Implement
   ```

4. Não implemente antes de `spec.md`, `plan.md` e `tasks.md` estarem completos, analisados e explicitamente aprovados pelo usuário.
5. Não misture a feature com 016 ou 017.
6. Verifique `git status`, branch atual e histórico. Não descarte alterações existentes.
7. Use a branch `feature/018-importacao-partidas-lcu`, criada a partir da base correta confirmada com o usuário; não empilhe a feature sobre uma branch não integrada sem autorização.
8. Commits devem ser intencionais e em português brasileiro, um por fase aprovada.
9. Não faça push, PR, publicação ou deploy sem pedido explícito.
10. Não versione `.superpowers/`, logs, payloads reais, tokens ou arquivos gerados pelo cliente.

## Uso obrigatório de Superpowers

- Comece com `superpowers:brainstorming` para revisar o problema, alternativas e decisões ainda abertas.
- Use o Spec Kit como fonte de verdade para `spec.md`, `plan.md`, contratos e `tasks.md`.
- Depois da aprovação do plano/tarefas, use `superpowers:writing-plans` somente para tornar a execução verificável, sem contradizer o Spec Kit.
- Na implementação, prefira `superpowers:subagent-driven-development` quando houver tarefas independentes; caso contrário, use `superpowers:executing-plans`.
- Use `superpowers:test-driven-development` para regras de domínio, validação do contrato, idempotência, segurança e mapeamento.
- Use `superpowers:systematic-debugging` para qualquer falha não trivial.
- Antes de declarar conclusão, use `superpowers:verification-before-completion` e solicite code review com o mecanismo Superpowers disponível.
- Não alegue que uma skill foi usada se ela não estiver disponível; nesse caso, informe a ausência e aplique manualmente o mesmo gate.

## Contexto da feature

O RinhaDasLendas precisa importar dados completos de partidas de League of Legends sem Riot Developer API key.

O fluxo alvo do MVP é:

```text
League Client aberto
        ↓
Coletor/exportador .NET no Windows
        ↓
arquivo *.rinha-match.json sanitizado e versionado
        ↓ upload autenticado
Backend RinhaDasLendas
        ↓ validação + prévia + confirmação
PostgreSQL
        ↓
/partidas e /estatisticas
```

Foi comprovado experimentalmente que, com o cliente aberto, o `lockfile` fornece porta e credencial temporária para consultar localmente um endpoint semelhante a:

```text
GET https://127.0.0.1:{porta}/lol-match-history/v1/games/{gameId}
```

A resposta contém times, participantes, campeões, KDA, ouro, farm, dano, visão, objetivos, itens, runas e bans. A API LCU é local, não oficialmente suportada e pode mudar.

## Decisões de produto já tomadas

- Não usar Riot Developer API key, Match-v5, RSO ou Tournament API no MVP.
- Não fazer upload de logs brutos.
- Não depender de parsing de `.rofl`.
- Nunca exportar ou transmitir senha do `lockfile`, Authorization header, JWT, token de chat, URL assinada ou endereço da partida.
- O coletor deve usar allowlist de campos e contrato próprio; não encaminhar o payload bruto do LCU ao backend.
- O MVP deve gerar um arquivo `*.rinha-match.json` para upload manual autenticado.
- Entrada manual de partida e estatísticas deve continuar funcionando.
- Participantes desconhecidos devem ser importados como snapshots e associados posteriormente a jogadores cadastrados.
- `riot_match_id` deve garantir idempotência; payload divergente para o mesmo ID deve gerar conflito explícito.
- Persistência de negócio deve ser relacional. JSON bruto, se retido para auditoria, não pode ser a única fonte de verdade.
- Partidas personalizadas não devem ser expostas publicamente sem consentimento aplicável.
- Envio direto por código temporário de uso único é incremento posterior.
- Detecção automática de fim de jogo é incremento posterior e nunca deve remover o fluxo manual.

## Artefatos de entrada

Leia e revise:

```text
specs/018-importacao-partidas-lcu/spec.md
docs/prompts/2026-07-21-pesquisa-importacao-partidas-lcu-gpt.md
```

Se houver relatório externo produzido pelo prompt de pesquisa, trate-o como insumo, valide as fontes e registre decisões em `research.md`; não copie conclusões cegamente.

## Fase inicial desta execução

1. Faça inventário do estado atual do repositório, sem modificar código.
2. Confirme a base Git adequada para a branch 018.
3. Execute brainstorming sobre:
   - arquivo versus envio direto;
   - contrato versionado;
   - segurança e privacidade;
   - vínculo com jogadores e drafts;
   - persistência relacional;
   - compatibilidade entre patches;
   - testes sem League Client no CI.
4. Revise `spec.md` e marque ambiguidades reais; não invente requisitos técnicos na especificação de produto.
5. Execute o equivalente a `/speckit-clarify`.
6. Pare e apresente ao usuário:
   - decisões propostas;
   - perguntas bloqueantes;
   - riscos;
   - alterações sugeridas para a spec.

Não avance para plano sem aprovação.

## Diretrizes para o plano futuro

Quando autorizado a executar `/speckit-plan`, avalie uma estrutura semelhante, sem tratá-la como obrigatória antes da pesquisa:

```text
BackEnd/src/RinhaDasLendas.LcuCollector/
BackEnd/src/RinhaDasLendas.Domain/Entities/Partida*.cs
BackEnd/src/RinhaDasLendas.Application/Commands/Partidas/
BackEnd/src/RinhaDasLendas.Application/Queries/Partidas/
BackEnd/src/RinhaDasLendas.Infrastructure/Riot/
BackEnd/src/RinhaDasLendas.Infrastructure/Repositories/PartidaRepository.cs
BackEnd/src/RinhaDasLendas.Api/Controllers/PartidasController.cs
FrontEnd/src/views/MatchesView.vue
FrontEnd/src/views/MatchDetailsView.vue
FrontEnd/src/views/StatsView.vue
FrontEnd/src/components/matches/
FrontEnd/src/services/matches.ts
FrontEnd/src/types/matches.ts
```

O coletor é um executável local necessário porque o LCU existe apenas no computador do usuário; ele não deve se transformar em microsserviço de backend.

## Requisitos técnicos a validar no planejamento

- .NET 10 para o coletor e backend.
- `HttpClient` com Basic Auth em memória.
- Bypass de certificado restrito a loopback e somente no cliente LCU.
- Descoberta segura do `lockfile` e instalações alternativas.
- Timeouts, cancelamento, retry limitado e mensagens localizadas.
- DTO de exportação com `schemaVersion` e allowlist.
- Limites de upload, profundidade JSON e valores numéricos.
- Hash canônico para detectar payload divergente.
- Idempotência e concorrência no banco.
- Preview antes da confirmação.
- Associação automática por Riot ID normalizado e resolução manual de ambiguidades.
- Snapshot histórico do Riot ID, campeão, build e estatísticas.
- Agregações de jogador consistentes e reconstruíveis.
- API REST versionada, Swagger e erros padronizados.
- Autorização específica para importar, confirmar e corrigir vínculos.
- Logs estruturados sem PII desnecessária ou segredos.
- Fixtures totalmente sintéticas/sanitizadas; nunca usar os logs reais do usuário no Git.
- Testes xUnit, FluentAssertions e Moq no backend/coletor.
- Testes Vue/TypeScript conforme padrões existentes.
- `pt.json` e `en.json` sincronizados; backend com recursos `.resx` PT/EN.

## Critérios de rejeição

Rejeite qualquer proposta que:

- envie o `lockfile` ou seu conteúdo ao backend;
- faça upload automático de diretórios de logs;
- use segredo fixo embutido no coletor;
- desabilite validação TLS globalmente;
- permita ao backend buscar URL indicada pelo arquivo importado;
- confie em nome de arquivo, Riot ID ou estatística sem validação;
- armazene apenas JSON bruto para regras de negócio;
- sobrescreva silenciosamente uma partida já confirmada;
- exponha custom matches de forma pública sem controle de consentimento;
- dependa exclusivamente do LCU e remova a entrada manual;
- implemente overlay ou vantagem durante a partida;
- introduza microsserviços ou infraestrutura desnecessária.

## Gates de conclusão da implementação futura

Somente considere a feature concluída quando:

1. Todos os requisitos e cenários aprovados estiverem rastreados para testes.
2. Builds e testes do backend, frontend e coletor passarem.
3. Upload inválido, duplicado, adulterado, superdimensionado e concorrente estiver coberto.
4. Nenhum segredo aparecer em código, logs, fixtures ou documentação.
5. O fluxo manual continuar funcionando sem cliente LoL.
6. O frontend funcionar por teclado e seguir o design system.
7. A auditoria de internacionalização exigida por `AGENTS.md` estiver integralmente `Sim`.
8. `git status`, diff e histórico forem revisados; `.superpowers/` permanecer ignorado.
9. Um relatório final listar arquivos alterados, comandos executados, testes e riscos residuais.

Comece agora apenas pela leitura, brainstorming e revisão/clarificação da especificação. Não implemente.

