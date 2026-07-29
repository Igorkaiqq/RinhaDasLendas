using MediatR;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Queries.DraftMontagens;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class GetDraftMontagemAdminQueryHandler(IDraftMontagemRepository repository) : IRequestHandler<GetDraftMontagemAdminQuery, DraftMontagemAdminResponseDto?>
{
    public async Task<DraftMontagemAdminResponseDto?> Handle(GetDraftMontagemAdminQuery query, CancellationToken cancellationToken)
    {
        var montagem = await repository.GetByIdAsync(query.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        var capitaesElegiveisIds = montagem.CicloVersao == DraftMontagemCicloVersao.ModoPosPresenca
            ? await repository.GetCapitaesElegiveisIdsAsync(
                montagem.Participantes.Select(participante => participante.JogadorId).ToList(),
                cancellationToken)
            : [];
        return DraftMontagemAdminResponseDto.FromEntity(montagem, capitaesElegiveisIds);
    }
}
