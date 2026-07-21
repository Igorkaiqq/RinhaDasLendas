# Relatório da Task 6

## Status

- T085 e T086 concluídas.
- Persistência, migração, repositório, endpoints e contratos do bot permanecem fora deste incremento para a Task 7.
- Implementação original: `9f6a708 feat: modelar claims de publicação Discord`.
- Correções da revisão planejadas em commit separado: `fix: corrigir transições de publicação Discord`.

## TDD

### RED

- A suíte focada falhou inicialmente porque `MV080`, `MV081`, estados, campos e métodos de claim ainda não existiam.
- Uma segunda etapa RED provou que `DataAtualizacao` ainda usava o relógio implícito do agregado, divergindo do `agora` informado.
- A revisão abriu novo RED porque os endpoints legados sempre recebiam `MV081`, claims expirados ainda podiam concluir/falhar, republicação limpava claims ativos e os mutadores da publicação eram públicos.
- Um RED complementar comprovou mutação parcial dos metadados legados e relógio implícito ao criar a primeira solicitação de republicação.
- A verificação completa reproduziu uma suposição não determinística preexistente: um jogador recém-criado precisava aparecer na primeira página de uma lista sem ordenação garantida e limitada a 100 itens; a persistência e o GET por ID já validam o registro criado.

### GREEN

- A suíte `DraftMontagemTests` passou com 40 testes.
- Os testes de recursos passaram com 18 testes.
- A suíte backend completa passou com 141 testes no container com acesso ao Docker para Testcontainers.
- O build Release concluiu com zero erros.
- A cobertura de endpoints mantém a validação da listagem sem depender da posição do jogador criado em uma coleção paginada.

## Transições

- `Pendente -> EmAndamento`: concede `ClaimId`, define `ClaimExpiraEm` e registra `UltimaTentativaEm` com relógio explícito.
- `EmAndamento -> Publicada`: exige claim não vazio, claim correspondente e `agora < ClaimExpiraEm`; registra mensagem/publicação e limpa a expiração.
- `EmAndamento -> Falha`: exige claim não vazio, claim correspondente e `agora < ClaimExpiraEm`; registra o erro e limpa a expiração.
- `EmAndamento -> RequerReconciliacao`: ocorre quando a tentativa está expirada, preserva o claim para auditoria e limpa a expiração.
- `Falha -> Pendente`: republicação administrativa limpa claim e expiração e recebe relógio explícito.
- `RequerReconciliacao -> Pendente`: republicação administrativa limpa o claim reconciliado.
- `Publicada -> Pendente`: somente após confirmação administrativa explícita de ausência da publicação.
- `Pendente -> Pendente`: idempotente, sem nova auditoria ou alteração da versão.
- Republicação em `EmAndamento` é rejeitada sem limpar o claim ativo.
- Segundo claim e claim após reconciliação são rejeitados com `MV080`.
- Conclusão ou falha com claim divergente são rejeitadas com `MV081`; claim expirado é rejeitado com `MV082` no instante da expiração ou depois.
- Claim vazio e expiração não futura são rejeitados com `MV083` e `MV084` antes da criação da publicação.
- O caminho legado continua publicando/falhando sem claim enquanto não existe tentativa `EmAndamento`; tentativa ativa é protegida por `MV085`.

## Testes

- Claim único e preservação da tentativa ativa.
- Conclusão e falha somente pelo claim ativo.
- Rejeição temporal de conclusão/falha no instante da expiração e depois.
- Rejeição de `Guid.Empty` e expiração menor ou igual ao relógio atual.
- Claim divergente sem alteração de estado ou metadados.
- Expiração para reconciliação, preservando o claim.
- Bloqueio de novo claim em reconciliação.
- Sucesso dos contratos legados de publicação/falha e proteção contra sobrescrever claim ativo.
- Republicação permitida para `Falha`, `RequerReconciliacao` e `Publicada` confirmada; `Pendente` idempotente e `EmAndamento` bloqueado.
- Ausência de mutadores públicos em `DraftMontagemPublicacaoDiscord`.
- Relógio explícito na publicação e no agregado.
- Mensagens localizadas de `MV080` a `MV086` em português e inglês.

## Internacionalização

- Textos hardcoded no frontend: não foram introduzidos; frontend não foi alterado.
- Mensagens hardcoded no backend: não foram introduzidas.
- `Messages.resx`, `Messages.pt-BR.resx` e `Messages.en-US.resx`: sincronizados.
- Recursos backend: atualizados e testados de `MV080` a `MV086`.
- Acentuação em português: revisada.
- Placeholders, botões, títulos, badges, toasts e estados vazios: não aplicáveis a este incremento de domínio.
- Validações frontend/backend com i18n/recurso: os novos erros de domínio usam `MessageCodes` e recursos; frontend não foi alterado.
- Novos arquivos: este relatório respeita o padrão; nenhum texto de interface foi criado.

## Observações

- Os campos de claim estão explicitamente ignorados no mapeamento EF durante esta task para não antecipar a persistência da Task 7.
- Os handlers e endpoints legados continuam funcionais sem alteração de contrato. A Task 7 deve migrá-los para o protocolo com claim e então remover os overloads legados.
- O caminho legado não pode publicar/falhar sobre uma tentativa `EmAndamento`, evitando sobrescrever claim ativo durante a transição.
- Persistência de `ClaimId`/`ClaimExpiraEm`, operações atômicas e endpoints do protocolo novo continuam reservados para a Task 7.
- O restore/build mantém o aviso conhecido `NU1903` para `Microsoft.OpenApi` 2.4.1.
