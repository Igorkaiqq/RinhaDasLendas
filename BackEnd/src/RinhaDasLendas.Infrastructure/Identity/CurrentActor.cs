using RinhaDasLendas.Application.Interfaces;

namespace RinhaDasLendas.Infrastructure.Identity;

public sealed class CurrentActor(ICurrentUser currentUser) : ICurrentActor
{
    public Guid? UserId => currentUser.UserId is { } userId && userId != Guid.Empty
        ? userId
        : null;

    public IReadOnlyCollection<string> Roles => UserId.HasValue ? currentUser.Roles : [];
}
