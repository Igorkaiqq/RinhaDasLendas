using System.Text.Json;
using FluentAssertions;
using Moq;
using RinhaDasLendas.Api.Services;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.DraftMontagens;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Models;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Tests.Jogadores;

namespace RinhaDasLendas.Tests.Application;

public sealed class DraftMontagemPublicationRealtimeTests
{
    [Fact]
    public async Task CancelamentoAdministrativoDeveContinuarNotificandoUmaUnicaVez()
    {
        var id = Guid.NewGuid();
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        repository.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repository.Setup(item => item.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        repository.Setup(item => item.GetJogadorByUsuarioIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Jogador?)null);
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        var handler = new CancelarDraftMontagemCommandHandler(
            repository.Object,
            new CancelarDraftMontagemValidator(),
            new TestCurrentUser(Guid.NewGuid()),
            notifier.Object);

        await handler.Handle(
            new CancelarDraftMontagemCommand(id, new CancelarDraftMontagemRequestDto("motivo administrativo")),
            CancellationToken.None);

        notifier.Verify(item => item.StateUpdatedAsync(id, It.IsAny<DraftMontagemRealtimeStateDto>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task PresencaManualAdministrativaDeveContinuarNotificandoUmaUnicaVez()
    {
        var id = Guid.NewGuid();
        var jogador = JogadorTestData.JogadorAtivo("Jogador");
        jogador.VincularUsuario(Guid.NewGuid());
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        repository.Setup(item => item.GetJogadoresByIdsAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(jogador.Id)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([jogador]);
        repository.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repository.Setup(item => item.GetJogadorByUsuarioIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Jogador?)null);
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        var handler = new AdicionarPresencaManualDraftMontagemCommandHandler(
            repository.Object,
            new AdicionarPresencaManualDraftMontagemValidator(),
            new TestCurrentUser(Guid.NewGuid()),
            notifier.Object,
            Mock.Of<IDraftMontagemMetrics>());

        await handler.Handle(
            new AdicionarPresencaManualDraftMontagemCommand(
                id,
                new AdicionarPresencaManualDraftMontagemRequestDto(jogador.Id, "inclusao administrativa")),
            CancellationToken.None);

        notifier.Verify(item => item.StateUpdatedAsync(id, It.IsAny<DraftMontagemRealtimeStateDto>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ConclusaoDeveNotificarUmaVezAposPersistenciaERecarga()
    {
        var id = Guid.NewGuid();
        var claimId = Guid.NewGuid();
        var persisted = false;
        var reloaded = false;
        var montagem = CreatePublishedMontagem(claimId);
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.TryConcluirPublicacaoDiscordAsync(
                id,
                DraftMontagemPublicacaoDiscordTipo.Presenca,
                claimId,
                "guild",
                "channel",
                "message",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => persisted = true)
            .ReturnsAsync(true);
        repository.Setup(item => item.ReloadByIdAsync(id, It.IsAny<CancellationToken>()))
            .Callback(() => reloaded = true)
            .ReturnsAsync(montagem);
        var notifier = CreateNotifier(() => persisted.Should().BeTrue(), () => reloaded.Should().BeTrue());
        var handler = new RegistrarPublicacaoDiscordDraftMontagemCommandHandler(
            repository.Object,
            new RegistrarPublicacaoDiscordDraftMontagemValidator(),
            Mock.Of<IDraftMontagemMetrics>(),
            notifier.Object);

        await handler.Handle(
            new RegistrarPublicacaoDiscordDraftMontagemCommand(
                id,
                new RegistrarPublicacaoDiscordDraftMontagemRequestDto("Presenca", claimId, "guild", "channel", "message")),
            CancellationToken.None);

        notifier.Verify(item => item.StateUpdatedAsync(id, It.IsAny<DraftMontagemRealtimeStateDto>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task FalhaDeveNotificarUmaVezAposPersistenciaERecarga()
    {
        var id = Guid.NewGuid();
        var claimId = Guid.NewGuid();
        var persisted = false;
        var reloaded = false;
        var montagem = CreateFailedMontagem(claimId);
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.TryRegistrarFalhaPublicacaoDiscordAsync(
                id,
                DraftMontagemPublicacaoDiscordTipo.Presenca,
                claimId,
                "guild",
                "channel",
                "erro",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => persisted = true)
            .ReturnsAsync(true);
        repository.Setup(item => item.ReloadByIdAsync(id, It.IsAny<CancellationToken>()))
            .Callback(() => reloaded = true)
            .ReturnsAsync(montagem);
        var notifier = CreateNotifier(() => persisted.Should().BeTrue(), () => reloaded.Should().BeTrue());
        var handler = new RegistrarFalhaPublicacaoDiscordDraftMontagemCommandHandler(
            repository.Object,
            new RegistrarFalhaPublicacaoDiscordDraftMontagemValidator(),
            Mock.Of<IDraftMontagemMetrics>(),
            notifier.Object);

        await handler.Handle(
            new RegistrarFalhaPublicacaoDiscordDraftMontagemCommand(
                id,
                new RegistrarFalhaPublicacaoDiscordDraftMontagemRequestDto("Presenca", claimId, "guild", "channel", "erro")),
            CancellationToken.None);

        notifier.Verify(item => item.StateUpdatedAsync(id, It.IsAny<DraftMontagemRealtimeStateDto>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task RepublicacaoDeveNotificarUmaVezAposPersistenciaERecarga()
    {
        var id = Guid.NewGuid();
        var claimId = Guid.NewGuid();
        var montagem = CreateFailedMontagem(claimId);
        var persisted = false;
        var reloaded = false;
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        repository.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => persisted = true)
            .Returns(Task.CompletedTask);
        repository.Setup(item => item.ReloadByIdAsync(id, It.IsAny<CancellationToken>()))
            .Callback(() => reloaded = true)
            .ReturnsAsync(montagem);
        repository.Setup(item => item.GetJogadorByUsuarioIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Jogador?)null);
        var notifier = CreateNotifier(() => persisted.Should().BeTrue(), () => reloaded.Should().BeTrue());
        var handler = new RepublicarPublicacaoDiscordDraftMontagemCommandHandler(
            repository.Object,
            new RepublicarPublicacaoDiscordDraftMontagemValidator(),
            new TestCurrentUser(Guid.NewGuid()),
            Mock.Of<IDraftMontagemMetrics>(),
            notifier.Object);

        await handler.Handle(
            new RepublicarPublicacaoDiscordDraftMontagemCommand(
                id,
                new RepublicarPublicacaoDiscordDraftMontagemRequestDto(DraftMontagemPublicacaoDiscordTipo.Presenca, "corrigido")),
            CancellationToken.None);

        notifier.Verify(item => item.StateUpdatedAsync(id, It.IsAny<DraftMontagemRealtimeStateDto>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ClaimAdquiridoDeveNotificarUmaVezAposRecarga()
    {
        var id = Guid.NewGuid();
        var claimId = Guid.NewGuid();
        var agora = DateTimeOffset.UtcNow;
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        montagem.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, null, null, claimId, agora.AddMinutes(5), agora);
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.MarcarPublicacoesExpiradasParaReconciliacaoAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repository.Setup(item => item.TryClaimPublicacaoDiscordAsync(
                id,
                DraftMontagemPublicacaoDiscordTipo.Presenca,
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DraftMontagemPublicacaoClaim(true, claimId, agora.AddMinutes(5), DraftMontagemPublicacaoDiscordStatus.EmAndamento));
        repository.Setup(item => item.ReloadByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        var handler = new AdquirirClaimPublicacaoDiscordDraftMontagemCommandHandler(
            repository.Object,
            new AdquirirClaimPublicacaoDiscordDraftMontagemValidator(),
            notifier.Object);

        await handler.Handle(
            new AdquirirClaimPublicacaoDiscordDraftMontagemCommand(id, new AdquirirClaimPublicacaoDiscordDraftMontagemRequestDto("Presenca")),
            CancellationToken.None);

        notifier.Verify(item => item.StateUpdatedAsync(id, It.IsAny<DraftMontagemRealtimeStateDto>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ClaimNegadoNaoDeveNotificar()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.MarcarPublicacoesExpiradasParaReconciliacaoAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repository.Setup(item => item.TryClaimPublicacaoDiscordAsync(
                id,
                DraftMontagemPublicacaoDiscordTipo.Presenca,
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DraftMontagemPublicacaoClaim(false, null, null, DraftMontagemPublicacaoDiscordStatus.Publicada));
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        var handler = new AdquirirClaimPublicacaoDiscordDraftMontagemCommandHandler(
            repository.Object,
            new AdquirirClaimPublicacaoDiscordDraftMontagemValidator(),
            notifier.Object);

        await handler.Handle(
            new AdquirirClaimPublicacaoDiscordDraftMontagemCommand(id, new AdquirirClaimPublicacaoDiscordDraftMontagemRequestDto("Presenca")),
            CancellationToken.None);

        notifier.Verify(item => item.StateUpdatedAsync(It.IsAny<Guid>(), It.IsAny<DraftMontagemRealtimeStateDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExpiracaoConfirmadaDeveNotificarMesmoSeClaimSeguinteFalhar()
    {
        var id = Guid.NewGuid();
        var expiradoId = Guid.NewGuid();
        var montagemExpirada = new DraftMontagem("Expirada", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.MarcarPublicacoesExpiradasParaReconciliacaoAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([expiradoId]);
        repository.Setup(item => item.ReloadByIdAsync(expiradoId, It.IsAny<CancellationToken>())).ReturnsAsync(montagemExpirada);
        repository.Setup(item => item.TryClaimPublicacaoDiscordAsync(
                id,
                DraftMontagemPublicacaoDiscordTipo.Presenca,
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("persistencia do claim"));
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        var handler = new AdquirirClaimPublicacaoDiscordDraftMontagemCommandHandler(
            repository.Object,
            new AdquirirClaimPublicacaoDiscordDraftMontagemValidator(),
            notifier.Object);

        var act = () => handler.Handle(
            new AdquirirClaimPublicacaoDiscordDraftMontagemCommand(id, new AdquirirClaimPublicacaoDiscordDraftMontagemRequestDto("Presenca")),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        notifier.Verify(item => item.StateUpdatedAsync(expiradoId, It.IsAny<DraftMontagemRealtimeStateDto>(), CancellationToken.None), Times.Once);
        notifier.Verify(item => item.StateUpdatedAsync(id, It.IsAny<DraftMontagemRealtimeStateDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PersistenciaComErroNaoDeveNotificar()
    {
        var id = Guid.NewGuid();
        var claimId = Guid.NewGuid();
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.TryConcluirPublicacaoDiscordAsync(
                It.IsAny<Guid>(),
                It.IsAny<DraftMontagemPublicacaoDiscordTipo>(),
                It.IsAny<Guid>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("persistencia"));
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        var handler = new RegistrarPublicacaoDiscordDraftMontagemCommandHandler(
            repository.Object,
            new RegistrarPublicacaoDiscordDraftMontagemValidator(),
            Mock.Of<IDraftMontagemMetrics>(),
            notifier.Object);

        var act = () => handler.Handle(
            new RegistrarPublicacaoDiscordDraftMontagemCommand(
                id,
                new RegistrarPublicacaoDiscordDraftMontagemRequestDto("Presenca", claimId, null, null, "message")),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        notifier.Verify(item => item.StateUpdatedAsync(It.IsAny<Guid>(), It.IsAny<DraftMontagemRealtimeStateDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConclusaoSemTransicaoNaoDeveNotificar()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.TryConcluirPublicacaoDiscordAsync(
                It.IsAny<Guid>(),
                It.IsAny<DraftMontagemPublicacaoDiscordTipo>(),
                It.IsAny<Guid>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(item => item.MarcarPublicacoesExpiradasParaReconciliacaoAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repository.Setup(item => item.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []));
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        var handler = new RegistrarPublicacaoDiscordDraftMontagemCommandHandler(
            repository.Object,
            new RegistrarPublicacaoDiscordDraftMontagemValidator(),
            Mock.Of<IDraftMontagemMetrics>(),
            notifier.Object);

        var act = () => handler.Handle(
            new RegistrarPublicacaoDiscordDraftMontagemCommand(
                id,
                new RegistrarPublicacaoDiscordDraftMontagemRequestDto("Presenca", Guid.NewGuid(), null, null, "message")),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        notifier.Verify(item => item.StateUpdatedAsync(It.IsAny<Guid>(), It.IsAny<DraftMontagemRealtimeStateDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FalhaSemTransicaoNaoDeveNotificar()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.TryRegistrarFalhaPublicacaoDiscordAsync(
                It.IsAny<Guid>(),
                It.IsAny<DraftMontagemPublicacaoDiscordTipo>(),
                It.IsAny<Guid>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(item => item.MarcarPublicacoesExpiradasParaReconciliacaoAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repository.Setup(item => item.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []));
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        var handler = new RegistrarFalhaPublicacaoDiscordDraftMontagemCommandHandler(
            repository.Object,
            new RegistrarFalhaPublicacaoDiscordDraftMontagemValidator(),
            Mock.Of<IDraftMontagemMetrics>(),
            notifier.Object);

        var act = () => handler.Handle(
            new RegistrarFalhaPublicacaoDiscordDraftMontagemCommand(
                id,
                new RegistrarFalhaPublicacaoDiscordDraftMontagemRequestDto("Presenca", Guid.NewGuid(), null, null, "erro")),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        notifier.Verify(item => item.StateUpdatedAsync(It.IsAny<Guid>(), It.IsAny<DraftMontagemRealtimeStateDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RepublicacaoComFalhaDePersistenciaNaoDeveNotificar()
    {
        var id = Guid.NewGuid();
        var montagem = CreateFailedMontagem(Guid.NewGuid());
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        repository.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("persistencia"));
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        var handler = new RepublicarPublicacaoDiscordDraftMontagemCommandHandler(
            repository.Object,
            new RepublicarPublicacaoDiscordDraftMontagemValidator(),
            new TestCurrentUser(Guid.NewGuid()),
            Mock.Of<IDraftMontagemMetrics>(),
            notifier.Object);

        var act = () => handler.Handle(
            new RepublicarPublicacaoDiscordDraftMontagemCommand(
                id,
                new RepublicarPublicacaoDiscordDraftMontagemRequestDto(DraftMontagemPublicacaoDiscordTipo.Presenca, "corrigido")),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        notifier.Verify(item => item.StateUpdatedAsync(It.IsAny<Guid>(), It.IsAny<DraftMontagemRealtimeStateDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(typeof(AdquirirClaimPublicacaoDiscordDraftMontagemCommandHandler))]
    [InlineData(typeof(RegistrarPublicacaoDiscordDraftMontagemCommandHandler))]
    [InlineData(typeof(RegistrarFalhaPublicacaoDiscordDraftMontagemCommandHandler))]
    [InlineData(typeof(RepublicarPublicacaoDiscordDraftMontagemCommandHandler))]
    public void FluxoDePublicacaoDeveDependerDoNotifier(Type serviceType)
    {
        serviceType.GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Should().Contain(parameter => parameter.ParameterType == typeof(IDraftMontagemRealtimeNotifier));
    }

    [Fact]
    public void ExpiracaoDeveRetornarIdsDosDraftsAlterados()
    {
        typeof(IDraftMontagemRepository)
            .GetMethod(nameof(IDraftMontagemRepository.MarcarPublicacoesExpiradasParaReconciliacaoAsync))!
            .ReturnType.Should().Be(typeof(Task<IReadOnlyCollection<Guid>>));
    }

    [Fact]
    public void EstadoRealtimeNaoDeveSerializarDadosAdministrativosOuOperacionais()
    {
        var montagem = new DraftMontagem(
            "Rinha",
            "observacao interna",
            5,
            DraftMontagemCriterioCapitaes.Manual,
            [],
            []);
        var claimId = Guid.NewGuid();
        var agora = DateTimeOffset.UtcNow;
        montagem.IniciarTentativaPublicacaoDiscord(
            DraftMontagemPublicacaoDiscordTipo.Presenca,
            "guild-secreta",
            "canal-secreto",
            claimId,
            agora.AddMinutes(5),
            agora);
        montagem.RegistrarFalhaPublicacaoDiscord(
            DraftMontagemPublicacaoDiscordTipo.Presenca,
            claimId,
            "guild-secreta",
            "canal-secreto",
            "erro-secreto",
            agora.AddMinutes(1));
        montagem.SolicitarRepublicacaoDiscord(
            DraftMontagemPublicacaoDiscordTipo.Presenca,
            Guid.NewGuid(),
            "motivo secreto",
            agora.AddMinutes(2));

        var state = new DraftMontagemRealtimeStateDto(
            DraftMontagemResponseDto.FromEntity(montagem),
            agora,
            false);
        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.Should().NotContain("acoesAdministrativas");
        json.Should().NotContain("motivoCancelamento");
        json.Should().NotContain("discordGuildId");
        json.Should().NotContain("discordPresenceMessageId");
        json.Should().NotContain("discordUserId");
        json.Should().NotContain("guildId");
        json.Should().NotContain("channelId");
        json.Should().NotContain("messageId");
        json.Should().NotContain("ultimoErroCodigo");
        json.Should().NotContain("claimId");
        json.Should().NotContain("responsavelUsuarioId");
        json.Should().NotContain("motivo secreto");
        json.Should().NotContain("erro-secreto");
    }

    [Fact]
    public void PublicacaoPublicaDeveSerializarSomenteTipoEStatus()
    {
        var montagem = CreatePublishedMontagem(Guid.NewGuid());

        var json = JsonSerializer.Serialize(DraftMontagemResponseDto.FromEntity(montagem), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);
        var publicationProperties = document.RootElement
            .GetProperty("publicacoesDiscord")[0]
            .EnumerateObject()
            .Select(property => property.Name)
            .ToList();

        publicationProperties.Should().BeEquivalentTo(["tipo", "status"]);
    }

    private static Mock<IDraftMontagemRealtimeNotifier> CreateNotifier(params Action[] assertions)
    {
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        notifier.Setup(item => item.StateUpdatedAsync(
                It.IsAny<Guid>(),
                It.IsAny<DraftMontagemRealtimeStateDto>(),
                It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                foreach (var assertion in assertions)
                {
                    assertion();
                }
            })
            .Returns(Task.CompletedTask);
        return notifier;
    }

    private static DraftMontagem CreatePublishedMontagem(Guid claimId)
    {
        var montagem = CreateClaimedMontagem(claimId, out var agora);
        montagem.RegistrarPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, claimId, "guild", "channel", "message", agora.AddMinutes(1));
        return montagem;
    }

    private static DraftMontagem CreateFailedMontagem(Guid claimId)
    {
        var montagem = CreateClaimedMontagem(claimId, out var agora);
        montagem.RegistrarFalhaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, claimId, "guild", "channel", "erro", agora.AddMinutes(1));
        return montagem;
    }

    private static DraftMontagem CreateClaimedMontagem(Guid claimId, out DateTimeOffset agora)
    {
        agora = DateTimeOffset.UtcNow;
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        montagem.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, "guild", "channel", claimId, agora.AddMinutes(5), agora);
        return montagem;
    }

    private sealed record TestCurrentUser(Guid? UserId) : ICurrentUser
    {
        public IReadOnlyCollection<string> Roles => [];
        public string? IpAddress => null;
        public string? UserAgent => null;
    }
}
