# Frontend UI Contract: Central de Agendamentos

## Access Boundary

- Route: `/configuracoes`.
- `PresenceScheduleSection` aparece quando `auth.hasPermission(Permissions.CanManageDrafts)`.
- `DiscordAdminConfigurationSection` permanece independente e aparece somente quando `auth.hasPermission(Permissions.CanManageUsers)`.
- Jogador comum não vê a central; Moderador vê agendas sem configuração sensível; Admin e SuperAdmin veem ambas as seções.
- O backend continua sendo a fonte de verdade de autorização, recorrência e deduplicação.

## Types And Service Contract

```ts
export type PresenceScheduleStatus = 'Ativo' | 'Pausado'
export type PresenceScheduleOccurrenceStatus = 'Processando' | 'Bloqueada' | 'Criada' | 'Perdida' | 'Falha'
export type IsoWeekday = 'Segunda' | 'Terca' | 'Quarta' | 'Quinta' | 'Sexta' | 'Sabado' | 'Domingo'

export interface SavePresenceScheduleRequest {
  nome: string
  observacao: string | null
  diasSemana: IsoWeekday[]
  horarioPublicacao: string
  horarioEncerramento: string
}

export function listPresenceSchedules(): Promise<PresenceScheduleSummary[]>
export function createPresenceSchedule(payload: SavePresenceScheduleRequest): Promise<PresenceScheduleSummary>
export function updatePresenceSchedule(id: string, payload: SavePresenceScheduleRequest): Promise<PresenceScheduleSummary>
export function pausePresenceSchedule(id: string): Promise<PresenceScheduleSummary>
export function reactivatePresenceSchedule(id: string): Promise<PresenceScheduleSummary>
export function archivePresenceSchedule(id: string): Promise<void>
```

- Horários são serializados como `HH:mm`.
- Erros preservam `messageCode`; `403` e `500` nunca são convertidos silenciosamente em lista vazia.
- Status arquivado não integra `PresenceScheduleStatus` porque agendas arquivadas deixam a coleção normal.

## PresenceScheduleSection

### Content

- Eyebrow localizado equivalente a `Automações`.
- Título localizado equivalente a `Listas de presença` e descrição curta.
- Ação principal `Novo agendamento`.
- Resumo de agendas ativas, próxima execução e fuso Brasília.
- Cards não tabulares ordenados por próxima execução.
- Cada card mostra nome, observação quando presente, dias, intervalo, status, próxima execução e resultado recente.
- Ações condicionais: editar e pausar em ativa; editar e reativar em pausada; excluir em ambas.
- Estado vazio localizado com CTA para primeira agenda.
- Loading usa padrão Skeleton existente; erro oferece mensagem localizada e tentativa novamente.

### Occurrence Status Presentation

| Status | UI meaning |
|--------|------------|
| `Processando` | Execução em andamento, sem prometer publicação concluída |
| `Bloqueada` | Aguardando disponibilidade/configuração antes do encerramento |
| `Criada` | Draft criado e entregue ao fluxo de publicação |
| `Perdida` | Janela encerrada sem criação de draft |
| `Falha` | Erro terminal conhecido, exibido por `messageCode` localizado |

Cor nunca é o único indicador; badge inclui texto localizado.

## PresenceScheduleFormDialog

### Fields

1. Nome obrigatório, 3-100 caracteres.
2. Observação opcional, até 500 caracteres.
3. Chips de `Segunda` a `Domingo`, ao menos um selecionado, com `aria-pressed`.
4. Horário de publicação `HH:mm`.
5. Horário de encerramento `HH:mm`, estritamente posterior.
6. Resumo localizado equivalente a `Horário de Brasília · Times de 5 · Repete até ser pausado`.

### Behavior

- Mesmo formulário para criação e edição, com título e ação localizados pelo modo.
- Validação local evita submissões obviamente inválidas e não substitui validação backend.
- Mensagens inline vêm de `settings.presenceSchedules`; resposta backend usa `messageCode` localizado.
- Envio desabilita controles e impede duplicação enquanto estiver em andamento.
- Fechamento por botão, `Escape` e cancelar; foco fica preso e retorna ao gatilho.
- Sucesso atualiza a coleção e apresenta feedback localizado conforme padrão existente.

## PresenceScheduleConfirmDialog

- Pausa explica que futuras ocorrências deixam de ser criadas e drafts existentes permanecem.
- Exclusão explica arquivamento e preservação dos drafts já criados.
- Confirmação destrutiva usa variante danger e exige ação explícita.
- Cancelar e fechar restauram foco; submissão em andamento impede repetição.

## Responsive Contract

- Desktop: resumo e cards com ações alinhadas, sem transformar a central em dashboard administrativo genérico.
- Tablet: menos colunas de resumo e cards adaptáveis.
- Mobile: uma coluna; chips quebráveis ou roláveis; ações com largura confortável.
- 320px: sem overflow horizontal da página ou diálogo; alvos de toque respeitam tokens de controle.
- `prefers-reduced-motion` é respeitado; todo foco interativo é visível.

## Internationalization Contract

- Toda string visível e nome acessível usa a raiz `settings.presenceSchedules` em `pt.json` e `en.json` com estrutura equivalente.
- Cobertura obrigatória: títulos, descrições, campos, placeholders, dias, status, badges, ações, confirmações, erros, loading, vazio, fuso, resumo, toasts e acessibilidade.
- Datas e dias são formatados pelo locale ativo; nome e observação inseridos pelo usuário não são traduzidos.
- Português usa acentuação correta e os testes de paridade falham para chave ausente em qualquer idioma.

## Required UI Tests

- Visibilidade Jogador/Moderador/Admin e separação de `CanManageDrafts`/`CanManageUsers`.
- Serviço: verbo, URL, payload `HH:mm`, retorno, `messageCode` e propagação de erro.
- Listagem, ordenação, loading, erro, estado vazio, próxima execução e todos os status.
- Criar, editar, pausar, reativar e arquivar com confirmações.
- Nome, observação, dias e janela inválidos; submissão única.
- Teclado, foco preso/restaurado, `Escape`, `aria-pressed`, nomes acessíveis e toque.
- Paridade PT/EN, acentuação e ausência de texto visível hardcoded.
- Desktop, tablet, mobile e largura de 320px sem overflow.
