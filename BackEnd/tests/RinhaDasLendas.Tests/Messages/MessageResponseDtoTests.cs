using FluentAssertions;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Tests.Messages;

public sealed class MessageResponseDtoTests
{
    [Fact]
    public void Constructor_ShouldExposeMessageFieldsDataAndDetails()
    {
        var data = new { Id = Guid.NewGuid(), Nome = "Faker da Firma" };
        var details = new Dictionary<string, string[]>
        {
            ["nome"] = ["Campo obrigatório"]
        };

        var response = new MessageResponseDto<object>(
            MessageCodes.OperationSuccess,
            "Operação realizada com sucesso",
            MessageCategory.Success,
            data,
            details);

        response.MessageCode.Should().Be(MessageCodes.OperationSuccess);
        response.Message.Should().Be("Operação realizada com sucesso");
        response.MessageCategory.Should().Be(MessageCategory.Success);
        response.Data.Should().BeSameAs(data);
        response.Details.Should().BeSameAs(details);
    }

    [Fact]
    public void Constructor_WithNoDetails_ShouldKeepDetailsNull()
    {
        var response = new MessageResponseDto<object>(
            MessageCodes.FieldRequired,
            "Campo obrigatório",
            MessageCategory.Validation,
            Data: null);

        response.MessageCode.Should().Be(MessageCodes.FieldRequired);
        response.MessageCategory.Should().Be(MessageCategory.Validation);
        response.Data.Should().BeNull();
        response.Details.Should().BeNull();
    }
}
