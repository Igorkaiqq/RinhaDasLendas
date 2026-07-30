using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RinhaDasLendas.Api.Services;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Models;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Tests.Services;

public sealed class DraftMontagemPublicationReconciliationServiceTests
{
    [Fact]
    public async Task CicloDeveCriarEscopoEInvocarExpiracao()
    {
        var stamps = new[]
        {
            new DraftMontagemVersionStamp(Guid.NewGuid(), 7, DateTimeOffset.UtcNow),
            new DraftMontagemVersionStamp(Guid.NewGuid(), 9, DateTimeOffset.UtcNow),
        };
        var repository = new Mock<IDraftMontagemRepository>();
        repository
            .Setup(item => item.MarcarPublicacoesExpiradasParaReconciliacaoAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(stamps);
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddScoped(_ => repository.Object)
            .AddScoped(_ => publisher.Object)
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
        publisher.Verify(item => item.PublishAfterCommitAsync(It.IsIn(stamps.Select(stamp => stamp.Id)), default), Times.Exactly(2));
    }

    [Fact]
    public async Task CicloSemExpiracaoNaoDeveNotificar()
    {
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.MarcarPublicacoesExpiradasParaReconciliacaoAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddScoped(_ => repository.Object)
            .AddScoped(_ => publisher.Object)
            .BuildServiceProvider();
        var service = new DraftMontagemPublicationReconciliationService(
            services.GetRequiredService<IServiceScopeFactory>(),
            services.GetRequiredService<IConfiguration>(),
            services.GetRequiredService<ILogger<DraftMontagemPublicationReconciliationService>>());

        var result = await service.RunCycleAsync(CancellationToken.None);

        result.Should().Be(0);
        publisher.Verify(item => item.PublishAfterCommitAsync(It.IsAny<Guid>(), default), Times.Never);
    }
}
