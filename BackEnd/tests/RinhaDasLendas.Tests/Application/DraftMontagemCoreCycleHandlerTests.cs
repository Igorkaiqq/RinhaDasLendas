using FluentAssertions;
using FluentValidation;
using Moq;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.DraftMontagens;
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
        var handler = new CreateDraftMontagemCommandHandler(repository.Object, new CreateDraftMontagemValidator());

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

    private static IReadOnlyCollection<Jogador> CriarJogadores(int quantidade)
    {
        return Enumerable.Range(1, quantidade)
            .Select(index => JogadorTestData.JogadorAtivo($"Jogador {index}"))
            .ToList();
    }
}
