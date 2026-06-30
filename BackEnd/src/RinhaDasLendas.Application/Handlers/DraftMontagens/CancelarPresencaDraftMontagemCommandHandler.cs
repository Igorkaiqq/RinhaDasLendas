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
    IDiscordIdentityLookupService discordIdentityLookup) : IRequestHandler<CancelarPresencaDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(CancelarPresencaDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        var usuarioId = await ResolveUserIdAsync(command.Request, cancellationToken);
        montagem.CancelarPresenca(usuarioId);
        await repository.SaveChangesAsync(cancellationToken);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
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
