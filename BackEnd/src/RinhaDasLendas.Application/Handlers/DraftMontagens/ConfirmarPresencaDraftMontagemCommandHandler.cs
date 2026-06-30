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

public sealed class ConfirmarPresencaDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    ICurrentUser currentUser,
    IDiscordIdentityLookupService discordIdentityLookup,
    IValidator<ConfirmarPresencaDraftMontagemRequestDto> validator) : IRequestHandler<ConfirmarPresencaDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(ConfirmarPresencaDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        var origem = Enum.Parse<DraftMontagemPresencaOrigem>(command.Request.Origem, true);
        var (usuarioId, jogadorId, discordUserId) = await ResolveIdentityAsync(command.Request.UsuarioId, command.Request.DiscordUserId, origem, cancellationToken);
        montagem.ConfirmarPresenca(usuarioId, jogadorId, discordUserId, origem);
        await repository.SaveChangesAsync(cancellationToken);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        return DraftMontagemResponseDto.FromEntity(updated);
    }

    private async Task<(Guid UsuarioId, Guid JogadorId, string? DiscordUserId)> ResolveIdentityAsync(Guid? usuarioId, string? discordUserId, DraftMontagemPresencaOrigem origem, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is Guid currentUserId)
        {
            var currentUserJogador = await repository.GetJogadorByUsuarioIdAsync(currentUserId, cancellationToken) ?? throw new DomainException(MessageCodes.PlayerProfileNotFound);
            return (currentUserId, currentUserJogador.Id, null);
        }

        if (origem == DraftMontagemPresencaOrigem.Discord)
        {
            if (string.IsNullOrWhiteSpace(discordUserId))
            {
                throw new DomainException(MessageCodes.DiscordAccountNotLinked);
            }

            var linked = await discordIdentityLookup.GetByDiscordUserIdAsync(discordUserId, cancellationToken);
            if (!linked.Vinculado || linked.UsuarioId is null || linked.JogadorId is null)
            {
                throw new DomainException(MessageCodes.DiscordAccountNotLinked);
            }

            return (linked.UsuarioId.Value, linked.JogadorId.Value, discordUserId);
        }

        var resolvedUserId = usuarioId ?? throw new DomainException(MessageCodes.UnauthorizedAccess);
        var jogador = await repository.GetJogadorByUsuarioIdAsync(resolvedUserId, cancellationToken) ?? throw new DomainException(MessageCodes.PlayerProfileNotFound);
        return (resolvedUserId, jogador.Id, discordUserId);
    }
}
