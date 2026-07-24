using FluentAssertions;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Tests.AgendamentosPresenca;

public sealed class AgendamentoPresencaValidatorTests
{
    private readonly AgendamentoPresencaRequestValidator _validator = new();

    public static TheoryData<SaveAgendamentoPresencaRequestDto, string> InvalidRequests => new()
    {
        { Valid() with { Nome = "" }, MessageCodes.PresenceScheduleNameRequired },
        { Valid() with { Nome = "ab" }, MessageCodes.PresenceScheduleNameLengthInvalid },
        { Valid() with { Nome = new string('a', 101) }, MessageCodes.PresenceScheduleNameLengthInvalid },
        { Valid() with { Observacao = new string('a', 501) }, MessageCodes.PresenceScheduleObservationTooLong },
        { Valid() with { DiasSemana = [] }, MessageCodes.PresenceScheduleDayRequired },
        { Valid() with { DiasSemana = [DiaSemanaIso.Sexta, DiaSemanaIso.Sexta] }, MessageCodes.PresenceScheduleDayDuplicated },
        { Valid() with { HorarioEncerramento = new TimeOnly(18, 0) }, MessageCodes.PresenceScheduleTimeRangeInvalid },
        { Valid() with { HorarioPublicacao = new TimeOnly(18, 0, 1) }, MessageCodes.PresenceScheduleTimeRangeInvalid },
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task ValidateAsync_ShouldReturnContractCode(
        SaveAgendamentoPresencaRequestDto request,
        string messageCode)
    {
        var result = await _validator.ValidateAsync(request);

        result.Errors.Should().Contain(error => error.ErrorCode == messageCode && error.ErrorMessage == messageCode);
    }

    [Fact]
    public async Task ValidateAsync_ShouldAcceptValidContract()
    {
        var result = await _validator.ValidateAsync(Valid());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_ShouldApplyDomainNormalizationBeforeLengthChecks()
    {
        var request = Valid() with
        {
            Nome = "  Agenda normalizada  ",
            Observacao = $"  {new string('a', 500)}  ",
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    private static SaveAgendamentoPresencaRequestDto Valid() => new(
        "Sexta da comunidade",
        "Lista semanal",
        [DiaSemanaIso.Sexta],
        new TimeOnly(18, 0),
        new TimeOnly(20, 0));
}
