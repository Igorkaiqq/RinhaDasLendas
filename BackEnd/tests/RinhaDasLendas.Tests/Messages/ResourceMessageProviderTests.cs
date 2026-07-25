using FluentAssertions;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Infrastructure.Messages;

namespace RinhaDasLendas.Tests.Messages;

public sealed class ResourceMessageProviderTests
{
    [Fact]
    public void GetMessage_WithPortugueseCulture_ReturnsPortugueseText()
    {
        var provider = new ResourceMessageProvider();

        var message = provider.GetMessage(MessageCodes.OperationSuccess, "pt-BR");

        message.Should().Be("Operação realizada com sucesso");
    }

    [Fact]
    public void GetMessage_WithEnglishCulture_ReturnsEnglishText()
    {
        var provider = new ResourceMessageProvider();

        var message = provider.GetMessage(MessageCodes.OperationSuccess, "en-US");

        message.Should().Be("Operation completed successfully");
    }

    [Fact]
    public void GetMessage_WithUnknownCode_ReturnsCodeFallback()
    {
        var provider = new ResourceMessageProvider();

        var message = provider.GetMessage("MX999", "pt-BR");

        message.Should().Be("[MX999]");
    }

    [Fact]
    public void GetMessage_WithInvalidCulture_ReturnsCodeFallback()
    {
        var provider = new ResourceMessageProvider();

        var message = provider.GetMessage(MessageCodes.OperationSuccess, "invalid culture");

        message.Should().Be($"[{MessageCodes.OperationSuccess}]");
    }

    [Theory]
    [InlineData(MessageCodes.DiscordPublicationNotPending, "pt-BR", "A publicação do Discord não está pendente")]
    [InlineData(MessageCodes.DiscordPublicationNotPending, "en-US", "The Discord publication is not pending")]
    [InlineData(MessageCodes.DiscordPublicationClaimMismatch, "pt-BR", "O claim da publicação do Discord é inválido")]
    [InlineData(MessageCodes.DiscordPublicationClaimMismatch, "en-US", "The Discord publication claim is invalid")]
    [InlineData(MessageCodes.DiscordPublicationClaimExpired, "pt-BR", "O claim da publicação do Discord expirou")]
    [InlineData(MessageCodes.DiscordPublicationClaimExpired, "en-US", "The Discord publication claim has expired")]
    [InlineData(MessageCodes.DiscordPublicationClaimInvalid, "pt-BR", "O identificador do claim da publicação do Discord é inválido")]
    [InlineData(MessageCodes.DiscordPublicationClaimInvalid, "en-US", "The Discord publication claim identifier is invalid")]
    [InlineData(MessageCodes.DiscordPublicationClaimExpirationInvalid, "pt-BR", "A expiração do claim da publicação do Discord deve ser futura")]
    [InlineData(MessageCodes.DiscordPublicationClaimExpirationInvalid, "en-US", "The Discord publication claim expiration must be in the future")]
    [InlineData(MessageCodes.DiscordPublicationInProgress, "pt-BR", "A publicação do Discord está em andamento")]
    [InlineData(MessageCodes.DiscordPublicationInProgress, "en-US", "The Discord publication is in progress")]
    [InlineData(MessageCodes.DiscordPublicationStillPublished, "pt-BR", "Confirme a ausência da publicação do Discord antes de republicar")]
    [InlineData(MessageCodes.DiscordPublicationStillPublished, "en-US", "Confirm that the Discord publication is absent before republishing")]
    [InlineData(MessageCodes.DiscordPublicationRequiresReconciliation, "pt-BR", "A publicação do Discord requer reconciliação administrativa")]
    [InlineData(MessageCodes.DiscordPublicationRequiresReconciliation, "en-US", "The Discord publication requires administrative reconciliation")]
    public void GetMessage_WithDiscordPublicationClaimCodes_ReturnsLocalizedText(string code, string culture, string expected)
    {
        var provider = new ResourceMessageProvider();

        var message = provider.GetMessage(code, culture);

        message.Should().Be(expected);
    }
}
