# Data Model: Arquivamento Administrativo de Drafts

## `DraftMontagem`

Novos campos persistidos:

| Campo | Tipo | Nulável | Regra |
|-------|------|---------|-------|
| `ArquivadoEm` | instante UTC | Sim | Nulo quando visível; preenchido quando arquivado |
| `ArquivadoPorUsuarioId` | UUID | Sim | Usuário autenticado que confirmou o arquivamento |
| `MotivoArquivamento` | texto até 500 | Sim | Trim aplicado; obrigatório quando arquivado |

Campo derivado:

```text
Arquivado = ArquivadoEm possui valor
```

Invariantes:

- os três campos são todos nulos ou todos preenchidos;
- motivo preenchido possui de 1 a 500 caracteres após trim;
- `ArquivadoPorUsuarioId` referencia um usuário existente com exclusão restritiva;
- restauração limpa os três campos atuais;
- relações e coleções existentes nunca são removidas pelo arquivamento.

## Transições

| Estado inicial | Ação | Estado operacional final | Estado de arquivo | Efeitos adicionais |
|----------------|------|--------------------------|-------------------|--------------------|
| `PresencaAberta` | Arquivar | `Cancelada` | Arquivado | Limpa operação, duas ações administrativas, publicação `Cancelamento/Pendente` |
| `PresencaEncerrada` | Arquivar | `Cancelada` | Arquivado | Mesmo comportamento ativo |
| `CapitaesDefinidos` | Arquivar | `Cancelada` | Arquivado | Mesmo comportamento ativo |
| `OrdemDefinida` | Arquivar | `Cancelada` | Arquivado | Mesmo comportamento ativo |
| `Aberta` | Arquivar | `Cancelada` | Arquivado | Mesmo comportamento ativo |
| `Finalizada` | Arquivar | `Finalizada` | Arquivado | Uma ação de arquivamento; sem cancelamento Discord |
| `Cancelada` | Arquivar | `Cancelada` | Arquivado | Uma ação de arquivamento; sem novo cancelamento Discord |
| Qualquer arquivado | Restaurar | Status preservado | Visível | Uma ação de restauração; não retoma operação |

Repetir a transição para o estado já atingido não cria evento nem altera `VersaoEstado`.

## Ações administrativas

Tipos envolvidos:

- `CancelamentoPorArquivamento`: subtipo administrativo distinto criado quando um draft ativo é arquivado, permitindo ocultação na projeção de Moderador;
- `Arquivamento`: criado em toda mudança real para arquivado;
- `Restauracao`: criado em toda mudança real para visível.

Cada ação mantém ID, responsável, motivo quando aplicável e instante. A restauração não remove eventos anteriores. A projeção de Moderador exclui ações pertencentes ao histórico de arquivamento; a projeção Admin+ as inclui.

## Publicação Discord

Novo tipo:

```text
Cancelamento
```

Estados reutilizados: `Pendente`, `EmAndamento`, `Publicada`, `Falha`, `RequerReconciliacao` e `Ignorada`.

Regras:

- somente arquivamento de estado não terminal cria `Cancelamento/Pendente`;
- a intenção é persistida na mesma transação do arquivamento;
- bot pode operar um arquivado somente para esse tipo;
- publicações anteriores permanecem como histórico, mas não podem ser reivindicadas ou concluídas após arquivamento;
- falha não desarquiva o draft; republicação autorizada retorna o cancelamento a `Pendente`.
- claim operacional já adquirido é revalidado antes do envio e não pode ser concluído após arquivamento; uma mensagem externa que já estava em voo pode existir e é compensada pela publicação posterior de cancelamento.

## Concorrência

`VersaoEstado` continua sendo o token otimista e passa a integrar os contratos de resumo, detalhe, arquivamento e restauração.

| Cenário | Resultado |
|---------|-----------|
| Arquivar + arquivar na mesma versão | Primeiro persiste; segundo retorna o estado atual sem evento e sem substituir motivo/autor |
| Restaurar + restaurar na mesma versão | Primeiro persiste; segundo retorna o estado atual sem evento |
| Arquivar + restaurar concorrentes | Primeiro persiste; perdedor recebe conflito |
| Arquivar + ação operacional | Um persiste; perdedor recebe conflito ou recurso indisponível após recarga |
| Repetição após timeout | Estado atual retornado sem novo evento |

## Persistência

Colunas `snake_case` em `draft_montagens`:

```text
arquivado_em
arquivado_por_usuario_id
motivo_arquivamento
```

Constraint exige os três campos simultaneamente nulos ou válidos. Índices:

- listagem operacional parcial por status/data onde `arquivado_em IS NULL`;
- listagem administrativa por `arquivado_em DESC` onde não nulo;
- FK por `arquivado_por_usuario_id`.

Nenhuma FK existente muda para cascade e nenhuma linha histórica recebe backfill como arquivada.

## Projeções

- Resumo normal: `id`, dados existentes, `arquivado`, `versaoEstado`.
- Detalhe normal: dados existentes, `arquivado`, `versaoEstado`; arquivados não são carregados nesse endpoint.
- Projeção Admin+ de arquivamento: detalhe completo, três metadados atuais e ações administrativas de arquivo.
- Projeção Discord: adiciona `arquivado`; em arquivado, candidato válido é somente `Cancelamento`.
- Evento realtime de arquivamento: somente `draftMontagemId`.
