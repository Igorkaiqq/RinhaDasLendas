using FluentAssertions;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Validators;

namespace RinhaDasLendas.Tests.Validators;

public sealed class CreateDraftValidatorTests
{
    private readonly CreateDraftValidator _validator = new();

    [Fact]
    public void Deve_aceitar_draft_valido()
    {
        var jogadores = Enumerable.Range(1, 4).Select(_ => Guid.NewGuid()).ToList();
        var request = new CreateDraftRequestDto("Rinha", null, 2, false, jogadores[0], jogadores[1], false, "TimeA", jogadores);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Deve_rejeitar_sem_capitaes_quando_nao_sorteia()
    {
        var request = new CreateDraftRequestDto("Rinha", null, 2, false, null, null, false, "TimeA", [Guid.NewGuid(), Guid.NewGuid()]);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}
