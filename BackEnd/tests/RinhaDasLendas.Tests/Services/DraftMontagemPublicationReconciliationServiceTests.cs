using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RinhaDasLendas.Api.Services;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Tests.Services;

public sealed class DraftMontagemPublicationReconciliationServiceTests
{
    [Fact]
    public async Task CicloDeveCriarEscopoEInvocarExpiracao()
    {
        var repository = new Mock<IDraftMontagemRepository>();
        repository
            .Setup(item => item.MarcarPublicacoesExpiradasParaReconciliacaoAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddScoped(_ => repository.Object)
            .BuildServiceProvider();
        var service = new DraftMontagemPublicationReconciliationService(
            services.GetRequiredService<IServiceScopeFactory>(),
            services.GetRequiredService<IConfiguration>(),
            services.GetRequiredService<ILogger<DraftMontagemPublicationReconciliationService>>());

        var result = await service.RunCycleAsync(CancellationToken.None);

        result.Should().Be(2);
        repository.Verify(item => item.MarcarPublicacoesExpiradasParaReconciliacaoAsync(
            It.IsAny<DateTimeOffset>(),
            CancellationToken.None), Times.Once);
    }
}
