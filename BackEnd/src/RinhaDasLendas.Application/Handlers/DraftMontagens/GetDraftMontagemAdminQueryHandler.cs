using MediatR;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Queries.DraftMontagens;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class GetDraftMontagemAdminQueryHandler(IDraftMontagemRepository repository) : IRequestHandler<GetDraftMontagemAdminQuery, DraftMontagemAdminResponseDto?>
{
    public async Task<DraftMontagemAdminResponseDto?> Handle(GetDraftMontagemAdminQuery query, CancellationToken cancellationToken)
    {
        var montagem = await repository.GetByIdAsync(query.Id, cancellationToken);
        return montagem is null ? null : DraftMontagemAdminResponseDto.FromEntity(montagem);
    }
}
