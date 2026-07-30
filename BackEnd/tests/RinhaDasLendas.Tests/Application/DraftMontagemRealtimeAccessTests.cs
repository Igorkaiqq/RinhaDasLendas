using FluentAssertions;
using MediatR;
using Moq;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.DraftMontagens;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.DraftMontagens;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Infrastructure.Messages;

namespace RinhaDasLendas.Tests.Application;

public sealed class DraftMontagemRealtimeAccessTests
{
    [Fact]
    public void SnapshotCompartilhado_NaoDeveExporCapacidadeDoUsuarioAtual()
    {
        var snapshotType = typeof(DraftMontagemRealtimeSnapshotDto);

        snapshotType.GetProperty(nameof(DraftMontagemRealtimeSnapshotDto.Montagem)).Should().NotBeNull();
        snapshotType.GetProperty(nameof(DraftMontagemRealtimeSnapshotDto.ServerNow)).Should().NotBeNull();
        snapshotType.GetProperty("CanCurrentUserPick").Should().BeNull();
        typeof(DraftMontagemRealtimeStateDto).GetProperty("CanCurrentUserPick").Should().NotBeNull();
    }

    [Theory]
    [InlineData(true, false, true, false)]
    [InlineData(false, false, true, false)]
    [InlineData(true, true, true, false)]
    [InlineData(true, false, false, false)]
    [InlineData(true, false, true, true)]
    public async Task ConsultaDeAcesso_DevePermitirSomenteHumanoAutenticadoComDraftAtivo(
        bool hasUserId,
        bool isBot,
        bool draftExists,
        bool archived)
    {
        var draft = CreateDraft();
        if (archived)
        {
            draft.Arquivar("Motivo de teste", Guid.NewGuid(), DateTimeOffset.UtcNow);
        }

        var repository = new Mock<IDraftMontagemRepository>();
        repository
            .Setup(item => item.GetByIdAsync(draft.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(draftExists ? draft : null);
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(item => item.UserId).Returns(hasUserId ? Guid.NewGuid() : null);
        currentUser.SetupGet(item => item.IsBot).Returns(isBot);
        var handler = new CanViewDraftMontagemQueryHandler(repository.Object, currentUser.Object);

        var allowed = await handler.Handle(new CanViewDraftMontagemQuery(draft.Id), CancellationToken.None);

        allowed.Should().Be(hasUserId && !isBot && draftExists && !archived);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetPersonalizado_DeveUsarConsultaCompartilhadaEPreservarContratoFlat(bool allowed)
    {
        var draft = CreateDraft();
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.GetByIdAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);
        repository.Setup(item => item.GetJogadorByUsuarioIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Jogador?)null);
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(item => item.UserId).Returns(Guid.NewGuid());
        var sender = new Mock<ISender>();
        sender
            .Setup(item => item.Send(new CanViewDraftMontagemQuery(draft.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allowed);
        var handler = new GetDraftMontagemRealtimeStateQueryHandler(repository.Object, currentUser.Object, sender.Object);

        var result = await handler.Handle(new GetDraftMontagemRealtimeStateQuery(draft.Id), CancellationToken.None);

        if (allowed)
        {
            result.Should().NotBeNull();
            result!.Montagem.Id.Should().Be(draft.Id);
            result.CanCurrentUserPick.Should().BeFalse();
        }
        else
        {
            result.Should().BeNull();
            repository.Verify(item => item.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Theory]
    [InlineData("pt-BR", "Este draft não está disponível para sincronização em tempo real")]
    [InlineData("en-US", "This draft is not available for real-time synchronization")]
    public void RejeicaoComum_DeveEstarLocalizada(string culture, string expected)
    {
        new ResourceMessageProvider().GetMessage(MessageCodes.DraftRealtimeUnavailable, culture).Should().Be(expected);
    }

    private static DraftMontagem CreateDraft()
        => new("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
}
