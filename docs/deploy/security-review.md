# Revisão De Segurança Para Produção

## Segredos

- Nenhum segredo real deve ser commitado.
- O arquivo `.env` real deve existir somente na VPS ou no Portainer.
- Rotacionar segredos que já tenham sido expostos em ambiente local ou histórico.
- `JWT_KEY`, `POSTGRES_PASSWORD`, `RINHA_API_INTERNAL_TOKEN`, `DISCORD_TOKEN` e `DISCORD_CLIENT_SECRET` devem ser fortes e únicos.

## Rede

- Somente Traefik publica portas públicas.
- PostgreSQL fica apenas na rede interna `rinhadaslendas_internal`.
- Backend não publica porta direta no host.
- Bot não publica porta.

## Traefik

- HTTPS via `letsencryptresolver`.
- Headers de segurança aplicados por labels.
- Rate limit básico aplicado no router da API.
- Redirect HTTP para HTTPS já está configurado globalmente no Traefik do servidor.

## API

- `ASPNETCORE_ENVIRONMENT=Production` obrigatório.
- Swagger só é exposto em Development.
- CORS é configurado por `FRONTEND_PUBLIC_URL`.
- `ForwardedHeaders` habilitado para uso atrás do Traefik.
- Startup bloqueia JWT key padrão/dev e connection string com `postgres/postgres` em produção.
- `/health` fica disponível para healthcheck e Traefik.

## Frontend

- Build estático servido por Nginx.
- `VITE_ENABLE_FAKE_FALLBACK=false` em produção.
- Assets versionados recebem cache.
- SPA fallback não expõe arquivos sensíveis porque apenas `dist` entra na imagem.

## Discord Bot

- Usa `RINHA_API_INTERNAL_TOKEN` e rede interna.
- Logs estruturados devem evitar impressão de tokens.
- Não roda como root no container.

## Pendências De Segurança

- Adicionar secret scanning no CI.
- Avaliar usuário de deploy sem root.
- Separar migrations do startup antes de escalar backend acima de 1 réplica.
- Criar rotina de backup e teste de restore do banco.
