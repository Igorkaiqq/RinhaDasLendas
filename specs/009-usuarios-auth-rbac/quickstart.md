# Quickstart: Usuários, Autenticação, Autorização e RBAC

## Purpose

Validar a feature após implementação, sem depender de Discord real.

## Prerequisites

- Banco PostgreSQL disponível conforme configuração do projeto.
- Backend executando migrations.
- Roles oficiais semeadas: `SuperAdmin`, `Admin`, `Moderador`, `Capitão`, `Jogador`.
- Pelo menos um SuperAdmin inicial configurado por mecanismo seguro.
- Frontend apontando para a API em `VITE_API_URL`.

## Backend Validation

Run from `BackEnd/`:

```bash
dotnet build
dotnet test
```

Expected outcome:

- Build succeeds.
- Auth validators pass.
- RBAC hierarchy tests pass.
- Refresh token rotation/reuse tests pass.
- User management handler tests pass.
- Integration tests cover protected endpoints returning `401`/`403` correctly.

## Frontend Validation

Run from `FrontEnd/`:

```bash
npm run lint
npm run build
npm run test:unit
```

Expected outcome:

- Build succeeds.
- Route guards redirect unauthenticated users.
- Forbidden state appears for insufficient permissions.
- Sidebar hides unauthorized navigation items.
- Auth service handles refresh and logout.

## Manual Scenario 1: Public Registration

1. Open `/register`.
2. Register with valid name, e-mail and password.
3. Login with the created account.
4. Open current user profile.

Expected outcome:

- Account exists.
- Role is `Jogador`.
- Administrative user menu is not visible.

## Manual Scenario 2: Login and Refresh

1. Login with active user.
2. Navigate to a protected page.
3. Reload the browser.
4. Wait for or simulate access-token expiration.

Expected outcome:

- Session is restored while refresh session is valid.
- API calls keep working after refresh.
- Logout revokes session and redirects to login.

## Manual Scenario 3: RBAC

1. Login as SuperAdmin.
2. Create or promote test users for Admin, Moderador, Capitão and Jogador.
3. Login as Admin and attempt to manage users.
4. Login as Moderador and attempt to alter roles.
5. Login as Jogador and attempt to access user management.

Expected outcome:

- SuperAdmin manages all roles.
- Admin manages only Moderador, Capitão and Jogador.
- Moderador cannot alter roles.
- Jogador cannot access user management.
- Backend returns `403` for prohibited direct calls.

## Manual Scenario 4: Password Recovery

1. Request forgot password for an existing e-mail.
2. Request forgot password for an unknown e-mail.
3. Use valid reset token for existing user.
4. Try old password and new password.

Expected outcome:

- Both requests return generic success message.
- Old password stops working.
- New password works.

## Manual Scenario 5: Draft Captain Eligibility

1. Ensure multiple users have linked active player profiles.
2. Assign `Capitão` explicitly to one user.
3. Keep Admin/SuperAdmin users without `Capitão` role.
4. Open draft captain selection.

Expected outcome:

- Only active players linked to users with explicit `Capitão` role are listed.
- Admin/SuperAdmin without `Capitão` do not appear.

## Manual Scenario 6: Discord Future Placeholder

1. Open user profile.
2. Check Discord section.

Expected outcome:

- UI can show not linked/linked status when data exists.
- OAuth action is hidden, disabled or marked as future unless explicitly enabled.
