using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RinhaDasLendas.Api.Services;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Tests.Services;

public sealed class DraftMontagemPublicationReconciliationServiceTests
{
    [Fact]
    public async Task CicloDeveCriarEscopoEInvocarExpiracao()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        var repository = new Mock<IDraftMontagemRepository>();
        repository
            .Setup(item => item.MarcarPublicacoesExpiradasParaReconciliacaoAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ids);
        repository.Setup(item => item.ReloadByIdAsync(It.IsIn(ids), It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddScoped(_ => repository.Object)
            .AddScoped(_ => notifier.Object)
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
        notifier.Verify(item => item.StateUpdatedAsync(
            It.IsIn(ids),
            It.IsAny<DraftMontagemRealtimeStateDto>(),
            CancellationToken.None), Times.Exactly(2));
    }

    [Fact]
    public async Task CicloSemExpiracaoNaoDeveNotificar()
    {
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.MarcarPublicacoesExpiradasParaReconciliacaoAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddScoped(_ => repository.Object)
            .AddScoped(_ => notifier.Object)
            .BuildServiceProvider();
        var service = new DraftMontagemPublicationReconciliationService(
            services.GetRequiredService<IServiceScopeFactory>(),
            services.GetRequiredService<IConfiguration>(),
            services.GetRequiredService<ILogger<DraftMontagemPublicationReconciliationService>>());

        var result = await service.RunCycleAsync(CancellationToken.None);

        result.Should().Be(0);
        notifier.Verify(item => item.StateUpdatedAsync(
            It.IsAny<Guid>(),
            It.IsAny<DraftMontagemRealtimeStateDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
