using FluentAssertions;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Tests.Application;

public sealed class DraftMontagemValidatorTests
{
    [Theory]
    [InlineData(500, true)]
    [InlineData(501, false)]
    public void Arquivamento_DeveValidarMotivoNormalizadoEVersao(int tamanho, bool valido)
    {
        var result = new ArquivarDraftMontagemValidator().Validate(
            new ArquivarDraftMontagemRequestDto(new string('x', tamanho), 0));

        result.IsValid.Should().Be(valido);
        new RestaurarDraftMontagemValidator().Validate(new RestaurarDraftMontagemRequestDto(-1))
            .Errors.Should().Contain(error => error.ErrorMessage == MessageCodes.DraftStateVersionInvalid);
    }

    [Fact]
    public void Arquivamento_DeveAceitar500CaracteresSignificativosComEspacosNasExtremidades()
    {
        var result = new ArquivarDraftMontagemValidator().Validate(
            new ArquivarDraftMontagemRequestDto($"  {new string('x', 500)}  ", 0));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Arquivamento_DeveRejeitar501CaracteresSignificativosAposTrim()
    {
        var result = new ArquivarDraftMontagemValidator().Validate(
            new ArquivarDraftMontagemRequestDto($"  {new string('x', 501)}  ", 0));

        result.Errors.Should().ContainSingle(error => error.ErrorMessage == MessageCodes.ArchiveReasonMaxLength);
    }

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

    [Theory]
    [InlineData(500, true)]
    [InlineData(501, false)]
    public void AcoesAdministrativas_DevemLimitarMotivoA500Caracteres(int length, bool expectedValid)
    {
        var motivo = new string('a', length);

        new CancelarDraftMontagemValidator().Validate(new CancelarDraftMontagemRequestDto(motivo)).IsValid.Should().Be(expectedValid);
        new AdicionarPresencaManualDraftMontagemValidator().Validate(new AdicionarPresencaManualDraftMontagemRequestDto(Guid.NewGuid(), motivo)).IsValid.Should().Be(expectedValid);
        new RemoverPresencaManualDraftMontagemValidator().Validate(new RemoverPresencaManualDraftMontagemRequestDto(Guid.NewGuid(), motivo)).IsValid.Should().Be(expectedValid);
        new RepublicarPublicacaoDiscordDraftMontagemValidator().Validate(new RepublicarPublicacaoDiscordDraftMontagemRequestDto(DraftMontagemPublicacaoDiscordTipo.Presenca, motivo)).IsValid.Should().Be(expectedValid);
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
        var claim = new AdquirirClaimPublicacaoDiscordDraftMontagemValidator().Validate(
            new AdquirirClaimPublicacaoDiscordDraftMontagemRequestDto(tipo!));

        completion.IsValid.Should().BeFalse();
        failure.IsValid.Should().BeFalse();
        claim.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("Presenca")]
    [InlineData("presenca")]
    [InlineData("ChamadaPresenca")]
    [InlineData("chamadapresenca")]
    [InlineData("TimesDefinidos")]
    [InlineData("timesdefinidos")]
    public void PublicacaoDiscord_DeveAceitarTodosOsTiposNominais(string tipo)
    {
        new AdquirirClaimPublicacaoDiscordDraftMontagemValidator().Validate(new AdquirirClaimPublicacaoDiscordDraftMontagemRequestDto(tipo)).IsValid.Should().BeTrue();
        new RegistrarPublicacaoDiscordDraftMontagemValidator().Validate(new RegistrarPublicacaoDiscordDraftMontagemRequestDto(tipo, Guid.NewGuid(), new string('1', 40), new string('2', 40), new string('3', 40))).IsValid.Should().BeTrue();
        new RegistrarFalhaPublicacaoDiscordDraftMontagemValidator().Validate(new RegistrarFalhaPublicacaoDiscordDraftMontagemRequestDto(tipo, Guid.NewGuid(), new string('1', 40), new string('2', 40), new string('e', 120))).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    public void PublicacaoDiscord_DeveRejeitarTiposNumericosMesmoQuandoDefinidos(string tipo)
    {
        new AdquirirClaimPublicacaoDiscordDraftMontagemValidator().Validate(new AdquirirClaimPublicacaoDiscordDraftMontagemRequestDto(tipo)).IsValid.Should().BeFalse();
        new RegistrarPublicacaoDiscordDraftMontagemValidator().Validate(new RegistrarPublicacaoDiscordDraftMontagemRequestDto(tipo, Guid.NewGuid(), "guild", "channel", "message")).IsValid.Should().BeFalse();
        new RegistrarFalhaPublicacaoDiscordDraftMontagemValidator().Validate(new RegistrarFalhaPublicacaoDiscordDraftMontagemRequestDto(tipo, Guid.NewGuid(), "guild", "channel", "erro")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void PublicacaoDiscord_DeveExigirClaimEMessageId()
    {
        var completion = new RegistrarPublicacaoDiscordDraftMontagemValidator().Validate(
            new RegistrarPublicacaoDiscordDraftMontagemRequestDto("Presenca", Guid.Empty, null, null, string.Empty));
        var failure = new RegistrarFalhaPublicacaoDiscordDraftMontagemValidator().Validate(
            new RegistrarFalhaPublicacaoDiscordDraftMontagemRequestDto("Presenca", Guid.Empty, null, null, null));

        completion.Errors.Should().Contain(error => error.PropertyName == "ClaimId" && error.ErrorMessage == MessageCodes.DiscordPublicationClaimInvalid);
        completion.Errors.Should().Contain(error => error.PropertyName == "MessageId" && error.ErrorMessage == MessageCodes.FieldRequired);
        failure.Errors.Should().Contain(error => error.PropertyName == "ClaimId" && error.ErrorMessage == MessageCodes.DiscordPublicationClaimInvalid);
    }

    [Theory]
    [InlineData(40, true)]
    [InlineData(41, false)]
    public void PublicacaoDiscord_DeveRespeitarLimitesDosIds(int length, bool expectedValid)
    {
        var id = new string('1', length);
        var completion = new RegistrarPublicacaoDiscordDraftMontagemValidator().Validate(
            new RegistrarPublicacaoDiscordDraftMontagemRequestDto("Presenca", Guid.NewGuid(), id, id, id));
        var failure = new RegistrarFalhaPublicacaoDiscordDraftMontagemValidator().Validate(
            new RegistrarFalhaPublicacaoDiscordDraftMontagemRequestDto("Presenca", Guid.NewGuid(), id, id, "erro"));

        completion.IsValid.Should().Be(expectedValid);
        failure.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData(120, true)]
    [InlineData(121, false)]
    public void FalhaPublicacaoDiscord_DeveLimitarErroA120Caracteres(int length, bool expectedValid)
    {
        var result = new RegistrarFalhaPublicacaoDiscordDraftMontagemValidator().Validate(
            new RegistrarFalhaPublicacaoDiscordDraftMontagemRequestDto("Presenca", Guid.NewGuid(), "guild", "channel", new string('e', length)));

        result.IsValid.Should().Be(expectedValid);
    }
}
