using RinhaDasLendas.Application.Security;

namespace RinhaDasLendas.Application.Interfaces;

public interface ICompetitiveAuthorizationService
{
    Task<bool> AuthorizeAsync(
        CompetitiveAuthorizationContext context,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<string>> GetAllowedActionsAsync(
        IReadOnlyCollection<CompetitiveAuthorizationContext> actions,
        CancellationToken cancellationToken);
}
