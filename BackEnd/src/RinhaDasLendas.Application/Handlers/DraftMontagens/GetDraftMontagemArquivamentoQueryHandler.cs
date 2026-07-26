using MediatR;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Queries.DraftMontagens;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class GetDraftMontagemArquivamentoQueryHandler(IDraftMontagemRepository repository)
    : IRequestHandler<GetDraftMontagemArquivamentoQuery, DraftMontagemArquivamentoDto?>
{
    public async Task<DraftMontagemArquivamentoDto?> Handle(GetDraftMontagemArquivamentoQuery query, CancellationToken cancellationToken)
    {
        var montagem = await repository.GetByIdIncludingArchivedAsync(query.Id, cancellationToken);
        return montagem is null ? null : DraftMontagemArquivamentoDto.FromEntity(montagem);
    }
}
