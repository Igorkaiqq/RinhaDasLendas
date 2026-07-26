using MediatR;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Queries.DraftMontagens;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class GetDraftMontagemDiscordOperationalQueryHandler(IDraftMontagemRepository repository) : IRequestHandler<GetDraftMontagemDiscordOperationalQuery, DraftMontagemDiscordOperationalDto?>
{
    public async Task<DraftMontagemDiscordOperationalDto?> Handle(GetDraftMontagemDiscordOperationalQuery query, CancellationToken cancellationToken)
    {
        var montagem = await repository.GetByIdIncludingArchivedAsync(query.Id, cancellationToken);
        return montagem is null ? null : DraftMontagemDiscordOperationalDto.FromEntity(montagem);
    }
}
