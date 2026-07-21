# Task 11 Report

## Status

- T099, T100, T101 e T102 concluídas.
- Motivo obrigatório, sem whitespace e com máximo de 500 caracteres em cancelamento, adição/remoção manual e republicação.
- Usuário administrativo válido resolvido antes da carga do agregado; `Guid.Empty` não é aceito.
- Adição manual audita executor, jogador-alvo e motivo normalizado.
- Operações bot-only continuam sem exigir GUID administrativo.

## RED

Backend:

```bash
docker compose -f .devcontainer/docker-compose.yml exec -T app dotnet test BackEnd/RinhaDasLendas.sln --filter "FullyQualifiedName~DraftMontagemValidatorTests" --configuration Release
```

Resultado inicial: compilação falhou porque `AdicionarPresencaManualDraftMontagemRequestDto` ainda não recebia motivo.

Frontend:

```bash
npm test -- --run src/services/draftMontagens.spec.ts src/components/drafts/DraftReasonDialog.spec.ts src/views/DraftsView.spec.ts
```

Resultado inicial: 39 testes aprovados e 5 falhos. As falhas comprovaram payload sem motivo, ausência da variante localizada, variante visual incorreta e submissão direta sem diálogo. Um ciclo RED adicional comprovou que motivos acima de 500 caracteres ainda eram enviados.

## Implementação

- Validators administrativos exigem motivo e aplicam limite de 500 caracteres.
- Validators de conclusão/falha de publicação validam tipo, `ClaimId`, `MessageId`, guild/channel até 40 e erro até 120.
- Aquisição, conclusão e falha de publicação usam `Enum.TryParse` e nunca convertem entrada desconhecida com `Enum.Parse`.
- Handlers administrativos usam resolução compartilhada de `ICurrentUser.UserId` antes de consultar o repositório.
- `DraftMontagem.AdicionarPresencaManual` registra `AdicaoPresencaManual` com executor, alvo e motivo.
- `DraftMontagemAcaoAdministrativa` rejeita executor `Guid.Empty`.
- Frontend envia `{ jogadorId, motivo }` e solicita o motivo pelo `DraftReasonDialog` antes da adição.
- A nova variante de adição é localizada, mostra o jogador-alvo e limita o motivo a 500 caracteres.

## Testes HTTP

- Motivo whitespace na adição manual retorna 400 localizado e não persiste presença nem auditoria.
- JWT administrativo sem `NameIdentifier` retorna 400 localizado e não cancela o draft.
- Adição válida persiste executor, alvo e motivo normalizado.
- Payload de publicação acima do limite retorna 400 e preserva o claim em andamento.
- Conclusão/falha bot-only com claim ativo continuam aprovadas sem executor administrativo.

## Auditoria de internacionalização

- Textos hardcoded novos no frontend: Não encontrados.
- Mensagens hardcoded novas no backend: Não encontradas.
- `pt.json` e `en.json`: Sim, 636 chaves em cada arquivo e nenhuma divergência.
- Recursos backend: Sim, 206 chaves em cada `.resx`; mensagem de limite de motivo generalizada em português e inglês.
- Acentuação em português: Sim, revisada.
- Placeholders, botões, títulos, badges, toasts e estados vazios: Sim, variante de adição revisada; demais itens não alterados.
- Validações frontend/backend com i18n/resources: Sim.
- Novos arquivos respeitam o padrão: Sim; apenas este relatório foi criado.

## Verificação final

- Backend focado: 29 testes aprovados, 0 falhos.
- Backend completo via SDK container, Docker socket e rede do compose: 183 testes aprovados, 0 falhos.
- Backend build Release: concluído, 0 erros.
- Frontend: 72 testes aprovados, 0 falhos.
- Frontend build: concluído sem erros.
- `git diff --check`: concluído sem erros.

## Concerns

- `Microsoft.OpenApi 2.4.1` mantém aviso NU1903 de vulnerabilidade alta já existente.
- O build frontend mantém aviso já existente de chunk JavaScript acima de 500 kB.
