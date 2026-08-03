# Fixture local para QA do draft

`RinhaDasLendas.BrowserQaFixture` prepara dados exclusivamente para QA manual da sincronização de drafts. O projeto não é referenciado pela API e não adiciona endpoint, bypass de autenticação ou configuração de produção.

## Proteções

- exige banco PostgreSQL já migrado até `20260731012844_AddDraftMontagemSystemActor`;
- aceita somente banco cujo nome comece por `rinha_feature029_browser_qa_`;
- recusa banco com usuários ou drafts existentes;
- recebe conexão, IDs, e-mails e senhas somente por variáveis de ambiente;
- exige UUIDs distintos para as duas sessões autenticadas;
- grava apenas hashes de senha e não devolve senhas nos metadados;
- cria usuários e drafts usando o modelo EF e os agregados de domínio reais.

## Dados criados

- duas contas SuperAdmin vinculadas a jogadores ativos e com cargo Capitão;
- dois titulares adicionais ativos;
- uma reserva ativa com cargo Capitão;
- um draft por presença com cinco confirmações, preparado para dois times de duas vagas e uma reserva;
- um draft manual independente com quatro participantes para layout sujo e conflito concorrente.

## Execução

Crie um banco descartável com sufixo único e aplique todas as migrations antes da fixture. Não reutilize banco de desenvolvimento, homologação ou produção.

Defina os valores efêmeros sem salvá-los no repositório:

```bash
export QA_DATABASE_CONNECTION="Host=postgres;Port=5432;Database=rinha_feature029_browser_qa_<sufixo-unico>;Username=<usuario>;Password=<senha-do-banco>"
export QA_ADMIN_A_ID="$(uuidgen)"
export QA_ADMIN_B_ID="$(uuidgen)"
export QA_ADMIN_A_EMAIL="<email-efemero-a>"
export QA_ADMIN_B_EMAIL="<email-efemero-b>"
export QA_ADMIN_A_PASSWORD="<senha-efemera-a>"
export QA_ADMIN_B_PASSWORD="<senha-efemera-b>"
```

Execute explicitamente no devcontainer:

```bash
dotnet run \
  --project /workspaces/RinhaDasLendas/.worktrees/feature-024/BackEnd/scripts/qa/RinhaDasLendas.BrowserQaFixture/RinhaDasLendas.BrowserQaFixture.csproj \
  --configuration Release
```

A saída JSON contém apenas IDs dos dois drafts, das contas autenticadas e dos jogadores. Remova o banco descartável e elimine as variáveis de ambiente ao concluir o QA.

Para acelerar somente a espera do worker real, a expiração do turno pode ser ajustada diretamente no banco descartável depois que o draft estiver `Aberta` em modo `TempoReal`. Use o ID devolvido pela fixture, confira o nome do banco e nunca execute este ajuste em outro ambiente:

```sql
UPDATE draft_montagens
SET turno_expira_em = now() - interval '1 second'
WHERE id = '<presenceDraftId>'
  AND status = 'Aberta'
  AND modo = 'TempoReal';
```

O serviço hospedado da API deve permanecer ativo para que o avanço seja produzido pelo worker real, e não pela fixture.
