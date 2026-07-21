using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class RegistrarFalhaPublicacaoDiscordDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    IDraftMontagemMetrics metrics) : IRequestHandler<RegistrarFalhaPublicacaoDiscordDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(RegistrarFalhaPublicacaoDiscordDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        var tipo = Enum.Parse<DraftMontagemPublicacaoDiscordTipo>(command.Request.Tipo, true);
        montagem.RegistrarFalhaPublicacaoDiscord(tipo, command.Request.DiscordGuildId ?? montagem.DiscordGuildId, command.Request.DiscordChannelId, command.Request.ErroCodigo);
        await repository.SaveChangesAsync(cancellationToken);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        metrics.RecordDiscordPublication(command.Id, tipo.ToString(), DraftMontagemPublicacaoDiscordStatus.Falha.ToString());
        return DraftMontagemResponseDto.FromEntity(updated);
    }
}
