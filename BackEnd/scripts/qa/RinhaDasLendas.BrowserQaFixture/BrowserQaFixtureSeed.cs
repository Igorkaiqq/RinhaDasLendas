using Microsoft.AspNetCore.Identity;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Infrastructure.Identity;

namespace RinhaDasLendas.BrowserQaFixture;

public sealed record BrowserQaFixtureMetadata(
    Guid PresenceDraftId,
    Guid ManualDraftId,
    Guid AdminAId,
    Guid AdminBId,
    IReadOnlyList<Guid> PlayerIds);

public sealed record BrowserQaFixtureSeed(
    IReadOnlyCollection<ApplicationUser> Users,
    IReadOnlyList<Jogador> Players,
    IReadOnlyCollection<IdentityUserRole<Guid>> UserRoles,
    DraftMontagem PresenceDraft,
    DraftMontagem ManualDraft,
    BrowserQaFixtureMetadata Metadata)
{
    public static BrowserQaFixtureSeed Create(
        BrowserQaFixtureOptions options,
        IReadOnlyDictionary<string, Guid> roleIds)
    {
        var superAdminRoleId = RequiredRole(roleIds, AuthRoles.SuperAdmin);
        var captainRoleId = RequiredRole(roleIds, AuthRoles.Capitao);
        var playerRoleId = RequiredRole(roleIds, AuthRoles.Jogador);
        var users = new List<ApplicationUser>
        {
            CreateAuthenticatedUser(options.AdminAId, "QA Admin A", options.AdminAEmail, options.AdminAPassword),
            CreateAuthenticatedUser(options.AdminBId, "QA Admin B", options.AdminBEmail, options.AdminBPassword),
            CreateFixtureUser("QA Titular 3"),
            CreateFixtureUser("QA Titular 4"),
            CreateFixtureUser("QA Reserva Capitão"),
        };
        var players = users.Select((user, index) => CreatePlayer(index + 1, user.Id)).ToList();
        var userRoles = new List<IdentityUserRole<Guid>>
        {
            new() { UserId = options.AdminAId, RoleId = superAdminRoleId },
            new() { UserId = options.AdminAId, RoleId = captainRoleId },
            new() { UserId = options.AdminBId, RoleId = superAdminRoleId },
            new() { UserId = options.AdminBId, RoleId = captainRoleId },
            new() { UserId = users[2].Id, RoleId = playerRoleId },
            new() { UserId = users[3].Id, RoleId = playerRoleId },
            new() { UserId = users[4].Id, RoleId = captainRoleId },
        };

        var presenceDraft = DraftMontagem.CriarPorPresenca("QA Realtime Completo", "Fixture local descartável da Unidade 13", 2);
        foreach (var (user, player) in users.Zip(players))
        {
            presenceDraft.ConfirmarPresenca(user.Id, player.Id, null, DraftMontagemPresencaOrigem.Web);
        }

        var manualDraft = DraftMontagem.CriarManualDireto(
            "QA Layout Concorrente",
            "Fixture local descartável para dirty layout e conflito 409",
            2,
            players.Take(4).Select(player => player.Id).ToList());
        var metadata = new BrowserQaFixtureMetadata(
            presenceDraft.Id,
            manualDraft.Id,
            options.AdminAId,
            options.AdminBId,
            players.Select(player => player.Id).ToList());

        return new BrowserQaFixtureSeed(users, players, userRoles, presenceDraft, manualDraft, metadata);
    }

    private static Guid RequiredRole(IReadOnlyDictionary<string, Guid> roleIds, string role)
        => roleIds.TryGetValue(role, out var roleId)
            ? roleId
            : throw new InvalidOperationException($"O cargo obrigatório {role} não existe no banco descartável.");

    private static ApplicationUser CreateAuthenticatedUser(Guid id, string name, string email, string password)
    {
        var user = CreateUser(id, name, email);
        user.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, password);
        return user;
    }

    private static ApplicationUser CreateFixtureUser(string name)
    {
        var id = Guid.NewGuid();
        return CreateUser(id, name, $"qa-{id:N}@example.test");
    }

    private static ApplicationUser CreateUser(Guid id, string name, string email) => new()
    {
        Id = id,
        Nome = name,
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        EmailConfirmed = true,
        Ativo = true,
        SecurityStamp = Guid.NewGuid().ToString("N"),
        ConcurrencyStamp = Guid.NewGuid().ToString("N"),
    };

    private static Jogador CreatePlayer(int index, Guid userId)
    {
        var player = new Jogador(
            $"QA Jogador {index}",
            null,
            $"qa{index}#BR1",
            null,
            null,
            null,
            Elo.Ouro,
            Divisao.II,
            [
                new PreferenciaRota(Rota.Top, 1, false),
                new PreferenciaRota(Rota.Jungle, 2, false),
                new PreferenciaRota(Rota.Mid, 3, false),
                new PreferenciaRota(Rota.Adc, 4, false),
                new PreferenciaRota(Rota.Support, 5, false),
            ]);
        player.VincularUsuario(userId);
        return player;
    }
}
