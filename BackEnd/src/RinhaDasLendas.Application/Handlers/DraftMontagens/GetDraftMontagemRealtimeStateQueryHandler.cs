using MediatR;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.DraftMontagens;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class GetDraftMontagemRealtimeStateQueryHandler(
    IDraftMontagemRepository repository,
    ICurrentUser currentUser) : IRequestHandler<GetDraftMontagemRealtimeStateQuery, DraftMontagemRealtimeStateDto?>
{
    public async Task<DraftMontagemRealtimeStateDto?> Handle(GetDraftMontagemRealtimeStateQuery query, CancellationToken cancellationToken)
    {
        var montagem = await repository.GetByIdAsync(query.Id, cancellationToken);
        return montagem is null
            ? null
            : await DraftMontagemRealtimeStateFactory.CreateAsync(montagem, repository, currentUser, DateTimeOffset.UtcNow, cancellationToken);
    }
}
