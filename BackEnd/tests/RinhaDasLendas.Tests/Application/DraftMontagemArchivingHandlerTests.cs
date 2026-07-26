using FluentAssertions;
using Moq;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.DraftMontagens;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Tests.Application;

public sealed class DraftMontagemArchivingHandlerTests
{
    [Fact]
    public async Task Arquivar_DeveUsarUsuarioAtualPersistirUmaVezENotificarSomenteAposSucesso()
    {
        var montagem = NovaMontagem();
        var usuarioId = Guid.NewGuid();
        var repository = new Mock<IDraftMontagemRepository>();
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        repository.Setup(item => item.GetByIdIncludingArchivedAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        repository.Setup(item => item.TrySaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(DraftMontagemSaveResultado.Persistido);
        var handler = new ArquivarDraftMontagemCommandHandler(repository.Object, new ArquivarDraftMontagemValidator(), new CurrentUser(usuarioId), notifier.Object);

        var result = await handler.Handle(
            new ArquivarDraftMontagemCommand(montagem.Id, new ArquivarDraftMontagemRequestDto(" motivo ", montagem.VersaoEstado)),
            CancellationToken.None);

        result!.Arquivado.Should().BeTrue();
        montagem.AcoesAdministrativas.Should().OnlyContain(acao => acao.ResponsavelUsuarioId == usuarioId);
        repository.Verify(item => item.TrySaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        notifier.Verify(item => item.ArchivedAsync(montagem.Id, It.IsAny<CancellationToken>()), Times.Once);
        notifier.Verify(item => item.StateUpdatedAsync(It.IsAny<Guid>(), It.IsAny<DraftMontagemRealtimeStateDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Arquivar_ComVersaoObsoletaDeveRetornarConflitoSemPersistir()
    {
        var montagem = NovaMontagem();
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.GetByIdIncludingArchivedAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        var handler = new ArquivarDraftMontagemCommandHandler(repository.Object, new ArquivarDraftMontagemValidator(), new CurrentUser(Guid.NewGuid()), Mock.Of<IDraftMontagemRealtimeNotifier>());

        var act = () => handler.Handle(
            new ArquivarDraftMontagemCommand(montagem.Id, new ArquivarDraftMontagemRequestDto("motivo", montagem.VersaoEstado + 1)),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage(MessageCodes.DraftStateConflict);
        repository.Verify(item => item.TrySaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Restaurar_DevePreservarCancelamentoEHistorico()
    {
        var montagem = NovaMontagem();
        montagem.Arquivar("motivo", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.GetByIdIncludingArchivedAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        repository.Setup(item => item.TrySaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(DraftMontagemSaveResultado.Persistido);
        var handler = new RestaurarDraftMontagemCommandHandler(repository.Object, new RestaurarDraftMontagemValidator(), new CurrentUser(Guid.NewGuid()));

        var result = await handler.Handle(
            new RestaurarDraftMontagemCommand(montagem.Id, new RestaurarDraftMontagemRequestDto(montagem.VersaoEstado)),
            CancellationToken.None);

        result!.Arquivado.Should().BeFalse();
        montagem.Status.Should().Be(DraftMontagemStatus.Cancelada);
        montagem.AcoesAdministrativas.Select(acao => acao.Tipo).Should().Contain(["Arquivamento", "Restauracao"]);
    }

    [Fact]
    public async Task HandlerOperacional_DeveTraduzirConflitoDeVersaoParaMv103()
    {
        var montagem = NovaMontagem();
        typeof(DraftMontagem).GetProperty(nameof(DraftMontagem.Status))!.SetValue(montagem, DraftMontagemStatus.Aberta);
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.GetByIdAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        repository.Setup(item => item.TrySaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(DraftMontagemSaveResultado.ConflitoDeVersao);
        var handler = new FinalizarDraftMontagemCommandHandler(repository.Object, new CurrentUser(Guid.NewGuid()), Mock.Of<IDraftMontagemRealtimeNotifier>());

        var act = () => handler.Handle(new FinalizarDraftMontagemCommand(montagem.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage(MessageCodes.DraftStateConflict);
        repository.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static DraftMontagem NovaMontagem() =>
        new("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);

    private sealed record CurrentUser(Guid? UserId) : ICurrentUser
    {
        public IReadOnlyCollection<string> Roles => [];
        public string? IpAddress => null;
        public string? UserAgent => null;
    }
}
