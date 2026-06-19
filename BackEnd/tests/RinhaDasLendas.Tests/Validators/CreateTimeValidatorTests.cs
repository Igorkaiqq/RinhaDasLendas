using FluentAssertions;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Validators;

namespace RinhaDasLendas.Tests.Validators;

public sealed class CreateTimeValidatorTests
{
    private readonly CreateTimeValidator _validator = new();

    [Fact]
    public void Deve_validar_payload_valido()
    {
        var jogadorId = Guid.NewGuid();
        var request = new CreateTimeRequestDto("Os Sem Baron", "OSB", null, jogadorId, [jogadorId]);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Deve_rejeitar_jogador_duplicado()
    {
        var jogadorId = Guid.NewGuid();
        var request = new CreateTimeRequestDto("Os Sem Baron", "OSB", null, null, [jogadorId, jogadorId]);

        var result = _validator.Validate(request);

        result.Errors.Should().Contain(error => error.ErrorMessage == "O mesmo jogador nao pode ser adicionado mais de uma vez.");
    }
}
