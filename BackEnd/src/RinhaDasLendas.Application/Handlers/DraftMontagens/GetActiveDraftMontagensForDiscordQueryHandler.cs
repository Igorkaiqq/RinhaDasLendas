using MediatR;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Queries.DraftMontagens;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class GetActiveDraftMontagensForDiscordQueryHandler(IDraftMontagemRepository repository) : IRequestHandler<GetActiveDraftMontagensForDiscordQuery, IReadOnlyCollection<DraftMontagemDiscordOperationalDto>>
{
    public async Task<IReadOnlyCollection<DraftMontagemDiscordOperationalDto>> Handle(GetActiveDraftMontagensForDiscordQuery request, CancellationToken cancellationToken)
    {
        var montagens = await repository.ListActiveForDiscordAsync(cancellationToken);
        return montagens.Select(DraftMontagemDiscordOperationalDto.FromEntity).ToList();
    }
}
