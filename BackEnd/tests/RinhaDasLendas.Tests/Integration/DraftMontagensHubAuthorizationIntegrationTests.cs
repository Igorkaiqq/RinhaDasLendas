using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using RinhaDasLendas.Api.Controllers;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Api.Hubs;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.DraftMontagens;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Tests.Integration;

public sealed class DraftMontagensHubAuthorizationIntegrationTests
{
    [Fact]
    public void Hub_DeveExigirAutenticacao()
    {
        typeof(DraftMontagensHub).GetCustomAttributes(typeof(AuthorizeAttribute), true).Should().ContainSingle();
    }

    [Fact]
    public async Task Join_DeveConsultarAcessoAntesDeAdicionarConexaoAoGrupo()
    {
        var draftId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender
            .Setup(item => item.Send(new CanViewDraftMontagemQuery(draftId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var groups = new Mock<IGroupManager>();
        var context = new Mock<HubCallerContext>();
        context.SetupGet(item => item.ConnectionId).Returns("connection-id");
        var hub = new DraftMontagensHub(sender.Object, Mock.Of<IMessageProvider>())
        {
            Context = context.Object,
            Groups = groups.Object,
        };

        await hub.JoinDraftMontagem(draftId);

        groups.Verify(
            item => item.AddToGroupAsync("connection-id", DraftMontagensHub.GroupName(draftId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task JoinNegado_DeveUsarRejeicaoComumSemAdicionarConexaoAoGrupo()
    {
        var draftId = Guid.NewGuid();
        var expectedMessage = "localized realtime rejection";
        var sender = new Mock<ISender>();
        sender
            .Setup(item => item.Send(new CanViewDraftMontagemQuery(draftId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var messages = new Mock<IMessageProvider>();
        messages.Setup(item => item.GetMessage(MessageCodes.DraftRealtimeUnavailable)).Returns(expectedMessage);
        var groups = new Mock<IGroupManager>();
        var hub = new DraftMontagensHub(sender.Object, messages.Object)
        {
            Context = Mock.Of<HubCallerContext>(),
            Groups = groups.Object,
        };

        var action = () => hub.JoinDraftMontagem(draftId);

        await action.Should().ThrowAsync<HubException>().WithMessage(expectedMessage);
        groups.Verify(
            item => item.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetNegado_DeveUsarAMesmaRejeicaoComumDoJoin()
    {
        var draftId = Guid.NewGuid();
        var expectedMessage = "localized realtime rejection";
        var sender = new Mock<ISender>();
        sender
            .Setup(item => item.Send(new GetDraftMontagemRealtimeStateQuery(draftId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DraftMontagemRealtimeStateDto?)null);
        var messages = new Mock<IMessageProvider>();
        messages.Setup(item => item.GetMessage(MessageCodes.DraftRealtimeUnavailable)).Returns(expectedMessage);
        var controller = new DraftMontagensController(sender.Object, messages.Object, Mock.Of<IAuthorizationService>());

        var result = await controller.GetRealtimeState(draftId, CancellationToken.None);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var error = notFound.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        error.Message.Should().Be(expectedMessage);
        error.MessageCode.Should().Be(MessageCodes.DraftRealtimeUnavailable);
    }
}
