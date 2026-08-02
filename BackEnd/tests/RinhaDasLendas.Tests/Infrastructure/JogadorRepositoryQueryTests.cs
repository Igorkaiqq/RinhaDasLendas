using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Infrastructure.Repositories;

namespace RinhaDasLendas.Tests.Infrastructure;

public sealed class JogadorRepositoryQueryTests
{
    [Fact]
    public void ConsultaDeCapitaesElegiveisDeveDesempatarPeloId()
    {
        var options = new DbContextOptionsBuilder<RinhaDasLendasDbContext>()
            .UseNpgsql("Host=localhost;Database=eligible_captains_order;Username=test;Password=test")
            .Options;
        using var context = new RinhaDasLendasDbContext(options);

        var sql = JogadorRepository
            .BuildCapitaesElegiveisQuery(context, Guid.NewGuid())
            .ToQueryString()
            .ToUpperInvariant();

        sql.Should().Contain("ORDER BY J.NOME_EXIBICAO, J.ID");
    }
}
