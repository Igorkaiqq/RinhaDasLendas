using MediatR;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Queries.Jogadores;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.Jogadores;

public sealed class GetJogadoresQueryHandler(IJogadorRepository jogadorRepository)
    : IRequestHandler<GetJogadoresQuery, PaginatedResponseDto<JogadorResponseDto>>,
        IRequestHandler<GetJogadorByIdQuery, JogadorResponseDto?>
{
    public async Task<PaginatedResponseDto<JogadorResponseDto>> Handle(GetJogadoresQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var jogadores = await jogadorRepository.ListAsync(query.SomenteAtivos, page, pageSize, cancellationToken);

        return new PaginatedResponseDto<JogadorResponseDto>(
            page,
            pageSize,
            jogadores.Select(JogadorResponseDto.FromEntity).ToList());
    }

    public async Task<JogadorResponseDto?> Handle(GetJogadorByIdQuery query, CancellationToken cancellationToken)
    {
        var jogador = await jogadorRepository.GetByIdAsync(query.JogadorId, cancellationToken);
        return jogador is null ? null : JogadorResponseDto.FromEntity(jogador);
    }
}
