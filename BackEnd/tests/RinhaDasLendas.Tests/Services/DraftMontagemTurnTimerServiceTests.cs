using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RinhaDasLendas.Api.Services;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Enums;
using RinhaDasLendas.Application.Handlers.DraftMontagens;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Models;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Tests.Services;

public sealed class DraftMontagemTurnTimerServiceTests
{
    [Fact]
    public async Task CicloDeveCriarUmScopeDeComandoPorIdEContinuarAposFalhaGenerica()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var senders = new List<Mock<ISender>>();
        using var provider = BuildProvider(
            [new(firstId), new(secondId)],
            senders,
            (_, index, _) => index == 0 ? Task.FromException(new InvalidOperationException("item failure")) : Task.CompletedTask);
        var service = CreateService(provider);

        var processed = await service.RunCycleAsync(CancellationToken.None);

        processed.Should().Be(1);
        senders.Should().HaveCount(2);
        senders.SelectMany(sender => sender.Invocations)
            .Select(invocation => ((ProcessarTurnoDraftMontagemExpiradoCommand)invocation.Arguments[0]).Id)
            .Should().Equal(firstId, secondId);
    }

    [Fact]
    public async Task CicloDeveObservarCancelamentoNaoOriginadoPeloHostEContinuar()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var senders = new List<Mock<ISender>>();
        using var provider = BuildProvider(
            [new(firstId), new(secondId)],
            senders,
            (_, index, _) => index == 0 ? Task.FromCanceled(new CancellationToken(true)) : Task.CompletedTask);
        var service = CreateService(provider);

        var processed = await service.RunCycleAsync(CancellationToken.None);

        processed.Should().Be(1);
        senders.Should().HaveCount(2);
    }

    [Fact]
    public async Task CicloDevePropagarCancelamentoDoHostSemProcessarProximoId()
    {
        var cancellation = new CancellationTokenSource();
        var senders = new List<Mock<ISender>>();
        using var provider = BuildProvider(
            [new(Guid.NewGuid()), new(Guid.NewGuid())],
            senders,
            (_, _, _) =>
            {
                cancellation.Cancel();
                return Task.FromCanceled(cancellation.Token);
            });
        var service = CreateService(provider);

        await service.Invoking(item => item.RunCycleAsync(cancellation.Token))
            .Should().ThrowAsync<OperationCanceledException>();
        senders.Should().ContainSingle();
    }

    [Fact]
    public async Task ComandoDeveRecarregarEIgnorarCandidatoQueNaoEstaMaisExpirado()
    {
        var montagem = CriarTempoRealIniciado(DateTimeOffset.UtcNow);
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.ReloadByIdAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        var metrics = new Mock<IDraftMontagemMetrics>();
        var handler = new ProcessarTurnoDraftMontagemExpiradoCommandHandler(repository.Object, publisher.Object, metrics.Object);

        await handler.Handle(new(montagem.Id, TimeSpan.FromHours(2)), CancellationToken.None);

        repository.Verify(item => item.ReloadByIdAsync(montagem.Id, CancellationToken.None), Times.Once);
        repository.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        publisher.Verify(item => item.PublishAfterCommitAsync(
            It.IsAny<Guid>(), It.IsAny<DraftMontagemSnapshotScope>(), It.IsAny<DraftMontagemAvailabilityChange>()), Times.Never);
    }

    [Fact]
    public async Task ComandoDeveRegistrarTimeoutSemAuditoriaAdministrativaExtra()
    {
        var montagem = CriarTempoRealIniciado(DateTimeOffset.UtcNow.AddMinutes(-1));
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.ReloadByIdAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        var metrics = new Mock<IDraftMontagemMetrics>();
        var handler = new ProcessarTurnoDraftMontagemExpiradoCommandHandler(repository.Object, publisher.Object, metrics.Object);

        await handler.Handle(new(montagem.Id, TimeSpan.FromHours(2)), CancellationToken.None);

        montagem.Escolhas.Should().ContainSingle(escolha => escolha.Tipo == DraftMontagemEscolhaTipo.Timeout);
        montagem.AcoesAdministrativas.Should().BeEmpty();
        repository.Verify(item => item.SaveChangesAsync(CancellationToken.None), Times.Once);
        publisher.Verify(item => item.PublishAfterCommitAsync(montagem.Id, DraftMontagemSnapshotScope.Active, DraftMontagemAvailabilityChange.None), Times.Once);
        metrics.Verify(item => item.RecordDraftTimeout(montagem.Id), Times.Once);
        metrics.Verify(item => item.RecordDraftCancelled(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ComandoDeveCancelarDuracaoMaximaComUmaUnicaAuditoriaSystem()
    {
        var montagem = CriarTempoRealIniciado(DateTimeOffset.UtcNow.AddHours(-3));
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.ReloadByIdAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        var metrics = new Mock<IDraftMontagemMetrics>();
        var handler = new ProcessarTurnoDraftMontagemExpiradoCommandHandler(repository.Object, publisher.Object, metrics.Object);

        await handler.Handle(new(montagem.Id, TimeSpan.FromHours(2)), CancellationToken.None);

        montagem.Status.Should().Be(DraftMontagemStatus.Cancelada);
        montagem.Escolhas.Should().BeEmpty();
        montagem.AcoesAdministrativas.Should().ContainSingle();
        montagem.AcoesAdministrativas.Single().ResponsavelTipo.Should().Be(DraftMontagemActorType.System);
        montagem.AcoesAdministrativas.Single().ResponsavelUsuarioId.Should().BeNull();
        repository.Verify(item => item.SaveChangesAsync(CancellationToken.None), Times.Once);
        publisher.Verify(item => item.PublishAfterCommitAsync(montagem.Id, DraftMontagemSnapshotScope.Active, DraftMontagemAvailabilityChange.None), Times.Once);
        metrics.Verify(item => item.RecordDraftCancelled(montagem.Id), Times.Once);
        metrics.Verify(item => item.RecordDraftTimeout(It.IsAny<Guid>()), Times.Never);
    }

    private static ServiceProvider BuildProvider(
        IReadOnlyCollection<DraftMontagemRealtimeCandidate> candidates,
        ICollection<Mock<ISender>> senders,
        Func<ProcessarTurnoDraftMontagemExpiradoCommand, int, CancellationToken, Task> send)
    {
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.ListExpiredRealtimeAsync(It.IsAny<DateTimeOffset>(), 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidates);
        var senderIndex = 0;
        return new ServiceCollection()
            .AddSingleton(repository.Object)
            .AddScoped<ISender>(_ =>
            {
                var index = senderIndex++;
                var sender = new Mock<ISender>();
                sender.Setup(item => item.Send(It.IsAny<ProcessarTurnoDraftMontagemExpiradoCommand>(), It.IsAny<CancellationToken>()))
                    .Returns((ProcessarTurnoDraftMontagemExpiradoCommand command, CancellationToken token) => send(command, index, token));
                senders.Add(sender);
                return sender.Object;
            })
            .BuildServiceProvider();
    }

    private static DraftMontagemTurnTimerService CreateService(IServiceProvider provider)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DraftMontagem:RealtimeMaxDurationMinutes"] = "120",
        }).Build();
        return new DraftMontagemTurnTimerService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            configuration,
            NullLogger<DraftMontagemTurnTimerService>.Instance);
    }

    private static DraftMontagem CriarTempoRealIniciado(DateTimeOffset inicio)
    {
        var montagem = DraftMontagem.CriarPorPresenca("Rinha", null, 2);
        var jogadoresIds = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToList();
        foreach (var jogadorId in jogadoresIds)
        {
            montagem.ConfirmarPresenca(Guid.NewGuid(), jogadorId, null, DraftMontagemPresencaOrigem.Web);
        }

        montagem.EncerrarPresenca(true, 2);
        montagem.SelecionarModo(DraftMontagemModo.TempoReal, jogadoresIds.ToHashSet());
        var capitaesIds = jogadoresIds.Take(2).ToList();
        montagem.DefinirCapitaes(capitaesIds, capitaesIds.ToHashSet());
        montagem.DefinirOrdemEscolha(DraftMontagemOrdemEscolhaModo.Manual, capitaesIds);
        montagem.IniciarTempoReal(inicio, capitaesIds.ToHashSet());
        return montagem;
    }
}
