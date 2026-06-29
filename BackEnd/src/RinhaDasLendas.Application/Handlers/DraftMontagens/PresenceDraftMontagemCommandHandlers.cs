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

        var resolvedUserId = usuarioId ?? currentUser.UserId ?? throw new DomainException(MessageCodes.UnauthorizedAccess);
        var jogador = await repository.GetJogadorByUsuarioIdAsync(resolvedUserId, cancellationToken) ?? throw new DomainException(MessageCodes.PlayerProfileNotFound);
        return (resolvedUserId, jogador.Id, discordUserId);
    }
}

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
        if (!string.IsNullOrWhiteSpace(request.DiscordUserId))
        {
            var linked = await discordIdentityLookup.GetByDiscordUserIdAsync(request.DiscordUserId, cancellationToken);
            if (linked.UsuarioId is null)
            {
                throw new DomainException(MessageCodes.DiscordAccountNotLinked);
            }

            return linked.UsuarioId.Value;
        }

        return request.UsuarioId ?? currentUser.UserId ?? throw new DomainException(MessageCodes.UnauthorizedAccess);
    }
}

public sealed class EncerrarPresencaDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    IValidator<EncerrarPresencaDraftMontagemRequestDto> validator) : IRequestHandler<EncerrarPresencaDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(EncerrarPresencaDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        montagem.EncerrarPresenca(command.Request.ContinuarComMenosDez, command.Request.TamanhoEquipe);
        await repository.SaveChangesAsync(cancellationToken);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        return DraftMontagemResponseDto.FromEntity(updated);
    }
}

public sealed class DefinirCapitaesDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    IValidator<DefinirCapitaesDraftMontagemRequestDto> validator) : IRequestHandler<DefinirCapitaesDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(DefinirCapitaesDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        montagem.DefinirCapitaes(command.Request.CapitaesIds);
        await repository.SaveChangesAsync(cancellationToken);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        return DraftMontagemResponseDto.FromEntity(updated);
    }
}

public sealed class DefinirOrdemEscolhaDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    IValidator<DefinirOrdemEscolhaDraftMontagemRequestDto> validator) : IRequestHandler<DefinirOrdemEscolhaDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(DefinirOrdemEscolhaDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        var modo = Enum.Parse<DraftMontagemOrdemEscolhaModo>(command.Request.Modo, true);
        montagem.DefinirOrdemEscolha(modo, command.Request.CapitaesIds);
        await repository.SaveChangesAsync(cancellationToken);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        return DraftMontagemResponseDto.FromEntity(updated);
    }
}

public sealed class RegistrarPublicacaoDiscordDraftMontagemCommandHandler(
    IDraftMontagemRepository repository) : IRequestHandler<RegistrarPublicacaoDiscordDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(RegistrarPublicacaoDiscordDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        montagem.ConfigurarPublicacaoDiscord(command.Request.DiscordGuildId ?? montagem.DiscordGuildId, command.Request.DiscordPresenceMessageId);
        await repository.SaveChangesAsync(cancellationToken);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        return DraftMontagemResponseDto.FromEntity(updated);
    }
}
