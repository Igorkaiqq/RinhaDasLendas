using FluentAssertions;
using FluentValidation;
using RinhaDasLendas.Application.Commands.Jogadores;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.Jogadores;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Tests.Jogadores;

public sealed class CreateJogadorTests
{
    [Fact]
    public async Task Handle_ShouldCreateActivePlayer_WhenRequestIsValid()
    {
        var repository = new InMemoryJogadorRepository();
        var handler = new CreateJogadorCommandHandler(repository, new JogadorCreateRequestDtoValidator());

        var response = await handler.Handle(new CreateJogadorCommand(JogadorTestData.CreateRequest()), CancellationToken.None);

        response.NomeExibicao.Should().Be("Faker da Firma");
        response.Status.Should().Be("Ativo");
        response.Preferencias.Should().HaveCount(5);
    }

    [Fact]
    public async Task Handle_ShouldRejectRequest_WhenNameIsMissing()
    {
        var repository = new InMemoryJogadorRepository();
        var handler = new CreateJogadorCommandHandler(repository, new JogadorCreateRequestDtoValidator());
        var request = JogadorTestData.CreateRequest("") with { NomeExibicao = "" };

        var act = () => handler.Handle(new CreateJogadorCommand(request), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage($"*{MessageCodes.PlayerNameRequired}*");
    }

    [Fact]
    public async Task Validator_ShouldRejectMissingDiscord()
    {
        var validator = new JogadorCreateRequestDtoValidator();
        var request = JogadorTestData.CreateRequest() with { Discord = "" };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == MessageCodes.FieldRequired);
    }

    [Theory]
    [InlineData("")]
    [InlineData("XPTO")]
    [InlineData("Ouro II")]
    public async Task Validator_ShouldRejectInvalidElo(string elo)
    {
        var validator = new JogadorCreateRequestDtoValidator();
        var request = JogadorTestData.CreateRequest() with { Elo = elo };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("Ouro", "II")]
    [InlineData("Mestre", null)]
    [InlineData("Grão-Mestre", null)]
    public async Task Validator_ShouldAcceptOfficialEloFormats(string elo, string? divisao)
    {
        var validator = new JogadorCreateRequestDtoValidator();
        var request = JogadorTestData.CreateRequest() with { Elo = elo, Divisao = divisao };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validator_ShouldRejectInvalidProfileDomains()
    {
        var validator = new JogadorCreateRequestDtoValidator();
        var request = JogadorTestData.CreateRequest() with
        {
            OpGgUrl = "https://example.com/player",
            DeepLolUrl = "https://example.com/player"
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("OP.GG"));
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("Deeplol"));
    }
}
