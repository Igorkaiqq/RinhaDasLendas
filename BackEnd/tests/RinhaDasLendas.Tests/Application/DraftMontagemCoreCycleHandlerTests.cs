using FluentAssertions;
using FluentValidation;
using Moq;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.DraftMontagens;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.DraftMontagens;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Tests.Jogadores;

namespace RinhaDasLendas.Tests.Application;

public sealed class DraftMontagemCoreCycleHandlerTests
{
    [Theory]
    [InlineData("Manual")]
    [InlineData("manual")]
    [InlineData("TempoReal")]
    [InlineData("temporeal")]
    public void ValidatorDeveAceitarModosNominais(string modo)
    {
        var result = new SelecionarModoDraftMontagemValidator().Validate(
            new SelecionarModoDraftMontagemRequestDto(modo));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Invalido")]
    [InlineData("1")]
    public void ValidatorDeveRejeitarModoAusenteInvalidoOuNumerico(string? modo)
    {
        var result = new SelecionarModoDraftMontagemValidator().Validate(
            new SelecionarModoDraftMontagemRequestDto(modo!));

        result.Errors.Should().Contain(error => error.ErrorMessage == MessageCodes.FieldRequired);
    }

    [Fact]
    public async Task SelecionarModoDeveValidarJogadoresAtivosPersistirUmaVezERetornarEstadoAtualizado()
    {
        var jogadores = CriarJogadores(10);
        var montagem = CriarPresencaEncerrada(jogadores);
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.GetByIdAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        repository.Setup(item => item.GetJogadoresByIdsAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == jogadores.Count && jogadores.All(jogador => ids.Contains(jogador.Id))),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(jogadores);
        var handler = new SelecionarModoDraftMontagemCommandHandler(
            repository.Object,
            new SelecionarModoDraftMontagemValidator());

        var result = await handler.Handle(
            new SelecionarModoDraftMontagemCommand(
                montagem.Id,
                new SelecionarModoDraftMontagemRequestDto("Manual")),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Modo.Should().Be(DraftMontagemModo.Manual.ToString());
        result.Status.Should().Be(DraftMontagemStatus.Aberta.ToString());
        montagem.Times.Should().OnlyContain(time => time.CapitaoId == null);
        repository.Verify(item => item.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SelecionarModoQuandoDraftNaoExisteDeveRetornarNullSemConsultarJogadoresOuPersistir()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((DraftMontagem?)null);
        var handler = new SelecionarModoDraftMontagemCommandHandler(
            repository.Object,
            new SelecionarModoDraftMontagemValidator());

        var result = await handler.Handle(
            new SelecionarModoDraftMontagemCommand(id, new SelecionarModoDraftMontagemRequestDto("Manual")),
            CancellationToken.None);

        result.Should().BeNull();
        repository.Verify(item => item.GetJogadoresByIdsAsync(
            It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RepetirMesmoModoDeveRetornarProjecaoSemConsultarJogadoresOuPersistir()
    {
        var jogadores = CriarJogadores(10);
        var montagem = CriarPresencaEncerrada(jogadores);
        montagem.SelecionarModo(
            DraftMontagemModo.Manual,
            jogadores.Select(item => item.Id).ToHashSet());
        jogadores.First().Inativar();
        var versao = montagem.VersaoEstado;
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.GetByIdAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        var handler = new SelecionarModoDraftMontagemCommandHandler(
            repository.Object,
            new SelecionarModoDraftMontagemValidator());

        var result = await handler.Handle(
            new SelecionarModoDraftMontagemCommand(
                montagem.Id,
                new SelecionarModoDraftMontagemRequestDto("Manual")),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Modo.Should().Be(DraftMontagemModo.Manual.ToString());
        montagem.VersaoEstado.Should().Be(versao);
        repository.Verify(item => item.GetJogadoresByIdsAsync(
            It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RepetirMesmoModoEmDraftLegadoDeveChegarAoDominioESerRecusado()
    {
        var jogadores = CriarJogadores(10);
        var jogadoresIds = jogadores.Select(item => item.Id).ToList();
        var montagem = new DraftMontagem(
            "Rinha",
            null,
            5,
            DraftMontagemCriterioCapitaes.Manual,
            jogadoresIds,
            jogadoresIds.Take(2).ToList());
        typeof(DraftMontagem)
            .GetProperty(nameof(DraftMontagem.CicloVersao))!
            .SetValue(montagem, DraftMontagemCicloVersao.Legado);
        var versao = montagem.VersaoEstado;
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.GetByIdAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        repository.Setup(item => item.GetJogadoresByIdsAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 0),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var handler = new SelecionarModoDraftMontagemCommandHandler(
            repository.Object,
            new SelecionarModoDraftMontagemValidator());

        var act = () => handler.Handle(
            new SelecionarModoDraftMontagemCommand(
                montagem.Id,
                new SelecionarModoDraftMontagemRequestDto("Manual")),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage(MessageCodes.DraftClosed);
        montagem.VersaoEstado.Should().Be(versao);
        repository.Verify(item => item.GetJogadoresByIdsAsync(
            It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 0),
            CancellationToken.None), Times.Once);
        repository.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CriacaoWebDeveUsarFabricaV2(bool criacaoDireta)
    {
        var jogadores = criacaoDireta ? CriarJogadores(10) : [];
        var jogadoresIds = jogadores.Select(item => item.Id).ToList();
        var request = new CreateDraftMontagemRequestDto(
            "Rinha",
            null,
            5,
            false,
            null,
            null,
            [],
            jogadoresIds);
        DraftMontagem? adicionada = null;
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.GetJogadoresByIdsAsync(jogadoresIds, It.IsAny<CancellationToken>())).ReturnsAsync(jogadores);
        repository.Setup(item => item.AddAsync(It.IsAny<DraftMontagem>(), It.IsAny<CancellationToken>()))
            .Callback<DraftMontagem, CancellationToken>((montagem, _) => adicionada = montagem)
            .Returns(Task.CompletedTask);
        repository.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var currentUser = new Mock<ICurrentUser>();
        var handler = new CreateDraftMontagemCommandHandler(repository.Object, new CreateDraftMontagemValidator(), currentUser.Object);

        await handler.Handle(new CreateDraftMontagemCommand(request), CancellationToken.None);

        adicionada.Should().NotBeNull();
        adicionada!.CicloVersao.Should().Be(DraftMontagemCicloVersao.ModoPosPresenca);
        if (criacaoDireta)
        {
            adicionada.Status.Should().Be(DraftMontagemStatus.Aberta);
            adicionada.Modo.Should().Be(DraftMontagemModo.Manual);
            adicionada.Times.Should().OnlyContain(time => time.CapitaoId == null);
        }
        else
        {
            adicionada.Status.Should().Be(DraftMontagemStatus.PresencaAberta);
            adicionada.Modo.Should().BeNull();
            adicionada.Times.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task BotNaoDeveCriarMontagemDiretaComJogadores()
    {
        var jogadorId = Guid.NewGuid();
        var request = new CreateDraftMontagemRequestDto(
            "Rinha",
            null,
            5,
            false,
            null,
            null,
            [],
            [jogadorId]);
        var repository = new Mock<IDraftMontagemRepository>();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(item => item.IsBot).Returns(true);
        var handler = new CreateDraftMontagemCommandHandler(
            repository.Object,
            new CreateDraftMontagemValidator(),
            currentUser.Object);

        var act = () => handler.Handle(new CreateDraftMontagemCommand(request), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage(MessageCodes.DraftMontagemBotCanOnlyCreatePresence);
        repository.Verify(item => item.GetJogadoresByIdsAsync(
            It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(item => item.AddAsync(
            It.IsAny<DraftMontagem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DefinirCapitaesV2DeveConsultarElegibilidadeAtualAntesDeMutar()
    {
        var jogadores = CriarJogadores(10);
        var montagem = CriarPresencaEncerrada(jogadores);
        montagem.SelecionarModo(DraftMontagemModo.TempoReal, jogadores.Select(jogador => jogador.Id).ToHashSet());
        var capitaesIds = jogadores.Take(2).Select(jogador => jogador.Id).ToList();
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.GetByIdAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        repository.Setup(item => item.GetCapitaesElegiveisIdsAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == jogadores.Count),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(capitaesIds);
        var handler = new DefinirCapitaesDraftMontagemCommandHandler(
            repository.Object,
            new DefinirCapitaesDraftMontagemValidator());

        await handler.Handle(
            new DefinirCapitaesDraftMontagemCommand(
                montagem.Id,
                new DefinirCapitaesDraftMontagemRequestDto(capitaesIds)),
            CancellationToken.None);

        montagem.Status.Should().Be(DraftMontagemStatus.CapitaesDefinidos);
        repository.Verify(item => item.GetCapitaesElegiveisIdsAsync(
            It.IsAny<IReadOnlyCollection<Guid>>(), CancellationToken.None), Times.Once);
        repository.Verify(item => item.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ConsultaAdminDeveExcluirReservaAtivaComRoleCapitaoDaProjecaoDeElegiveis()
    {
        var jogadores = CriarJogadores(12);
        var montagem = CriarPresencaEncerrada(jogadores);
        montagem.SelecionarModo(DraftMontagemModo.TempoReal, jogadores.Select(jogador => jogador.Id).ToHashSet());
        var titularesElegiveisIds = jogadores.Take(2).Select(jogador => jogador.Id).ToList();
        var reservaElegivelId = jogadores.Last().Id;
        var elegiveisGlobaisIds = titularesElegiveisIds.Append(reservaElegivelId).ToList();
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.GetByIdAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        repository.Setup(item => item.GetCapitaesElegiveisIdsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(elegiveisGlobaisIds);
        var handler = new GetDraftMontagemAdminQueryHandler(repository.Object);

        var result = await handler.Handle(new GetDraftMontagemAdminQuery(montagem.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.CapitaesElegiveisIds.Should().BeEquivalentTo(titularesElegiveisIds);
        result.CapitaesElegiveisIds.Should().NotContain(reservaElegivelId);
        repository.Verify(item => item.GetCapitaesElegiveisIdsAsync(
            It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 10 && !ids.Contains(reservaElegivelId)),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task IniciarV2DeveRevalidarCapitaesAntesDeCriarPrimeiroTurno()
    {
        var jogadores = CriarJogadores(10);
        var montagem = CriarTempoRealComOrdemDefinida(jogadores);
        var capitaesIds = montagem.Times.Select(time => time.CapitaoId!.Value).ToList();
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.GetByIdAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        repository.Setup(item => item.GetCapitaesElegiveisIdsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(capitaesIds);
        var currentUser = new Mock<ICurrentUser>();
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        notifier.Setup(item => item.StateUpdatedAsync(montagem.Id, It.IsAny<DraftMontagemRealtimeStateDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new IniciarDraftMontagemTempoRealCommandHandler(repository.Object, currentUser.Object, notifier.Object);

        await handler.Handle(new IniciarDraftMontagemTempoRealCommand(montagem.Id), CancellationToken.None);

        montagem.Status.Should().Be(DraftMontagemStatus.Aberta);
        montagem.TurnoSequencia.Should().Be(1);
        repository.Verify(item => item.GetCapitaesElegiveisIdsAsync(
            It.IsAny<IReadOnlyCollection<Guid>>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task PickV2DeveRevalidarCapitaoAntesDeRegistrarEscolha()
    {
        var jogadores = CriarJogadores(10).ToList();
        var montagem = CriarTempoRealComOrdemDefinida(jogadores);
        var capitaesIds = montagem.Times.Select(time => time.CapitaoId!.Value).ToList();
        montagem.IniciarTempoReal(DateTimeOffset.UtcNow, capitaesIds.ToHashSet());
        var capitao = jogadores.Single(jogador => jogador.Id == capitaesIds[0]);
        var usuarioId = Guid.NewGuid();
        capitao.VincularUsuario(usuarioId);
        var jogadorEscolhidoId = jogadores.First(jogador => !capitaesIds.Contains(jogador.Id)).Id;
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.GetByIdAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        repository.Setup(item => item.GetJogadorByUsuarioIdAsync(usuarioId, It.IsAny<CancellationToken>())).ReturnsAsync(capitao);
        repository.Setup(item => item.GetCapitaesElegiveisIdsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(capitaesIds);
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(item => item.UserId).Returns(usuarioId);
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        notifier.Setup(item => item.StateUpdatedAsync(montagem.Id, It.IsAny<DraftMontagemRealtimeStateDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var metrics = new Mock<IDraftMontagemMetrics>();
        var handler = new RegistrarPickDraftMontagemCommandHandler(
            repository.Object,
            currentUser.Object,
            new RegistrarPickDraftMontagemValidator(),
            notifier.Object,
            metrics.Object);

        await handler.Handle(
            new RegistrarPickDraftMontagemCommand(
                montagem.Id,
                new RegistrarPickDraftMontagemRequestDto(jogadorEscolhidoId)),
            CancellationToken.None);

        montagem.Escolhas.Should().ContainSingle(escolha => escolha.JogadorId == jogadorEscolhidoId);
        repository.Verify(item => item.GetCapitaesElegiveisIdsAsync(
            It.IsAny<IReadOnlyCollection<Guid>>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public void ValidatorDeSubstituicaoDeveRejeitarNovoCapitaoVazioQuandoInformado()
    {
        var request = new SubstituirReservaDraftMontagemRequestDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.Empty,
            null);

        var result = new SubstituirReservaDraftMontagemValidator().Validate(request);

        result.Errors.Should().Contain(error => error.ErrorMessage == MessageCodes.DraftMontagemCaptainsRequired);
    }

    [Fact]
    public async Task SubstituirCapitaoV2DeveConsultarElegibilidadeEAtualizarAutoridadeDoTurno()
    {
        var jogadores = CriarJogadores(5).ToList();
        var montagem = CriarTempoRealIniciadoComReserva(jogadores);
        var time = montagem.Times.Single(item => item.CapitaoId == montagem.TurnoAtualCapitaoId);
        var capitaoSaiuId = time.CapitaoId!.Value;
        var reservaEntrouId = jogadores[^1].Id;
        var usuarioId = Guid.NewGuid();
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.GetByIdAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        repository.Setup(item => item.GetCapitaesElegiveisIdsAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == jogadores.Count),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([reservaEntrouId]);
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(item => item.UserId).Returns(usuarioId);
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        notifier.Setup(item => item.StateUpdatedAsync(montagem.Id, It.IsAny<DraftMontagemRealtimeStateDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new SubstituirReservaDraftMontagemCommandHandler(
            repository.Object,
            currentUser.Object,
            new SubstituirReservaDraftMontagemValidator(),
            notifier.Object);

        await handler.Handle(
            new SubstituirReservaDraftMontagemCommand(
                montagem.Id,
                new SubstituirReservaDraftMontagemRequestDto(
                    time.Id,
                    capitaoSaiuId,
                    reservaEntrouId,
                    reservaEntrouId,
                    null)),
            CancellationToken.None);

        montagem.TurnoAtualCapitaoId.Should().Be(reservaEntrouId);
        repository.Verify(item => item.GetCapitaesElegiveisIdsAsync(
            It.IsAny<IReadOnlyCollection<Guid>>(), CancellationToken.None), Times.Once);
        repository.Verify(item => item.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    private static DraftMontagem CriarPresencaEncerrada(IReadOnlyCollection<Jogador> jogadores)
    {
        var montagem = DraftMontagem.CriarPorPresenca("Rinha", null, 5);
        foreach (var jogador in jogadores)
        {
            montagem.ConfirmarPresenca(Guid.NewGuid(), jogador.Id, null, DraftMontagemPresencaOrigem.Web);
        }

        montagem.EncerrarPresenca(false, 5);
        return montagem;
    }

    private static DraftMontagem CriarTempoRealComOrdemDefinida(IReadOnlyCollection<Jogador> jogadores)
    {
        var montagem = CriarPresencaEncerrada(jogadores);
        var jogadoresIds = jogadores.Select(jogador => jogador.Id).ToList();
        var capitaesIds = jogadoresIds.Take(2).ToList();
        montagem.SelecionarModo(DraftMontagemModo.TempoReal, jogadoresIds.ToHashSet());
        montagem.DefinirCapitaes(capitaesIds, capitaesIds.ToHashSet());
        montagem.DefinirOrdemEscolha(DraftMontagemOrdemEscolhaModo.Manual, capitaesIds);
        return montagem;
    }

    private static DraftMontagem CriarTempoRealIniciadoComReserva(IReadOnlyCollection<Jogador> jogadores)
    {
        var montagem = DraftMontagem.CriarPorPresenca("Rinha", null, 2);
        foreach (var jogador in jogadores)
        {
            montagem.ConfirmarPresenca(Guid.NewGuid(), jogador.Id, null, DraftMontagemPresencaOrigem.Web);
        }

        montagem.EncerrarPresenca(true, 2);
        var jogadoresIds = jogadores.Select(jogador => jogador.Id).ToList();
        var capitaesIds = jogadoresIds.Take(2).ToList();
        montagem.SelecionarModo(DraftMontagemModo.TempoReal, jogadoresIds.ToHashSet());
        montagem.DefinirCapitaes(capitaesIds, capitaesIds.ToHashSet());
        montagem.DefinirOrdemEscolha(DraftMontagemOrdemEscolhaModo.Manual, capitaesIds);
        montagem.IniciarTempoReal(DateTimeOffset.UtcNow, capitaesIds.ToHashSet());
        return montagem;
    }

    private static IReadOnlyCollection<Jogador> CriarJogadores(int quantidade)
    {
        return Enumerable.Range(1, quantidade)
            .Select(index => JogadorTestData.JogadorAtivo($"Jogador {index}"))
            .ToList();
    }
}
