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
        IReadOnlyCollection<Guid> capitaesElegiveisSubstituicaoIds = [];
        if (montagem.CicloVersao == DraftMontagemCicloVersao.ModoPosPresenca)
        {
            var participantesIds = montagem.Participantes
                .Select(participante => participante.JogadorId)
                .ToHashSet();
            var titularesIds = montagem.Participantes
                .Where(participante => participante.Estado != DraftMontagemParticipanteEstado.Reserva)
                .Select(participante => participante.JogadorId)
                .ToHashSet();
            capitaesElegiveisSubstituicaoIds = (await repository.GetCapitaesElegiveisIdsAsync(participantesIds, cancellationToken))
                .Where(participantesIds.Contains)
                .ToList();
            capitaesElegiveisIds = capitaesElegiveisSubstituicaoIds
                .Where(titularesIds.Contains)
                .ToList();
        }

        return DraftMontagemAdminResponseDto.FromEntity(montagem, capitaesElegiveisIds, capitaesElegiveisSubstituicaoIds);
    }
}
