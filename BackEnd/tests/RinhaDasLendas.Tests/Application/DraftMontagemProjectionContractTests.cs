using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RinhaDasLendas.Api.Controllers;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Tests.Application;

public sealed class DraftMontagemProjectionContractTests
{
    [Fact]
    public void DezesseteSuperficiesPublicasDetalhadasDevemDeclararSomenteDtoPublico()
    {
        string[] actionNames =
        [
            nameof(DraftMontagensController.GetById),
            nameof(DraftMontagensController.Create),
            nameof(DraftMontagensController.StartRealtime),
            nameof(DraftMontagensController.ConfirmPresence),
            nameof(DraftMontagensController.CancelPresence),
            nameof(DraftMontagensController.AddManualPresence),
            nameof(DraftMontagensController.RemoveManualPresence),
            nameof(DraftMontagensController.ClosePresence),
            nameof(DraftMontagensController.DefineCaptains),
            nameof(DraftMontagensController.DefinePickOrder),
            nameof(DraftMontagensController.RepublishDiscordPublication),
            nameof(DraftMontagensController.Pick),
            nameof(DraftMontagensController.SubstituteReserve),
            nameof(DraftMontagensController.SaveLayout),
            nameof(DraftMontagensController.DrawCaptains),
            nameof(DraftMontagensController.Finalize),
            nameof(DraftMontagensController.Cancel),
        ];

        var responseTypes = typeof(DraftMontagensController)
            .GetMethods()
            .Where(method => actionNames.Contains(method.Name))
            .Select(method => method.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), true)
                .Cast<ProducesResponseTypeAttribute>()
                .Single(attribute => attribute.StatusCode is StatusCodes.Status200OK or StatusCodes.Status201Created)
                .Type)
            .ToList();

        responseTypes.Should().HaveCount(17);
        responseTypes.Should().OnlyContain(type => type == typeof(DraftMontagemResponseDto) || type == typeof(DraftMontagemRealtimeStateDto));
        responseTypes.Should().NotContain(typeof(DraftMontagemAdminResponseDto));
        responseTypes.Should().NotContain(typeof(DraftMontagemDiscordOperationalDto));
    }
}
