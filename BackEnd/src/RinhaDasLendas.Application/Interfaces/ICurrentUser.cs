namespace RinhaDasLendas.Application.Interfaces;

public interface ICurrentUser
{
    Guid? UserId { get; }
    IReadOnlyCollection<string> Roles { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}
