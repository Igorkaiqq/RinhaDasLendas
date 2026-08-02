using System.Text.Json.Serialization;
using RinhaDasLendas.Application.Interfaces;

namespace RinhaDasLendas.Api.Filters;

public sealed record ApiErrorResponse(string Message, IReadOnlyCollection<string> Errors, string? MessageCode = null)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<ApiFieldError>? FieldErrors { get; init; }

    public static ApiErrorResponse FromCode(IMessageProvider messages, string messageCode) => new(messages.GetMessage(messageCode), [], messageCode);
}

public sealed record ApiFieldError(string Field, string MessageCode, string Message);
