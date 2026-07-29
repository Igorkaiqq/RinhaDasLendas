using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public static class DraftMontagemRealtimeStateFactory
{
    public static async Task<DraftMontagemRealtimeStateDto> CreateAsync(
        DraftMontagem montagem,
        IDraftMontagemRepository repository,
        ICurrentUser currentUser,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var jogador = currentUser.UserId is Guid userId
            ? await repository.GetJogadorByUsuarioIdAsync(userId, cancellationToken)
            : null;

        var canPick = jogador is not null && montagem.TurnoAtualCapitaoId == jogador.Id;
        if (canPick && montagem.CicloVersao == DraftMontagemCicloVersao.ModoPosPresenca)
        {
            var turnoValido = montagem.Status == DraftMontagemStatus.Aberta
                && montagem.Modo == DraftMontagemModo.TempoReal
                && montagem.TurnoAtualTimeId is Guid timeId
                && montagem.TurnoSequencia is not null
                && montagem.TurnoIniciadoEm <= now
                && montagem.TurnoExpiraEm > now
                && montagem.Times.Any(time => time.Id == timeId && time.CapitaoId == jogador!.Id)
                && montagem.Participantes.Any(participante => participante.JogadorId == jogador!.Id
                    && participante.TimeId == timeId
                    && participante.Estado == DraftMontagemParticipanteEstado.Time
                    && participante.Capitao);
            if (!turnoValido)
            {
                canPick = false;
            }
            else
            {
                var elegiveis = await repository.GetCapitaesElegiveisIdsAsync([jogador!.Id], cancellationToken);
                canPick = elegiveis.Contains(jogador.Id);
            }
        }
        return Create(montagem, now, canPick);
    }

    public static DraftMontagemRealtimeStateDto Create(DraftMontagem montagem, DateTimeOffset now, bool canCurrentUserPick = false)
    {
        return new DraftMontagemRealtimeStateDto(DraftMontagemResponseDto.FromEntity(montagem), now, canCurrentUserPick);
    }
}
