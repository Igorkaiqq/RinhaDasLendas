using FluentAssertions;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Validators;

namespace RinhaDasLendas.Tests.Validators;

public sealed class UpdateTimeValidatorTests
{
    private readonly UpdateTimeValidator _validator = new();

    [Fact]
    public void Deve_rejeitar_capitao_fora_da_composicao()
    {
        var request = new UpdateTimeRequestDto("Os Sem Baron", "OSB", null, Guid.NewGuid(), [Guid.NewGuid()]);

        var result = _validator.Validate(request);

        result.Errors.Should().Contain(error => error.ErrorMessage == "Capitao deve fazer parte do time.");
    }
}
