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

public sealed class CancelarPresencaDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    ICurrentUser currentUser,
    IDiscordIdentityLookupService discordIdentityLookup,
    IDraftMontagemRealtimeNotifier notifier,
    IDraftMontagemMetrics metrics) : IRequestHandler<CancelarPresencaDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(CancelarPresencaDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        var usuarioId = await ResolveUserIdAsync(command.Request, cancellationToken);
        var versaoAnterior = montagem.VersaoEstado;
        montagem.CancelarPresenca(usuarioId);
        if (montagem.VersaoEstado == versaoAnterior)
        {
            return DraftMontagemResponseDto.FromEntity(montagem);
        }

        var saveResult = await repository.TrySaveChangesAsync(cancellationToken);
        if (saveResult != DraftMontagemSaveResultado.Persistido)
        {
            var reloaded = await repository.ReloadByIdAsync(command.Id, cancellationToken);
            if (reloaded is null)
            {
                return null;
            }

            var presencasDoUsuario = reloaded.Presencas.Where(presenca => presenca.UsuarioId == usuarioId).ToList();
            if (presencasDoUsuario.Count > 0 && presencasDoUsuario.All(presenca => !presenca.Confirmada))
            {
                return DraftMontagemResponseDto.FromEntity(reloaded);
            }

            throw new DomainException(MessageCodes.PresencePersistenceConflict);
        }

        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        await notifier.StateUpdatedAsync(command.Id, await DraftMontagemRealtimeStateFactory.CreateAsync(updated, repository, currentUser, DateTimeOffset.UtcNow, cancellationToken), cancellationToken);
        metrics.RecordPresenceCancelled(command.Id, string.IsNullOrWhiteSpace(command.Request.DiscordUserId) ? "Web" : "Discord");
        return DraftMontagemResponseDto.FromEntity(updated);
    }

    private async Task<Guid> ResolveUserIdAsync(CancelarPresencaDraftMontagemRequestDto request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is Guid currentUserId)
        {
            return currentUserId;
        }

        if (!string.IsNullOrWhiteSpace(request.DiscordUserId))
        {
            var linked = await discordIdentityLookup.GetByDiscordUserIdAsync(request.DiscordUserId, cancellationToken);
            if (linked.UsuarioId is null)
            {
                throw new DomainException(MessageCodes.DiscordAccountNotLinked);
            }

            return linked.UsuarioId.Value;
        }

        return request.UsuarioId ?? throw new DomainException(MessageCodes.UnauthorizedAccess);
    }
}
