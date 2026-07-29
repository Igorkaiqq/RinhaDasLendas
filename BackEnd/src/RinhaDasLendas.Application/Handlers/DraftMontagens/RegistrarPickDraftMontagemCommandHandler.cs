using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class RegistrarPickDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    ICurrentUser currentUser,
    IValidator<RegistrarPickDraftMontagemRequestDto> validator,
    IDraftMontagemRealtimeNotifier notifier,
    IDraftMontagemMetrics metrics) : IRequestHandler<RegistrarPickDraftMontagemCommand, DraftMontagemRealtimeStateDto?>
{
    public async Task<DraftMontagemRealtimeStateDto?> Handle(RegistrarPickDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        if (currentUser.UserId is not Guid userId)
        {
            throw new DomainException(MessageCodes.UnauthorizedAccess);
        }

        var capitao = await repository.GetJogadorByUsuarioIdAsync(userId, cancellationToken) ?? throw new DomainException(MessageCodes.PlayerProfileNotFound);
        var now = DateTimeOffset.UtcNow;
        var capitaesElegiveisIds = montagem.CicloVersao == DraftMontagemCicloVersao.ModoPosPresenca
            ? (await repository.GetCapitaesElegiveisIdsAsync(
                montagem.Participantes.Select(participante => participante.JogadorId).ToList(),
                cancellationToken)).ToHashSet()
            : [];
        montagem.RegistrarPickTempoReal(capitao.Id, command.Request.JogadorId, now, capitaesElegiveisIds);
        await repository.SaveChangesAsync(cancellationToken);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        var state = await DraftMontagemRealtimeStateFactory.CreateAsync(updated, repository, currentUser, now, cancellationToken);
        await notifier.StateUpdatedAsync(command.Id, state, cancellationToken);
        metrics.RecordPick(command.Id, "pick");
        return state;
    }
}
