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

public sealed class RegistrarFalhaPublicacaoDiscordDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    IValidator<RegistrarFalhaPublicacaoDiscordDraftMontagemRequestDto> validator,
    IDraftMontagemMetrics metrics) : IRequestHandler<RegistrarFalhaPublicacaoDiscordDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(RegistrarFalhaPublicacaoDiscordDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        if (!Enum.TryParse<DraftMontagemPublicacaoDiscordTipo>(command.Request.Tipo, true, out var tipo) || !Enum.IsDefined(tipo))
        {
            throw new DomainException(MessageCodes.FieldRequired);
        }
        var agora = DateTimeOffset.UtcNow;
        var updated = await repository.TryRegistrarFalhaPublicacaoDiscordAsync(
            command.Id,
            tipo,
            command.Request.ClaimId,
            command.Request.DiscordGuildId,
            command.Request.DiscordChannelId,
            command.Request.ErroCodigo,
            agora,
            cancellationToken);
        if (!updated)
        {
            await repository.MarcarPublicacoesExpiradasParaReconciliacaoAsync(agora, cancellationToken);
            if (await repository.GetByIdAsync(command.Id, cancellationToken) is null)
            {
                return null;
            }

            throw new DomainException(MessageCodes.DiscordPublicationClaimMismatch);
        }

        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        metrics.RecordDiscordPublication(command.Id, tipo.ToString(), DraftMontagemPublicacaoDiscordStatus.Falha.ToString());
        return montagem is null ? null : DraftMontagemResponseDto.FromEntity(montagem);
    }
}
