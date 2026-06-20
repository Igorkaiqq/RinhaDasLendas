# Research: Montagem Visual de Draft e Times

## Decision: Criar agregado separado para montagem visual

**Rationale**: O `DraftSessao` atual representa picks alternados entre dois times (`TimeA` e `TimeB`). A montagem visual exige N times, reservas, jogadores livres e movimentacao arbitraria. Um agregado separado evita quebrar o comportamento existente e deixa os conceitos mais claros.

**Alternatives considered**:

- Generalizar `DraftSessao`: reaproveita tabelas, mas mistura fluxo sequencial de picks com board livre multi-times.
- Substituir draft atual: alto risco, pois a feature 007 ja cobre picks alternados e historico.

## Decision: Usar preferencias cadastradas como default e rota contextual por montagem

**Rationale**: Preferencias do jogador sao dados de perfil e nao devem ser sobrescritas por uma decisao pontual da noite. A rota contextual permite adaptar o draft sem perder a fonte de verdade.

**Alternatives considered**:

- Usar apenas preferencias cadastradas: rapido, mas pouco flexivel.
- Usar apenas selecao do draft: perde valor do cadastro e duplica informacao.

## Decision: Persistir layout final e estado atual, nao apenas historico de movimentos

**Rationale**: Para reabrir a tela rapidamente, a API precisa retornar estado atual de times, reservas e livres. Historico pode ser futuro.

**Alternatives considered**:

- Persistir apenas eventos de movimento: auditavel, mas mais complexo para MVP.
- Persistir apenas no frontend: perde estado no reload e viola regras criticas no backend.

## Decision: Salvar layout explicitamente no MVP

**Rationale**: Salvamento explicito reduz quantidade de requisicoes, simplifica conflitos e atende uso interno. A UI pode indicar alteracoes pendentes.

**Alternatives considered**:

- Auto-save a cada movimento: experiencia fluida, mas exige tratamento robusto de concorrencia.
- Salvar apenas ao finalizar: risco de perda de trabalho durante montagem longa.

## Decision: Drag and drop com alternativa por acoes

**Rationale**: Drag and drop e central no prototipo, mas mobile/acessibilidade precisam de caminho alternativo. Cards devem ter acoes como `Mover para`, `Enviar para reservas`, `Promover a capitao`.

**Alternatives considered**:

- Drag-only: mais simples, mas fraco em mobile.
- Apenas botoes: acessivel, mas perde experiencia do prototipo.

## Decision: Exportacao captura uma area dedicada de resultado

**Rationale**: O prototipo exporta `capture-area`. A aplicacao deve ter area limpa, sem controles, para evitar capturar filtros/botoes.

**Alternatives considered**:

- Exportar tela inteira: inclui controles e pode ficar ilegivel.
- Gerar imagem no backend: mais controlado, mas maior custo para MVP.
