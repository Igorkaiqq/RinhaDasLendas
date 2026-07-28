using FluentAssertions;
using Moq;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.DraftMontagens;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Tests.Application;

public sealed class DraftMontagemCommandHandlerTests
{
    [Fact]
    public async Task ReabrirPresenca_DeveUsarUsuarioAtualPersistirUmaVezRetornarEstadoAtualizadoENotificar()
    {
        var montagem = NovaMontagemComPresencaEncerrada();
        var usuarioId = Guid.NewGuid();
        var repository = new Mock<IDraftMontagemRepository>();
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        repository.Setup(item => item.GetByIdAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        repository.Setup(item => item.GetJogadorByUsuarioIdAsync(usuarioId, It.IsAny<CancellationToken>())).ReturnsAsync((Jogador?)null);
        var handler = new ReabrirPresencaDraftMontagemCommandHandler(repository.Object, new CurrentUser(usuarioId), notifier.Object);

        var result = await handler.Handle(new ReabrirPresencaDraftMontagemCommand(montagem.Id), CancellationToken.None);

        result!.Status.Should().Be(DraftMontagemStatus.PresencaAberta.ToString());
        montagem.AcoesAdministrativas.Should().ContainSingle(acao =>
            acao.Tipo == "ReaberturaPresenca" && acao.ResponsavelUsuarioId == usuarioId);
        repository.Verify(item => item.SaveChangesAsync(CancellationToken.None), Times.Once);
        notifier.Verify(item => item.StateUpdatedAsync(
            montagem.Id,
            It.Is<DraftMontagemRealtimeStateDto>(state => state.Montagem.Status == DraftMontagemStatus.PresencaAberta.ToString()),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ReabrirPresenca_QuandoDraftNaoExisteDeveRetornarNullSemPersistirOuNotificar()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IDraftMontagemRepository>();
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        repository.Setup(item => item.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((DraftMontagem?)null);
        var handler = new ReabrirPresencaDraftMontagemCommandHandler(repository.Object, new CurrentUser(Guid.NewGuid()), notifier.Object);

        var result = await handler.Handle(new ReabrirPresencaDraftMontagemCommand(id), CancellationToken.None);

        result.Should().BeNull();
        repository.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        notifier.Verify(item => item.StateUpdatedAsync(
            It.IsAny<Guid>(),
            It.IsAny<DraftMontagemRealtimeStateDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static DraftMontagem NovaMontagemComPresencaEncerrada()
    {
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        montagem.EncerrarPresenca(true, 5);
        return montagem;
    }

    private sealed record CurrentUser(Guid? UserId) : ICurrentUser
    {
        public IReadOnlyCollection<string> Roles => [];
        public string? IpAddress => null;
        public string? UserAgent => null;
    }
}
