using System.Text.Json;
using FluentAssertions;
using Moq;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Enums;
using RinhaDasLendas.Application.Handlers.DraftMontagens;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Services;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Models;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Tests.Application;

public sealed class DraftMontagemPublicationRealtimeTests
{
    [Fact]
    public async Task SucessoDeCancelamentoArquivado_DevePublicarUmSnapshotSemEventoDeDisponibilidade()
    {
        var montagem = CreateArchivedCancellationMontagem();
        var claimId = Guid.NewGuid();
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.TryConcluirPublicacaoDiscordAsync(
                montagem.Id,
                DraftMontagemPublicacaoDiscordTipo.Cancelamento,
                claimId,
                null,
                null,
                "message",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Stamp(montagem.Id));
        repository.Setup(item => item.ReloadByIdIncludingArchivedAsync(
                montagem.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(montagem);
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        var publisher = new DraftMontagemRealtimePublisher(
            repository.Object,
            notifier.Object,
            Mock.Of<IDraftMontagemRealtimeTelemetry>());
        var handler = new RegistrarPublicacaoDiscordDraftMontagemCommandHandler(
            repository.Object,
            new RegistrarPublicacaoDiscordDraftMontagemValidator(),
            Mock.Of<IDraftMontagemMetrics>(),
            publisher);

        await handler.Handle(
            new RegistrarPublicacaoDiscordDraftMontagemCommand(
                montagem.Id,
                new RegistrarPublicacaoDiscordDraftMontagemRequestDto(
                    "Cancelamento", claimId, null, null, "message")),
            CancellationToken.None);

        notifier.Verify(item => item.SharedStateUpdatedAsync(
            montagem.Id,
            It.IsAny<DraftMontagemRealtimeSnapshotDto>(),
            It.IsAny<CancellationToken>()), Times.Once);
        notifier.Verify(item => item.ArchivedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        notifier.Verify(item => item.RestoredAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(item => item.ReloadByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ClaimDeCancelamentoArquivado_DeveSolicitarSnapshotIncludingArchivedSemAvailability()
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.MarcarPublicacoesExpiradasParaReconciliacaoAsync(
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repository.Setup(item => item.TryClaimPublicacaoDiscordAsync(
                id,
                DraftMontagemPublicacaoDiscordTipo.Cancelamento,
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DraftMontagemPublicacaoClaimResult(
                new DraftMontagemPublicacaoClaim(
                    true, Guid.NewGuid(), now.AddMinutes(5), DraftMontagemPublicacaoDiscordStatus.EmAndamento),
                Stamp(id)));
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        var handler = new AdquirirClaimPublicacaoDiscordDraftMontagemCommandHandler(
            repository.Object,
            new AdquirirClaimPublicacaoDiscordDraftMontagemValidator(),
            publisher.Object);

        await handler.Handle(
            new AdquirirClaimPublicacaoDiscordDraftMontagemCommand(
                id, new AdquirirClaimPublicacaoDiscordDraftMontagemRequestDto("Cancelamento")),
            CancellationToken.None);

        publisher.Verify(item => item.PublishAfterCommitAsync(
            id,
            DraftMontagemSnapshotScope.IncludingArchived,
            DraftMontagemAvailabilityChange.None), Times.Once);
    }

    [Fact]
    public async Task FalhaDeCancelamentoArquivado_DeveSolicitarSnapshotIncludingArchivedSemAvailability()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.TryRegistrarFalhaPublicacaoDiscordAsync(
                id,
                DraftMontagemPublicacaoDiscordTipo.Cancelamento,
                It.IsAny<Guid>(),
                null,
                null,
                "erro",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Stamp(id));
        repository.Setup(item => item.ReloadByIdIncludingArchivedAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateArchivedCancellationMontagem());
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        var handler = new RegistrarFalhaPublicacaoDiscordDraftMontagemCommandHandler(
            repository.Object,
            new RegistrarFalhaPublicacaoDiscordDraftMontagemValidator(),
            Mock.Of<IDraftMontagemMetrics>(),
            publisher.Object);

        await handler.Handle(
            new RegistrarFalhaPublicacaoDiscordDraftMontagemCommand(
                id,
                new RegistrarFalhaPublicacaoDiscordDraftMontagemRequestDto(
                    "Cancelamento", Guid.NewGuid(), null, null, "erro")),
            CancellationToken.None);

        publisher.Verify(item => item.PublishAfterCommitAsync(
            id,
            DraftMontagemSnapshotScope.IncludingArchived,
            DraftMontagemAvailabilityChange.None), Times.Once);
    }

    [Fact]
    public async Task RepublicacaoDeCancelamentoArquivado_DevePublicarUmSnapshotENoOpNaoDeveSalvarOuEmitirEvento()
    {
        var userId = Guid.NewGuid();
        var montagem = CreateArchivedCancellationMontagem(userId);
        var now = DateTimeOffset.UtcNow;
        var claimId = Guid.NewGuid();
        montagem.IniciarTentativaPublicacaoDiscord(
            DraftMontagemPublicacaoDiscordTipo.Cancelamento,
            null,
            null,
            claimId,
            now.AddMinutes(5),
            now);
        montagem.RegistrarFalhaPublicacaoDiscord(
            DraftMontagemPublicacaoDiscordTipo.Cancelamento,
            claimId,
            null,
            null,
            "erro",
            now.AddSeconds(1));
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.GetByIdIncludingArchivedAsync(
                montagem.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(montagem);
        repository.Setup(item => item.ReloadByIdIncludingArchivedAsync(
                montagem.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(montagem);
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        var publisher = new DraftMontagemRealtimePublisher(
            repository.Object,
            notifier.Object,
            Mock.Of<IDraftMontagemRealtimeTelemetry>());
        var handler = new RepublicarCancelamentoDraftArquivadoCommandHandler(
            repository.Object,
            new TestCurrentUser(userId),
            publisher);
        var auditCountBefore = montagem.AcoesAdministrativas.Count;

        await handler.Handle(new RepublicarCancelamentoDraftArquivadoCommand(montagem.Id), CancellationToken.None);
        var auditCountAfterFirst = montagem.AcoesAdministrativas.Count;
        await handler.Handle(new RepublicarCancelamentoDraftArquivadoCommand(montagem.Id), CancellationToken.None);

        auditCountAfterFirst.Should().Be(auditCountBefore + 1);
        montagem.AcoesAdministrativas.Should().HaveCount(auditCountAfterFirst);
        repository.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        notifier.Verify(item => item.SharedStateUpdatedAsync(
            montagem.Id,
            It.IsAny<DraftMontagemRealtimeSnapshotDto>(),
            It.IsAny<CancellationToken>()), Times.Once);
        notifier.Verify(item => item.ArchivedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        notifier.Verify(item => item.RestoredAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task ConclusaoDevePublicarStampUmaVezAposPersistencia()
    {
        var id = Guid.NewGuid();
        var claimId = Guid.NewGuid();
        var persisted = false;
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.TryConcluirPublicacaoDiscordAsync(
                id, DraftMontagemPublicacaoDiscordTipo.Presenca, claimId, "guild", "channel", "message",
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Callback(() => persisted = true)
            .ReturnsAsync(Stamp(id));
        repository.Setup(item => item.ReloadByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePublishedMontagem(claimId));
        var publisher = PublisherAsserting(() => persisted.Should().BeTrue());
        var handler = new RegistrarPublicacaoDiscordDraftMontagemCommandHandler(
            repository.Object, new RegistrarPublicacaoDiscordDraftMontagemValidator(),
            Mock.Of<IDraftMontagemMetrics>(), publisher.Object);

        await handler.Handle(
            new RegistrarPublicacaoDiscordDraftMontagemCommand(
                id, new RegistrarPublicacaoDiscordDraftMontagemRequestDto("Presenca", claimId, "guild", "channel", "message")),
            CancellationToken.None);

        publisher.Verify(item => item.PublishAfterCommitAsync(id, default), Times.Once);
    }

    [Fact]
    public async Task FalhaDevePublicarStampUmaVezAposPersistencia()
    {
        var id = Guid.NewGuid();
        var claimId = Guid.NewGuid();
        var persisted = false;
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.TryRegistrarFalhaPublicacaoDiscordAsync(
                id, DraftMontagemPublicacaoDiscordTipo.Presenca, claimId, "guild", "channel", "erro",
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Callback(() => persisted = true)
            .ReturnsAsync(Stamp(id));
        repository.Setup(item => item.ReloadByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFailedMontagem(claimId));
        var publisher = PublisherAsserting(() => persisted.Should().BeTrue());
        var handler = new RegistrarFalhaPublicacaoDiscordDraftMontagemCommandHandler(
            repository.Object, new RegistrarFalhaPublicacaoDiscordDraftMontagemValidator(),
            Mock.Of<IDraftMontagemMetrics>(), publisher.Object);

        await handler.Handle(
            new RegistrarFalhaPublicacaoDiscordDraftMontagemCommand(
                id, new RegistrarFalhaPublicacaoDiscordDraftMontagemRequestDto("Presenca", claimId, "guild", "channel", "erro")),
            CancellationToken.None);

        publisher.Verify(item => item.PublishAfterCommitAsync(id, default), Times.Once);
    }

    [Fact]
    public async Task ClaimAdquiridoDevePublicarStampERetornarClaim()
    {
        var id = Guid.NewGuid();
        var claimId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.MarcarPublicacoesExpiradasParaReconciliacaoAsync(
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repository.Setup(item => item.TryClaimPublicacaoDiscordAsync(
                id, DraftMontagemPublicacaoDiscordTipo.Presenca, It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DraftMontagemPublicacaoClaimResult(
                new DraftMontagemPublicacaoClaim(true, claimId, now.AddMinutes(5), DraftMontagemPublicacaoDiscordStatus.EmAndamento),
                Stamp(id)));
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        var handler = new AdquirirClaimPublicacaoDiscordDraftMontagemCommandHandler(
            repository.Object, new AdquirirClaimPublicacaoDiscordDraftMontagemValidator(), publisher.Object);

        var result = await handler.Handle(
            new AdquirirClaimPublicacaoDiscordDraftMontagemCommand(id, new AdquirirClaimPublicacaoDiscordDraftMontagemRequestDto("Presenca")),
            CancellationToken.None);

        result!.Adquirido.Should().BeTrue();
        publisher.Verify(item => item.PublishAfterCommitAsync(id, default), Times.Once);
    }

    [Fact]
    public async Task ClaimNegadoSemStampNaoDevePublicar()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.MarcarPublicacoesExpiradasParaReconciliacaoAsync(
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repository.Setup(item => item.TryClaimPublicacaoDiscordAsync(
                id, DraftMontagemPublicacaoDiscordTipo.Presenca, It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DraftMontagemPublicacaoClaimResult(
                new DraftMontagemPublicacaoClaim(false, null, null, DraftMontagemPublicacaoDiscordStatus.Publicada),
                null));
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        var handler = new AdquirirClaimPublicacaoDiscordDraftMontagemCommandHandler(
            repository.Object, new AdquirirClaimPublicacaoDiscordDraftMontagemValidator(), publisher.Object);

        await handler.Handle(
            new AdquirirClaimPublicacaoDiscordDraftMontagemCommand(id, new AdquirirClaimPublicacaoDiscordDraftMontagemRequestDto("Presenca")),
            CancellationToken.None);

        publisher.Verify(item => item.PublishAfterCommitAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task ExpiracaoConfirmadaDevePublicarMesmoSeClaimSeguinteFalhar()
    {
        var id = Guid.NewGuid();
        var expiredId = Guid.NewGuid();
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.MarcarPublicacoesExpiradasParaReconciliacaoAsync(
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([Stamp(expiredId)]);
        repository.Setup(item => item.TryClaimPublicacaoDiscordAsync(
                id, DraftMontagemPublicacaoDiscordTipo.Presenca, It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("persistencia do claim"));
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        var handler = new AdquirirClaimPublicacaoDiscordDraftMontagemCommandHandler(
            repository.Object, new AdquirirClaimPublicacaoDiscordDraftMontagemValidator(), publisher.Object);

        var act = () => handler.Handle(
            new AdquirirClaimPublicacaoDiscordDraftMontagemCommand(id, new AdquirirClaimPublicacaoDiscordDraftMontagemRequestDto("Presenca")),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        publisher.Verify(item => item.PublishAfterCommitAsync(
            expiredId,
            DraftMontagemSnapshotScope.IncludingArchived,
            DraftMontagemAvailabilityChange.None), Times.Once);
        publisher.Verify(item => item.PublishAfterCommitAsync(
            id,
            It.IsAny<DraftMontagemSnapshotScope>(),
            It.IsAny<DraftMontagemAvailabilityChange>()), Times.Never);
    }

    [Fact]
    public async Task ConclusaoSemStampDeveReconciliarENaoPublicarODraftRejeitado()
    {
        var id = Guid.NewGuid();
        var expiredId = Guid.NewGuid();
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.TryConcluirPublicacaoDiscordAsync(
                It.IsAny<Guid>(), It.IsAny<DraftMontagemPublicacaoDiscordTipo>(), It.IsAny<Guid>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DraftMontagemVersionStamp?)null);
        repository.Setup(item => item.MarcarPublicacoesExpiradasParaReconciliacaoAsync(
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([Stamp(expiredId)]);
        repository.Setup(item => item.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []));
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        var handler = new RegistrarPublicacaoDiscordDraftMontagemCommandHandler(
            repository.Object, new RegistrarPublicacaoDiscordDraftMontagemValidator(),
            Mock.Of<IDraftMontagemMetrics>(), publisher.Object);

        var act = () => handler.Handle(
            new RegistrarPublicacaoDiscordDraftMontagemCommand(
                id, new RegistrarPublicacaoDiscordDraftMontagemRequestDto("Presenca", Guid.NewGuid(), null, null, "message")),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        publisher.Verify(item => item.PublishAfterCommitAsync(
            expiredId,
            DraftMontagemSnapshotScope.IncludingArchived,
            DraftMontagemAvailabilityChange.None), Times.Once);
        publisher.Verify(item => item.PublishAfterCommitAsync(
            id,
            It.IsAny<DraftMontagemSnapshotScope>(),
            It.IsAny<DraftMontagemAvailabilityChange>()), Times.Never);
    }

    [Fact]
    public async Task RepublicacaoDevePublicarAposSaveENoOpNaoDeveSalvarNemPublicar()
    {
        var claimId = Guid.NewGuid();
        var montagem = CreateFailedMontagem(claimId);
        var id = montagem.Id;
        var saved = false;
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        repository.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => saved = true)
            .Returns(Task.CompletedTask);
        repository.Setup(item => item.ReloadByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        var publisher = PublisherAsserting(() => saved.Should().BeTrue());
        var handler = new RepublicarPublicacaoDiscordDraftMontagemCommandHandler(
            repository.Object, new RepublicarPublicacaoDiscordDraftMontagemValidator(),
            new TestCurrentUser(Guid.NewGuid()), Mock.Of<IDraftMontagemMetrics>(), publisher.Object);
        var command = new RepublicarPublicacaoDiscordDraftMontagemCommand(
            id, new RepublicarPublicacaoDiscordDraftMontagemRequestDto(DraftMontagemPublicacaoDiscordTipo.Presenca, "corrigido"));

        await handler.Handle(command, CancellationToken.None);
        await handler.Handle(command, CancellationToken.None);

        repository.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        publisher.Verify(item => item.PublishAfterCommitAsync(id, default), Times.Once);
    }

    [Theory]
    [InlineData(typeof(AdquirirClaimPublicacaoDiscordDraftMontagemCommandHandler))]
    [InlineData(typeof(RegistrarPublicacaoDiscordDraftMontagemCommandHandler))]
    [InlineData(typeof(RegistrarFalhaPublicacaoDiscordDraftMontagemCommandHandler))]
    [InlineData(typeof(RepublicarPublicacaoDiscordDraftMontagemCommandHandler))]
    public void FluxoDePublicacaoDeveDependerDoPublisher(Type serviceType)
    {
        serviceType.GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Should().Contain(parameter => parameter.ParameterType == typeof(IDraftMontagemRealtimePublisher));
    }

    [Fact]
    public void ExpiracaoDeveRetornarStampsDosDraftsAlterados()
    {
        typeof(IDraftMontagemRepository)
            .GetMethod(nameof(IDraftMontagemRepository.MarcarPublicacoesExpiradasParaReconciliacaoAsync))!
            .ReturnType.Should().Be(typeof(Task<IReadOnlyCollection<DraftMontagemVersionStamp>>));
    }

    [Fact]
    public void EstadoRealtimeNaoDeveSerializarDadosAdministrativosOuOperacionais()
    {
        var montagem = CreateFailedMontagem(Guid.NewGuid());
        var json = JsonSerializer.Serialize(DraftMontagemResponseDto.FromEntity(montagem), new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.Should().NotContain("acoesAdministrativas");
        json.Should().NotContain("discordGuildId");
        json.Should().NotContain("discordPresenceMessageId");
        json.Should().NotContain("guildId");
        json.Should().NotContain("channelId");
        json.Should().NotContain("messageId");
        json.Should().NotContain("ultimoErroCodigo");
        json.Should().NotContain("claimId");
    }

    [Fact]
    public void PublicacaoPublicaDeveSerializarSomenteTipoEStatus()
    {
        var montagem = CreatePublishedMontagem(Guid.NewGuid());
        var json = JsonSerializer.Serialize(DraftMontagemResponseDto.FromEntity(montagem), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);

        document.RootElement.GetProperty("publicacoesDiscord")[0]
            .EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo(["tipo", "status"]);
    }

    private static DraftMontagemVersionStamp Stamp(Guid id) => new(id, 7, DateTimeOffset.UtcNow);

    private static DraftMontagem CreateArchivedCancellationMontagem(Guid? userId = null)
    {
        var montagem = new DraftMontagem("Rinha arquivada", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        montagem.Arquivar("motivo", userId ?? Guid.NewGuid(), DateTimeOffset.UtcNow);
        return montagem;
    }

    private static Mock<IDraftMontagemRealtimePublisher> PublisherAsserting(Action assertion)
    {
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        publisher.Setup(item => item.PublishAfterCommitAsync(It.IsAny<Guid>(), default))
            .Callback(assertion)
            .Returns(Task.CompletedTask);
        return publisher;
    }

    private static DraftMontagem CreatePublishedMontagem(Guid claimId)
    {
        var montagem = CreateClaimedMontagem(claimId, out var now);
        montagem.RegistrarPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, claimId, "guild", "channel", "message", now.AddMinutes(1));
        return montagem;
    }

    private static DraftMontagem CreateFailedMontagem(Guid claimId)
    {
        var montagem = CreateClaimedMontagem(claimId, out var now);
        montagem.RegistrarFalhaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, claimId, "guild", "channel", "erro", now.AddMinutes(1));
        return montagem;
    }

    private static DraftMontagem CreateClaimedMontagem(Guid claimId, out DateTimeOffset now)
    {
        now = DateTimeOffset.UtcNow;
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        montagem.ConfigurarEncerramentoPresenca(now.AddHours(1));
        montagem.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, "guild", "channel", claimId, now.AddMinutes(5), now);
        return montagem;
    }

    private sealed record TestCurrentUser(Guid? UserId) : ICurrentUser
    {
        public IReadOnlyCollection<string> Roles => [];
        public string? IpAddress => null;
        public string? UserAgent => null;
    }
}
