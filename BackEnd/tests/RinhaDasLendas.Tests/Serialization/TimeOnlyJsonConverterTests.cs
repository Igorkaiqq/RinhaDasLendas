using System.Text.Json;
using FluentAssertions;
using RinhaDasLendas.Api.Serialization;

namespace RinhaDasLendas.Tests.Serialization;

public sealed class TimeOnlyJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    [Fact]
    public void Deserialize_ShouldAcceptExactHourAndMinuteString()
    {
        JsonSerializer.Deserialize<TimeOnly>("\"18:00\"", Options).Should().Be(new TimeOnly(18, 0));
    }

    [Theory]
    [InlineData("18")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("\"18:00:00\"")]
    [InlineData("\"8:00\"")]
    public void Deserialize_ShouldRejectAnythingExceptExactHourAndMinuteString(string json)
    {
        var action = () => JsonSerializer.Deserialize<TimeOnly>(json, Options);

        action.Should().Throw<JsonException>();
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new TimeOnlyJsonConverter());
        return options;
    }
}
