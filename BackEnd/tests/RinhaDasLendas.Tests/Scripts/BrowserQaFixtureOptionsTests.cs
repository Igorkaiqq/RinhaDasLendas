using FluentAssertions;
using RinhaDasLendas.BrowserQaFixture;

namespace RinhaDasLendas.Tests.Scripts;

public sealed class BrowserQaFixtureOptionsTests
{
    [Fact]
    public void FromEnvironment_ShouldAcceptExplicitDisposableDatabaseAndDistinctAdmins()
    {
        var adminAId = Guid.NewGuid();
        var adminBId = Guid.NewGuid();

        var options = BrowserQaFixtureOptions.FromEnvironment(ValidEnvironment(adminAId, adminBId));

        options.AdminAId.Should().Be(adminAId);
        options.AdminBId.Should().Be(adminBId);
        options.AdminAEmail.Should().Be("qa-admin-a@example.test");
        options.AdminBEmail.Should().Be("qa-admin-b@example.test");
    }

    [Theory]
    [InlineData("rinha_das_lendas")]
    [InlineData("postgres")]
    [InlineData("rinha_feature029_browser_qa")]
    public void FromEnvironment_ShouldRejectDatabaseOutsideUniqueQaPrefix(string database)
    {
        var environment = ValidEnvironment(Guid.NewGuid(), Guid.NewGuid());
        environment["QA_DATABASE_CONNECTION"] = $"Host=postgres;Database={database};Username=postgres;Password=postgres";

        var action = () => BrowserQaFixtureOptions.FromEnvironment(environment);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*rinha_feature029_browser_qa_*");
    }

    [Fact]
    public void FromEnvironment_ShouldRejectEqualAuthenticatedUserIds()
    {
        var repeatedId = Guid.NewGuid();

        var action = () => BrowserQaFixtureOptions.FromEnvironment(ValidEnvironment(repeatedId, repeatedId));

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*distintos*");
    }

    [Theory]
    [InlineData("QA_ADMIN_A_PASSWORD")]
    [InlineData("QA_ADMIN_B_PASSWORD")]
    [InlineData("QA_ADMIN_A_EMAIL")]
    [InlineData("QA_ADMIN_B_EMAIL")]
    public void FromEnvironment_ShouldRequireExternalAuthenticationValues(string key)
    {
        var environment = ValidEnvironment(Guid.NewGuid(), Guid.NewGuid());
        environment.Remove(key);

        var action = () => BrowserQaFixtureOptions.FromEnvironment(environment);

        action.Should().Throw<InvalidOperationException>().WithMessage($"*{key}*");
    }

    private static Dictionary<string, string?> ValidEnvironment(Guid adminAId, Guid adminBId) => new()
    {
        ["QA_DATABASE_CONNECTION"] = "Host=postgres;Database=rinha_feature029_browser_qa_unit13;Username=postgres;Password=postgres",
        ["QA_ADMIN_A_ID"] = adminAId.ToString(),
        ["QA_ADMIN_B_ID"] = adminBId.ToString(),
        ["QA_ADMIN_A_EMAIL"] = "qa-admin-a@example.test",
        ["QA_ADMIN_B_EMAIL"] = "qa-admin-b@example.test",
        ["QA_ADMIN_A_PASSWORD"] = "external-a",
        ["QA_ADMIN_B_PASSWORD"] = "external-b",
    };
}
