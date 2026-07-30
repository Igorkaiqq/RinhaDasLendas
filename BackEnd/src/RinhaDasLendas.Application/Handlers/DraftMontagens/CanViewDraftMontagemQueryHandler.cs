using MediatR;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.DraftMontagens;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class CanViewDraftMontagemQueryHandler(
    IDraftMontagemRepository repository,
    ICurrentUser currentUser) : IRequestHandler<CanViewDraftMontagemQuery, bool>
{
    public async Task<bool> Handle(CanViewDraftMontagemQuery query, CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue || currentUser.IsBot)
        {
            return false;
        }

        var montagem = await repository.GetByIdAsync(query.Id, cancellationToken);
        return montagem is not null && !montagem.Arquivado;
    }
}
