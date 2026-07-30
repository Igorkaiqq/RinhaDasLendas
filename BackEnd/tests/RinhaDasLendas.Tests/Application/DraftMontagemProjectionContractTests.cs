using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Api.Controllers;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Infrastructure.Messages;

namespace RinhaDasLendas.Tests.Application;

public sealed class DraftMontagemProjectionContractTests
{
    [Fact]
    public void GuardaArquiteturalDeveFixarMetadataEDtoExatoDasDezoitoSuperficiesPublicas()
    {
        // This guards declared response metadata and DTO families only. The HTTP mutation matrix belongs to T110.
        string[] actionNames =
        [
            nameof(DraftMontagensController.GetById),
            nameof(DraftMontagensController.Create),
            nameof(DraftMontagensController.SelectMode),
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

        responseTypes.Should().HaveCount(18);
        responseTypes.Should().OnlyContain(type => type == typeof(DraftMontagemResponseDto) || type == typeof(DraftMontagemRealtimeStateDto));
        responseTypes.Should().NotContain(typeof(DraftMontagemAdminResponseDto));
        responseTypes.Should().NotContain(typeof(DraftMontagemDiscordOperationalDto));
    }

    [Fact]
    public void ProjecoesDevemExporModoAnulavelECicloSemVazarElegibilidadeNoContratoPublico()
    {
        var montagem = DraftMontagem.CriarPorPresenca("Rinha", null, 5);

        var publicJson = JsonSerializer.Serialize(
            DraftMontagemResponseDto.FromEntity(montagem),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var adminJson = JsonSerializer.Serialize(
            DraftMontagemAdminResponseDto.FromEntity(montagem, [Guid.NewGuid()]),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        publicJson.Should().Contain("\"modo\":null")
            .And.Contain("\"cicloVersao\":\"ModoPosPresenca\"")
            .And.NotContain("capitaesElegiveisIds");
        adminJson.Should().Contain("\"modo\":null")
            .And.Contain("\"cicloVersao\":\"ModoPosPresenca\"")
            .And.Contain("capitaesElegiveisIds")
            .And.Contain("capitaesElegiveisSubstituicaoIds");
        typeof(DraftMontagemResumoDto).GetProperty("CicloVersao").Should().NotBeNull();
    }

    [Theory]
    [InlineData(nameof(DraftMontagensController.DefineCaptains))]
    [InlineData(nameof(DraftMontagensController.DefinePickOrder))]
    [InlineData(nameof(DraftMontagensController.StartRealtime))]
    [InlineData(nameof(DraftMontagensController.SaveLayout))]
    [InlineData(nameof(DraftMontagensController.SubstituteReserve))]
    [InlineData(nameof(DraftMontagensController.DrawCaptains))]
    [InlineData(nameof(DraftMontagensController.Finalize))]
    public void OperacoesAdministrativasDoCicloDevemDeclarar401_403_409NoSwagger(string actionName)
    {
        var statusCodes = typeof(DraftMontagensController)
            .GetMethod(actionName)!
            .GetCustomAttributes(typeof(ProducesResponseTypeAttribute), true)
            .Cast<ProducesResponseTypeAttribute>()
            .Select(attribute => attribute.StatusCode);

        statusCodes.Should().Contain([
            StatusCodes.Status401Unauthorized,
            StatusCodes.Status403Forbidden,
            StatusCodes.Status409Conflict,
        ]);
    }

    [Theory]
    [InlineData(nameof(DraftMontagensController.DefineCaptains))]
    [InlineData(nameof(DraftMontagensController.DefinePickOrder))]
    [InlineData(nameof(DraftMontagensController.DrawCaptains))]
    [InlineData(nameof(DraftMontagensController.Finalize))]
    public void OperacoesComErroDeDominioDevemDeclarar400NoSwagger(string actionName)
    {
        var response = typeof(DraftMontagensController)
            .GetMethod(actionName)!
            .GetCustomAttributes(typeof(ProducesResponseTypeAttribute), true)
            .Cast<ProducesResponseTypeAttribute>()
            .SingleOrDefault(attribute => attribute.StatusCode == StatusCodes.Status400BadRequest);

        response.Should().NotBeNull();
        response!.Type.Should().Be(typeof(ApiErrorResponse));
    }

    [Theory]
    [InlineData("MV107", "pt-BR", "O capitão do draft deve pertencer ao recorte titular")]
    [InlineData("MV107", "en-US", "The draft captain must be part of the starter pool")]
    [InlineData("MV108", "pt-BR", "O capitão do draft deve estar ativo, vinculado a um usuário ativo e possuir o cargo Capitão")]
    [InlineData("MV108", "en-US", "The draft captain must be active, linked to an active user, and have the Captain role")]
    [InlineData("MV109", "pt-BR", "O novo capitão só pode ser informado quando o capitão atual sair do time")]
    [InlineData("MV109", "en-US", "The new captain may only be provided when the current captain leaves the team")]
    [InlineData("MV110", "pt-BR", "O bot só pode criar drafts de presença")]
    [InlineData("MV110", "en-US", "The bot can only create presence drafts")]
    public void NovosCodigosDevemResolverMensagensLocalizadas(string code, string culture, string expected)
    {
        new ResourceMessageProvider().GetMessage(code, culture).Should().Be(expected);
    }
}
