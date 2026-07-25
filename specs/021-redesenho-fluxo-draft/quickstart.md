# Quickstart: Validação do Redesenho do Fluxo de Draft

## Pré-requisitos

- Node.js e dependências de `FrontEnd/` instaladas.
- Para validação autenticada, conta dedicada com perfil de jogador e conta Moderador+.
- Drafts de validação nos estados de presença, capitães, ordem, escolhas, finalizado e cancelado.
- Não registrar credenciais, tokens ou dados pessoais em comandos, screenshots ou logs.

## 1. Baseline focado

```bash
npm test --prefix FrontEnd -- \
  src/views/DraftsView.spec.ts \
  src/components/drafts/DraftStateRail.spec.ts \
  src/constants/systemUpdates.spec.ts \
  src/services/systemUpdates.spec.ts \
  src/views/SystemUpdatesView.spec.ts \
  src/components/updates/SystemUpdateCard.spec.ts \
  src/i18n/i18n.spec.ts
```

Baseline de planejamento em 2026-07-25: 58 testes aprovados nos sete arquivos focados.

## 2. Validar `2026.07.3`

Confirmar:

- release no topo com data `2026-07-25`;
- categoria `fix`, área `drafts` e link para Configurações;
- detalhe sobre confirmação visual dos dias;
- `.3` como única release destacada;
- `.2` preservada e sem destaque;
- conteúdo equivalente em português e inglês;
- nenhum anúncio antecipado do redesenho.

```bash
npm test --prefix FrontEnd -- \
  src/constants/systemUpdates.spec.ts \
  src/services/systemUpdates.spec.ts \
  src/views/SystemUpdatesView.spec.ts \
  src/components/updates/SystemUpdateCard.spec.ts \
  src/i18n/i18n.spec.ts
```

## 3. Validar componentes e jornadas

```bash
npm test --prefix FrontEnd -- \
  src/views/DraftsView.spec.ts \
  src/components/drafts/DraftNavigator.spec.ts \
  src/components/drafts/DraftWorkspaceHeader.spec.ts \
  src/components/drafts/DraftPreparationPanel.spec.ts \
  src/components/drafts/DraftDiscordPublicationPanel.spec.ts \
  src/components/drafts/DraftStateRail.spec.ts \
  src/components/drafts/visual/DraftVisualBoard.spec.ts
```

Validar presença aberta, capitães, ordem, escolhas, finalização, cancelamento, status desconhecido, permissões, falha e recuperação.

## 4. Gate frontend completo

```bash
npm test --prefix FrontEnd
npm run build --prefix FrontEnd
npm run lint:check --prefix FrontEnd
npm audit --prefix FrontEnd -- --audit-level=moderate
```

O plano inclui `lint:check` como script não destrutivo (`eslint .`); o script `lint` existente continua reservado para correção automática.

## 5. Validar no navegador

Iniciar o frontend no ambiente de desenvolvimento e abrir uma sessão autenticada dedicada:

```bash
agent-browser --session feature021 open http://localhost:5173/drafts
agent-browser --session feature021 snapshot -i
```

Repetir a jornada nos viewports:

```bash
agent-browser --session feature021 set viewport 1440 900
agent-browser --session feature021 set viewport 1024 768
agent-browser --session feature021 set viewport 768 900
agent-browser --session feature021 set viewport 320 844
```

Em cada largura, confirmar:

- ausência de overflow horizontal da página;
- somente uma região de rolagem vertical por vez;
- draft, etapa e ação principal identificáveis;
- todos os controles acessíveis por teclado e toque;
- nomes longos sem sobreposição;
- preferências de rota visíveis no pool e nos detalhes;
- cancelado sem etapa ativa;
- português e inglês sem chave técnica visível;
- movimento reduzido sem pulsos ou rolagem animada;
- console sem erros.

Checagem objetiva de overflow:

```bash
agent-browser --session feature021 eval \
  "document.documentElement.scrollWidth <= document.documentElement.clientWidth"
```

Encerrar:

```bash
agent-browser --session feature021 close
```

## 6. Publicação do redesenho

### Matriz de critérios e evidências

| Critério | Evidência obrigatória | Aprovação |
|----------|----------------------|-----------|
| SC-001 | Testes da view e cabeçalho nos sete status | Zero ou uma ação primária conforme o estado, regiões na ordem de leitura e nenhuma ação de avanço nos terminais |
| SC-002 | Screenshots e checagem de overflow em 1440, 1024, 768 e 320px | Nenhum overflow horizontal, sobreposição ou ação inacessível |
| SC-003 | Testes do painel com 0, 1, 10, 14 e 30 participantes e validação em 320px | Identidade, origem e ação disponíveis sem alteração de largura |
| SC-004 | Matriz do rail para sete status e desconhecido | Nenhum status incorreto; cancelado sem etapa ativa |
| SC-005 | Testes de confirmar, encerrar, cancelar, capitães, ordem, escolher, remover, finalizar e republicar | Permissão, duplicidade e resultado preservados em cada ação |
| SC-006 | Jornada por Tab, Shift+Tab, Enter e Espaço | Foco sempre visível, ordem lógica e etapa atual anunciada |
| SC-007 | Paridade de locale e auditoria de textos visíveis | PT/EN completos, sem chave técnica ou texto hardcoded |
| SC-008 | Testes de Atualizações e inspeção autenticada | `2026.07.3` no topo, única destacada e link válido |
| SC-009 | Execução das seis jornadas em PT e EN | Nenhuma perda funcional, informacional ou de permissão |
| SC-010 | Jornada de inclusão, remoção e avanço com lista extensa | No máximo uma região de rolagem vertical por vez |

Registrar resultados e evidências em `specs/021-redesenho-fluxo-draft/verification-report.md`.

### Auditoria de internacionalização

O relatório final deve confirmar explicitamente:

- ausência de textos visíveis hardcoded em `DraftsView.vue` e `components/drafts/**/*.vue`;
- sincronização integral entre `pt.json` e `en.json`;
- conteúdo `2026.07.3` equivalente nos dois idiomas;
- acentuação portuguesa revisada;
- placeholders, botões, títulos, badges, toasts, empty states e mensagens de validação revisados;
- validações frontend usando i18n;
- nenhum arquivo novo fora do padrão;
- backend sem mensagens novas e resources backend sem alteração necessária.

### Gate local para criar a entrada editorial

Não adicionar a release posterior enquanto algum item local estiver pendente:

- seis jornadas aceitas;
- matriz de viewports aprovada;
- teclado, foco e alvos de toque aprovados;
- permissões e atualização ao vivo sem regressão;
- PT/EN equivalentes;
- suíte, build, lint e auditoria aprovados;

A entrada pode ser criada depois desses gates locais para seguir no mesmo artefato implantado. A publicação só é considerada concluída após o deploy e a validação autenticada em produção, executados depois da integração em `main`.

A versão será o próximo `AAAA.MM.N` disponível na data real da publicação.
