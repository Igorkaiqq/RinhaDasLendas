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
}
