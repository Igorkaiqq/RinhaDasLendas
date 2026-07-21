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

public sealed class RegistrarPublicacaoDiscordDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    IDraftMontagemMetrics metrics) : IRequestHandler<RegistrarPublicacaoDiscordDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(RegistrarPublicacaoDiscordDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        var tipo = string.IsNullOrWhiteSpace(command.Request.Tipo)
            ? DraftMontagemPublicacaoDiscordTipo.Presenca
            : Enum.Parse<DraftMontagemPublicacaoDiscordTipo>(command.Request.Tipo, true);
        montagem.RegistrarPublicacaoDiscord(tipo, command.Request.DiscordGuildId ?? montagem.DiscordGuildId, command.Request.DiscordChannelId, command.Request.DiscordPresenceMessageId);
        await repository.SaveChangesAsync(cancellationToken);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        metrics.RecordDiscordPublication(command.Id, tipo.ToString(), DraftMontagemPublicacaoDiscordStatus.Publicada.ToString());
        return DraftMontagemResponseDto.FromEntity(updated);
    }
}
