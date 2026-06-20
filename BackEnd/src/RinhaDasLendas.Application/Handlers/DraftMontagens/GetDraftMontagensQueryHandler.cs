using MediatR;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Queries.DraftMontagens;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class GetDraftMontagensQueryHandler(IDraftMontagemRepository repository) : IRequestHandler<GetDraftMontagensQuery, PaginatedResponseDto<DraftMontagemResumoDto>>
{
    public async Task<PaginatedResponseDto<DraftMontagemResumoDto>> Handle(GetDraftMontagensQuery query, CancellationToken cancellationToken)
    {
        var status = Enum.TryParse<DraftMontagemStatus>(query.Status, true, out var parsed) ? parsed : (DraftMontagemStatus?)null;
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var montagens = await repository.ListAsync(query.Search, status, page, pageSize, cancellationToken);
        var total = await repository.CountAsync(query.Search, status, cancellationToken);

        return new PaginatedResponseDto<DraftMontagemResumoDto>(
            page,
            pageSize,
            montagens.Select(montagem => new DraftMontagemResumoDto(
                montagem.Id,
                montagem.Nome,
                montagem.Status.ToString(),
                montagem.TamanhoEquipe,
                montagem.QuantidadeTimes,
                montagem.QuantidadeReservas,
                montagem.DataCadastro,
                montagem.DataAtualizacao)).ToList(),
            total,
            (int)Math.Ceiling(total / (double)pageSize));
    }
}

public sealed class GetDraftMontagemByIdQueryHandler(IDraftMontagemRepository repository) : IRequestHandler<GetDraftMontagemByIdQuery, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(GetDraftMontagemByIdQuery query, CancellationToken cancellationToken)
    {
        var montagem = await repository.GetByIdAsync(query.Id, cancellationToken);
        return montagem is null ? null : DraftMontagemResponseDto.FromEntity(montagem);
    }
}
