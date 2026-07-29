namespace RinhaDasLendas.Application.Interfaces;

public interface ICurrentUser
{
    Guid? UserId { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsBot => false;
    string? IpAddress { get; }
    string? UserAgent { get; }
}
