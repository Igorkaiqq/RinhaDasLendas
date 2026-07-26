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
    IDraftMontagemMetrics metrics,
    IDraftMontagemRealtimeNotifier notifier) : IRequestHandler<RegistrarFalhaPublicacaoDiscordDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(RegistrarFalhaPublicacaoDiscordDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        if (!DraftMontagemPublicacaoDiscordTipoParser.TryParse(command.Request.Tipo, out var tipo))
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
            var expirados = await repository.MarcarPublicacoesExpiradasParaReconciliacaoAsync(agora, cancellationToken);
            await DraftMontagemRealtimeNotificationPublisher.PublishReloadedAsync(expirados, repository, notifier, cancellationToken);
            var existente = tipo == DraftMontagemPublicacaoDiscordTipo.Cancelamento
                ? await repository.GetByIdIncludingArchivedAsync(command.Id, cancellationToken)
                : await repository.GetByIdAsync(command.Id, cancellationToken);
            if (existente is null)
            {
                return null;
            }

            throw new DomainException(MessageCodes.DiscordPublicationClaimMismatch);
        }

        var montagem = tipo == DraftMontagemPublicacaoDiscordTipo.Cancelamento
            ? await repository.ReloadByIdIncludingArchivedAsync(command.Id, cancellationToken)
            : await repository.ReloadByIdAsync(command.Id, cancellationToken);
        metrics.RecordDiscordPublication(command.Id, tipo.ToString(), DraftMontagemPublicacaoDiscordStatus.Falha.ToString());
        if (montagem is not null && !montagem.Arquivado)
        {
            await notifier.StateUpdatedAsync(
                command.Id,
                DraftMontagemRealtimeStateFactory.Create(montagem, DateTimeOffset.UtcNow),
                cancellationToken);
        }

        return montagem is null ? null : DraftMontagemResponseDto.FromEntity(montagem);
    }
}
