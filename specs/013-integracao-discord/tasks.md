# Tasks: Integração Discord

## Backend

- [ ] Criar entidades `ExternalAccount` e `ExternalAuthState`.
- [ ] Mapear entidades no `RinhaDasLendasDbContext`.
- [ ] Criar migration para tabelas genéricas e migração de `vinculos_discord`.
- [ ] Criar opções `DiscordOptions`.
- [ ] Criar DTOs de resposta/status/callback.
- [ ] Criar interfaces de serviços OAuth/contas externas.
- [ ] Implementar serviços Discord HTTP e vínculo externo.
- [ ] Reutilizar emissão JWT existente para login externo.
- [ ] Criar usuário interno com role `Jogador` quando login Discord não encontrar vínculo e houver e-mail utilizável.
- [ ] Criar commands/handlers para login, link, callback e unlink.
- [ ] Criar endpoints REST em `AuthController`.
- [ ] Adicionar mensagens `.resx` em pt-BR/en-US/default.
- [ ] Adicionar testes de regras principais.

## Frontend

- [ ] Ler design system antes de implementar UI.
- [ ] Adicionar botão “Entrar com Discord” no login usando i18n.
- [ ] Criar/ajustar serviço frontend para fluxo Discord.
- [ ] Criar tela `Configurações > Integrações`.
- [ ] Atualizar seção Discord com estados carregando, vinculado, não vinculado e erro.
- [ ] Adicionar rota de sucesso/erro se necessário.
- [ ] Atualizar `pt.json` e `en.json` mantendo sincronização.

## Verification

- [ ] Executar testes backend.
- [ ] Executar build backend.
- [ ] Executar testes frontend.
- [ ] Executar build frontend.
- [ ] Auditar internacionalização.
