using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RinhaDasLendas.Domain.Models;
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Infrastructure.Repositories;

namespace RinhaDasLendas.Tests.Infrastructure;

public sealed class DraftMontagemCandidateProjectionTests
{
    [Fact]
    public void CandidatoRealtimeDeveConterSomenteId()
    {
        typeof(DraftMontagemRealtimeCandidate).GetProperties()
            .Select(property => property.Name)
            .Should().Equal(nameof(DraftMontagemRealtimeCandidate.Id));
    }

    [Fact]
    public void ConsultaDeCandidatosRealtimeDeveProjetarSomenteIdSemIncludes()
    {
        var options = new DbContextOptionsBuilder<RinhaDasLendasDbContext>()
            .UseNpgsql("Host=localhost;Database=projection_only;Username=test;Password=test")
            .Options;
        using var context = new RinhaDasLendasDbContext(options);

        var sql = DraftMontagemRepository
            .BuildExpiredRealtimeCandidatesQuery(context.DraftMontagens, DateTimeOffset.UtcNow, 25)
            .ToQueryString();

        sql.Should().Contain("SELECT d.id");
        var normalizedSql = sql.ToUpperInvariant();
        normalizedSql.Should().NotContain("JOIN");
        normalizedSql.Should().NotContain("DRAFT_MONTAGEM_TIMES");
        normalizedSql.Should().NotContain("DRAFT_MONTAGEM_PARTICIPANTES");
        normalizedSql.Should().NotContain("DRAFT_MONTAGEM_ESCOLHAS");
        normalizedSql.Should().NotContain("DRAFT_MONTAGEM_ACOES_ADMINISTRATIVAS");
    }
}
