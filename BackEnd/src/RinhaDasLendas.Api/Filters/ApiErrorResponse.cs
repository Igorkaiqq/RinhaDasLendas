using RinhaDasLendas.Application.Interfaces;

namespace RinhaDasLendas.Api.Filters;

public sealed record ApiErrorResponse(string Message, IReadOnlyCollection<string> Errors, string? MessageCode = null)
{
    public static ApiErrorResponse FromCode(IMessageProvider messages, string messageCode) => new(messages.GetMessage(messageCode), [], messageCode);
}
