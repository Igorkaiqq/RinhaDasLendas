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

public sealed class AdicionarPresencaManualDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    IValidator<AdicionarPresencaManualDraftMontagemRequestDto> validator,
    ICurrentUser currentUser,
    IDraftMontagemRealtimeNotifier notifier,
    IDraftMontagemMetrics metrics) : IRequestHandler<AdicionarPresencaManualDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(AdicionarPresencaManualDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        var jogador = (await repository.GetJogadoresByIdsAsync([command.Request.JogadorId], cancellationToken)).SingleOrDefault()
            ?? throw new DomainException(MessageCodes.PlayerNotFound);
        if (jogador.Status != JogadorStatus.Ativo)
        {
            throw new DomainException(MessageCodes.InactivePlayerCannotJoinQueue);
        }

        if (jogador.UsuarioId is not Guid usuarioId)
        {
            throw new DomainException(MessageCodes.PlayerProfileNotFound);
        }

        montagem.AdicionarPresencaManual(usuarioId, jogador.Id);
        await repository.SaveChangesAsync(cancellationToken);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        await notifier.StateUpdatedAsync(command.Id, await DraftMontagemRealtimeStateFactory.CreateAsync(updated, repository, currentUser, DateTimeOffset.UtcNow, cancellationToken), cancellationToken);
        metrics.RecordPresenceConfirmed(command.Id, DraftMontagemPresencaOrigem.Manual.ToString());
        return DraftMontagemResponseDto.FromEntity(updated);
    }
}
