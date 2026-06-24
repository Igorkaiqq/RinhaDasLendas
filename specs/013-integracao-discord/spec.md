# Feature Specification: Integração Discord

**Branch**: `feature/013-integracao-discord`  
**Date**: 2026-06-24

## Summary

Adicionar Discord como autenticação externa opcional e vínculo de conta para usuários internos já existentes. A feature deve preservar login tradicional, cadastro tradicional, Identity, JWT, roles, permissões e Draft existentes.

## User Stories

### Login com Discord vinculado

Como usuário com Discord já vinculado, quero entrar usando Discord para acessar a plataforma sem digitar senha.

### Login com Discord sem conta existente

Como visitante sem vínculo, ao tentar entrar com Discord devo ter uma conta interna criada automaticamente e já vinculada ao Discord.

### Vincular Discord

Como usuário autenticado, quero vincular minha conta Discord em integrações para habilitar login externo e futuras funcionalidades sociais.

### Desvincular Discord

Como usuário autenticado, quero remover o vínculo Discord sem perder acesso pela senha tradicional.

## Requirements

- O login tradicional deve permanecer sem alteração comportamental.
- O cadastro tradicional deve permanecer sem alteração comportamental.
- O backend deve usar OAuth2 Authorization Code Grant do Discord.
- O backend deve gerar, expirar e invalidar `state` anti-CSRF.
- O backend deve trocar `code` por token usando `application/x-www-form-urlencoded`.
- O backend deve buscar o Discord ID oficial antes de autenticar ou vincular.
- O backend deve criar usuário interno automaticamente quando login Discord não encontrar vínculo e o Discord retornar e-mail utilizável.
- O login Discord deve reutilizar o mesmo mecanismo interno de emissão JWT e refresh token.
- O vínculo deve ser genérico para suportar futuros provedores.
- Toda mensagem backend deve usar resources.
- Todo texto frontend deve usar i18n.

## Success Criteria

- Usuário vinculado consegue iniciar OAuth Discord e receber sessão interna válida.
- Usuário não vinculado é criado como `Jogador`, tem Discord vinculado e recebe sessão interna válida.
- Usuário autenticado consegue vincular e desvincular Discord.
- Índices únicos impedem duplicidade de provedor/usuário externo ativo.
- Builds e testes existentes de backend e frontend permanecem funcionais.

## Non-Goals

- Não implementar bot Discord.
- Não sincronizar guilds, cargos ou presença.
- Não persistir tokens Discord para uso futuro.
- Não alterar autorização, roles ou Draft.
