using MediatR;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Queries.DraftMontagens;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class GetJogadoresElegiveisPresencaDraftMontagemQueryHandler(IDraftMontagemRepository repository) : IRequestHandler<GetJogadoresElegiveisPresencaDraftMontagemQuery, PaginatedResponseDto<DraftMontagemJogadorElegivelPresencaDto>>
{
    public async Task<PaginatedResponseDto<DraftMontagemJogadorElegivelPresencaDto>> Handle(GetJogadoresElegiveisPresencaDraftMontagemQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var jogadores = await repository.SearchJogadoresElegiveisParaPresencaManualAsync(query.DraftMontagemId, query.Search, page, pageSize, cancellationToken);
        var total = await repository.CountJogadoresElegiveisParaPresencaManualAsync(query.DraftMontagemId, query.Search, cancellationToken);

        return new PaginatedResponseDto<DraftMontagemJogadorElegivelPresencaDto>(
            page,
            pageSize,
            jogadores.Select(jogador => new DraftMontagemJogadorElegivelPresencaDto(jogador.Id, jogador.NomeExibicao)).ToList(),
            total,
            (int)Math.Ceiling(total / (double)pageSize));
    }
}
