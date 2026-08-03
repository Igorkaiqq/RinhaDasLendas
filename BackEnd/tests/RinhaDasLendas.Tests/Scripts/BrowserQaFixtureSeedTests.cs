using FluentAssertions;
using RinhaDasLendas.BrowserQaFixture;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Tests.Scripts;

public sealed class BrowserQaFixtureSeedTests
{
    [Fact]
    public void Create_ShouldBuildTwoAdminsFiveLinkedPlayersAndRequiredDraftStates()
    {
        var options = CreateOptions();
        var roles = AuthRoles.Levels.Keys.ToDictionary(role => role, _ => Guid.NewGuid());

        var seed = BrowserQaFixtureSeed.Create(options, roles);

        seed.Users.Should().HaveCount(5);
        seed.Players.Should().HaveCount(5);
        seed.Players.Should().OnlyContain(player => player.UsuarioId.HasValue && player.Status == JogadorStatus.Ativo);
        seed.Players.Take(2).Select(player => player.UsuarioId).Should().Equal(options.AdminAId, options.AdminBId);
        seed.UserRoles.Where(item => item.RoleId == roles[AuthRoles.SuperAdmin]).Select(item => item.UserId)
            .Should().BeEquivalentTo([options.AdminAId, options.AdminBId]);
        seed.UserRoles.Count(item => item.RoleId == roles[AuthRoles.Capitao]).Should().Be(3);

        seed.PresenceDraft.Status.Should().Be(DraftMontagemStatus.PresencaAberta);
        seed.PresenceDraft.Modo.Should().BeNull();
        seed.PresenceDraft.Presencas.Should().HaveCount(5).And.OnlyContain(item => item.Confirmada);

        seed.ManualDraft.Status.Should().Be(DraftMontagemStatus.Aberta);
        seed.ManualDraft.Modo.Should().Be(DraftMontagemModo.Manual);
        seed.ManualDraft.QuantidadeTimes.Should().Be(2);
        seed.ManualDraft.Participantes.Should().HaveCount(4);
    }

    [Fact]
    public void Create_ShouldHashExternalPasswordsWithoutReturningThemInMetadata()
    {
        var options = CreateOptions();
        var roles = AuthRoles.Levels.Keys.ToDictionary(role => role, _ => Guid.NewGuid());

        var seed = BrowserQaFixtureSeed.Create(options, roles);

        seed.Users.Single(user => user.Id == options.AdminAId).PasswordHash.Should().NotContain(options.AdminAPassword);
        seed.Users.Single(user => user.Id == options.AdminBId).PasswordHash.Should().NotContain(options.AdminBPassword);
        seed.Metadata.ToString().Should().NotContain(options.AdminAPassword).And.NotContain(options.AdminBPassword);
    }

    private static BrowserQaFixtureOptions CreateOptions() => new(
        "Host=postgres;Database=rinha_feature029_browser_qa_tests;Username=postgres;Password=postgres",
        Guid.NewGuid(),
        Guid.NewGuid(),
        "qa-admin-a@example.test",
        "qa-admin-b@example.test",
        "external-password-a",
        "external-password-b");
}
