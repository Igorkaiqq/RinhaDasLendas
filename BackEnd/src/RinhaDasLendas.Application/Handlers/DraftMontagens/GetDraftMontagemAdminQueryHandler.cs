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

        IReadOnlyCollection<Guid> capitaesElegiveisIds = [];
        if (montagem.CicloVersao == DraftMontagemCicloVersao.ModoPosPresenca)
        {
            var titularesIds = montagem.Participantes
                .Where(participante => participante.Estado != DraftMontagemParticipanteEstado.Reserva)
                .Select(participante => participante.JogadorId)
                .ToHashSet();
            capitaesElegiveisIds = (await repository.GetCapitaesElegiveisIdsAsync(titularesIds, cancellationToken))
                .Where(titularesIds.Contains)
                .ToList();
        }

        return DraftMontagemAdminResponseDto.FromEntity(montagem, capitaesElegiveisIds);
    }
}
