# Relatório da Task 6

## Status

- T085 e T086 concluídas.
- Persistência, migração, repositório, endpoints e contratos do bot permanecem fora deste incremento para a Task 7.
- Commit planejado: `feat: modelar claims de publicação Discord`.

## TDD

### RED

- A suíte focada falhou inicialmente porque `MV080`, `MV081`, estados, campos e métodos de claim ainda não existiam.
- Uma segunda etapa RED provou que `DataAtualizacao` ainda usava o relógio implícito do agregado, divergindo do `agora` informado.

### GREEN

- A suíte `DraftMontagemTests` passou com 25 testes após a implementação.
- Os testes de recursos passaram com 8 testes.
- A suíte backend completa passou com 116 testes no container com acesso ao Docker para Testcontainers.
- O build Release concluiu com zero erros.

## Transições

- `Pendente -> EmAndamento`: concede `ClaimId`, define `ClaimExpiraEm` e registra `UltimaTentativaEm` com relógio explícito.
- `EmAndamento -> Publicada`: exige o claim ativo, registra mensagem e publicação, e limpa a expiração.
- `EmAndamento -> Falha`: exige o claim ativo, registra o erro e limpa a expiração.
- `EmAndamento -> RequerReconciliacao`: ocorre quando a tentativa está expirada, preserva o claim para auditoria e limpa a expiração.
- `Falha -> Pendente`: republicação administrativa limpa claim e expiração e recebe relógio explícito.
- Segundo claim e claim após reconciliação são rejeitados com `MV080`.
- Conclusão ou falha com claim divergente, ausente ou pelo contrato anterior são rejeitadas com `MV081` sem mutação.

## Testes

- Claim único e preservação da tentativa ativa.
- Conclusão e falha somente pelo claim ativo.
- Claim divergente sem alteração de estado ou metadados.
- Expiração para reconciliação, preservando o claim.
- Bloqueio de novo claim em reconciliação.
- Bloqueio dos contratos anteriores sem claim.
- Relógio explícito na publicação e no agregado.
- Mensagens localizadas de `MV080` e `MV081` em português e inglês.

## Internacionalização

- Textos hardcoded no frontend: não foram introduzidos; frontend não foi alterado.
- Mensagens hardcoded no backend: não foram introduzidas.
- `Messages.resx`, `Messages.pt-BR.resx` e `Messages.en-US.resx`: sincronizados.
- Recursos backend: atualizados com `MV080` e `MV081`.
- Acentuação em português: revisada.
- Placeholders, botões, títulos, badges, toasts e estados vazios: não aplicáveis a este incremento de domínio.
- Validações frontend/backend com i18n/recurso: os novos erros de domínio usam `MessageCodes` e recursos; frontend não foi alterado.
- Novos arquivos: este relatório respeita o padrão; nenhum texto de interface foi criado.

## Observações

- Os campos de claim estão explicitamente ignorados no mapeamento EF durante esta task para não antecipar a persistência da Task 7.
- Os handlers antigos de conclusão/falha continuam compilando, mas recebem `MV081` até serem migrados para o contrato com claim na Task 7; não existe bypass público de conclusão sem claim.
- O restore/build mantém o aviso conhecido `NU1903` para `Microsoft.OpenApi` 2.4.1.
