namespace RinhaDasLendas.Domain.Exceptions;

public sealed class DomainException(string messageCode) : Exception(messageCode)
{
    public string MessageCode { get; } = messageCode;
}
