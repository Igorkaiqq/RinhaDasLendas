using FluentAssertions;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Tests.Application;

public sealed class DraftMontagemValidatorTests
{
    [Fact]
    public void Deve_retornar_erro_de_validacao_para_tamanho_de_equipe_zero()
    {
        var validator = new CreateDraftMontagemValidator();
        var request = new CreateDraftMontagemRequestDto(
            "Rinha",
            null,
            0,
            true,
            null,
            null,
            [],
            Enumerable.Range(1, 5).Select(_ => Guid.NewGuid()).ToList());

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.ErrorMessage).Should().Contain(MessageCodes.TeamSizeRange);
    }
}
