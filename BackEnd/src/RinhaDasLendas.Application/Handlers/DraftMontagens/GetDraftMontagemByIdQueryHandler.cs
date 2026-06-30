using MediatR;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Queries.DraftMontagens;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class GetDraftMontagemByIdQueryHandler(IDraftMontagemRepository repository) : IRequestHandler<GetDraftMontagemByIdQuery, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(GetDraftMontagemByIdQuery query, CancellationToken cancellationToken)
    {
        var montagem = await repository.GetByIdAsync(query.Id, cancellationToken);
        return montagem is null ? null : DraftMontagemResponseDto.FromEntity(montagem);
    }
}
