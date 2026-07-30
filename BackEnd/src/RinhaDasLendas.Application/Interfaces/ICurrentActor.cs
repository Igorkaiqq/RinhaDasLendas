namespace RinhaDasLendas.Application.Interfaces;

public interface ICurrentActor
{
    Guid? UserId { get; }
    IReadOnlyCollection<string> Roles { get; }
}
