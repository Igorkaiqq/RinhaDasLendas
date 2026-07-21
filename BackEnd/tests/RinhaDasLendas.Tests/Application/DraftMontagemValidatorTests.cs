using FluentAssertions;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Tests.Application;

public sealed class DraftMontagemValidatorTests
{
    public static TheoryData<string?> MotivosInvalidos => new()
    {
        null,
        string.Empty,
        "   ",
    };

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

    [Theory]
    [MemberData(nameof(MotivosInvalidos))]
    public void AcoesAdministrativas_DevemExigirMotivo(string? motivo)
    {
        var validatorsAndRequests = new (object Validator, object Request)[]
        {
            (new CancelarDraftMontagemValidator(), new CancelarDraftMontagemRequestDto(motivo)),
            (new AdicionarPresencaManualDraftMontagemValidator(), new AdicionarPresencaManualDraftMontagemRequestDto(Guid.NewGuid(), motivo)),
            (new RemoverPresencaManualDraftMontagemValidator(), new RemoverPresencaManualDraftMontagemRequestDto(Guid.NewGuid(), motivo)),
            (new RepublicarPublicacaoDiscordDraftMontagemValidator(), new RepublicarPublicacaoDiscordDraftMontagemRequestDto(DraftMontagemPublicacaoDiscordTipo.Presenca, motivo)),
        };

        var results = validatorsAndRequests.Select(item => item switch
        {
            { Validator: CancelarDraftMontagemValidator validator, Request: CancelarDraftMontagemRequestDto request } => validator.Validate(request),
            { Validator: AdicionarPresencaManualDraftMontagemValidator validator, Request: AdicionarPresencaManualDraftMontagemRequestDto request } => validator.Validate(request),
            { Validator: RemoverPresencaManualDraftMontagemValidator validator, Request: RemoverPresencaManualDraftMontagemRequestDto request } => validator.Validate(request),
            { Validator: RepublicarPublicacaoDiscordDraftMontagemValidator validator, Request: RepublicarPublicacaoDiscordDraftMontagemRequestDto request } => validator.Validate(request),
            _ => throw new InvalidOperationException(),
        });

        results.Should().OnlyContain(result => !result.IsValid);
        results.Should().OnlyContain(result => result.Errors.Any(error => error.ErrorMessage == MessageCodes.FieldRequired));
    }

    [Fact]
    public void AcoesAdministrativas_DevemLimitarMotivoA500Caracteres()
    {
        var motivo = new string('a', 501);

        new CancelarDraftMontagemValidator().Validate(new CancelarDraftMontagemRequestDto(motivo)).Errors.Should().Contain(error => error.ErrorMessage == MessageCodes.CancellationReasonMaxLength);
        new AdicionarPresencaManualDraftMontagemValidator().Validate(new AdicionarPresencaManualDraftMontagemRequestDto(Guid.NewGuid(), motivo)).Errors.Should().Contain(error => error.ErrorMessage == MessageCodes.CancellationReasonMaxLength);
        new RemoverPresencaManualDraftMontagemValidator().Validate(new RemoverPresencaManualDraftMontagemRequestDto(Guid.NewGuid(), motivo)).Errors.Should().Contain(error => error.ErrorMessage == MessageCodes.CancellationReasonMaxLength);
        new RepublicarPublicacaoDiscordDraftMontagemValidator().Validate(new RepublicarPublicacaoDiscordDraftMontagemRequestDto(DraftMontagemPublicacaoDiscordTipo.Presenca, motivo)).Errors.Should().Contain(error => error.ErrorMessage == MessageCodes.CancellationReasonMaxLength);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Invalido")]
    [InlineData("999")]
    public void PublicacaoDiscord_DeveRejeitarTipoInvalido(string? tipo)
    {
        var completion = new RegistrarPublicacaoDiscordDraftMontagemValidator().Validate(
            new RegistrarPublicacaoDiscordDraftMontagemRequestDto(tipo!, Guid.NewGuid(), "guild", "channel", "message"));
        var failure = new RegistrarFalhaPublicacaoDiscordDraftMontagemValidator().Validate(
            new RegistrarFalhaPublicacaoDiscordDraftMontagemRequestDto(tipo!, Guid.NewGuid(), "guild", "channel", "erro"));

        completion.IsValid.Should().BeFalse();
        failure.IsValid.Should().BeFalse();
    }

    [Fact]
    public void PublicacaoDiscord_DeveExigirClaimEMessageId()
    {
        var completion = new RegistrarPublicacaoDiscordDraftMontagemValidator().Validate(
            new RegistrarPublicacaoDiscordDraftMontagemRequestDto("Presenca", Guid.Empty, null, null, null!));
        var failure = new RegistrarFalhaPublicacaoDiscordDraftMontagemValidator().Validate(
            new RegistrarFalhaPublicacaoDiscordDraftMontagemRequestDto("Presenca", Guid.Empty, null, null, null));

        completion.Errors.Should().Contain(error => error.PropertyName == "ClaimId" && error.ErrorMessage == MessageCodes.DiscordPublicationClaimInvalid);
        completion.Errors.Should().Contain(error => error.PropertyName == "MessageId" && error.ErrorMessage == MessageCodes.FieldRequired);
        failure.Errors.Should().Contain(error => error.PropertyName == "ClaimId" && error.ErrorMessage == MessageCodes.DiscordPublicationClaimInvalid);
    }

    [Fact]
    public void PublicacaoDiscord_DeveRespeitarLimitesDosCampos()
    {
        var overForty = new string('1', 41);
        var overOneHundredTwenty = new string('e', 121);
        var completion = new RegistrarPublicacaoDiscordDraftMontagemValidator().Validate(
            new RegistrarPublicacaoDiscordDraftMontagemRequestDto("Presenca", Guid.NewGuid(), overForty, overForty, overForty));
        var failure = new RegistrarFalhaPublicacaoDiscordDraftMontagemValidator().Validate(
            new RegistrarFalhaPublicacaoDiscordDraftMontagemRequestDto("Presenca", Guid.NewGuid(), overForty, overForty, overOneHundredTwenty));

        completion.Errors.Should().Contain(error => error.PropertyName == "DiscordGuildId" && error.ErrorMessage == MessageCodes.MaxLengthExceeded);
        completion.Errors.Should().Contain(error => error.PropertyName == "DiscordChannelId" && error.ErrorMessage == MessageCodes.MaxLengthExceeded);
        completion.Errors.Should().Contain(error => error.PropertyName == "MessageId" && error.ErrorMessage == MessageCodes.MaxLengthExceeded);
        failure.Errors.Should().Contain(error => error.PropertyName == "DiscordGuildId" && error.ErrorMessage == MessageCodes.MaxLengthExceeded);
        failure.Errors.Should().Contain(error => error.PropertyName == "DiscordChannelId" && error.ErrorMessage == MessageCodes.MaxLengthExceeded);
        failure.Errors.Should().Contain(error => error.PropertyName == "ErroCodigo" && error.ErrorMessage == MessageCodes.MaxLengthExceeded);
    }
}
