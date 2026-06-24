using FluentAssertions;
using FluentValidation;
using RinhaDasLendas.Application.Commands.Jogadores;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.Jogadores;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Tests.Jogadores;

public sealed class UpdatePreferenciasTests
{
    [Fact]
    public async Task Handle_ShouldUpdatePreferences_WhenPayloadIsValid()
    {
        var repository = new InMemoryJogadorRepository();
        var jogador = JogadorTestData.JogadorAtivo();
        await repository.AddAsync(jogador, CancellationToken.None);
        var handler = new UpdatePreferenciasCommandHandler(repository, new UpdatePreferenciasRotasRequestDtoValidator());
        var request = new UpdatePreferenciasRotasRequestDto([
            new("Mid", 1, false),
            new("Adc", 2, false),
            new("Jungle", 3, false),
            new("Top", 4, true),
            new("Support", 5, false)
        ]);

        var response = await handler.Handle(new UpdatePreferenciasCommand(jogador.Id, request), CancellationToken.None);

        response.Should().NotBeNull();
        response!.Preferencias.First().Rota.Should().Be("Mid");
        response.Preferencias.Single(preferencia => preferencia.NaoJogoNemLascando).Rota.Should().Be("Top");
    }

    [Fact]
    public async Task Validator_ShouldRejectDuplicatedPriorities()
    {
        var validator = new UpdatePreferenciasRotasRequestDtoValidator();
        var request = new UpdatePreferenciasRotasRequestDto([
            new("Top", 1, false),
            new("Jungle", 1, false),
            new("Mid", 3, false),
            new("Adc", 4, false),
            new("Support", 5, false)
        ]);

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == MessageCodes.RoutePrioritiesMustBeUnique);
    }

    [Fact]
    public async Task Validator_ShouldRejectMoreThanOneBlockedRoute()
    {
        var validator = new UpdatePreferenciasRotasRequestDtoValidator();
        var request = new UpdatePreferenciasRotasRequestDto([
            new("Top", 1, true),
            new("Jungle", 2, true),
            new("Mid", 3, false),
            new("Adc", 4, false),
            new("Support", 5, false)
        ]);

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == MessageCodes.OnlyOneNeverPlayRole);
    }
}
