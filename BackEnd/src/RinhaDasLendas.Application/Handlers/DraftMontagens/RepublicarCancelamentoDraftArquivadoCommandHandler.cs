using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class RepublicarCancelamentoDraftArquivadoCommandHandler(
    IDraftMontagemRepository repository,
    ICurrentUser currentUser)
    : IRequestHandler<RepublicarCancelamentoDraftArquivadoCommand, DraftMontagemArquivamentoResultadoDto?>
{
    public async Task<DraftMontagemArquivamentoResultadoDto?> Handle(RepublicarCancelamentoDraftArquivadoCommand command, CancellationToken cancellationToken)
    {
        var userId = DraftMontagemHandlerHelpers.ResolveRequiredCurrentUserId(currentUser);
        var montagem = await repository.GetByIdIncludingArchivedAsync(command.Id, cancellationToken);
        if (montagem is null
            || montagem.AcoesAdministrativas.All(item => item.Tipo != "CancelamentoPorArquivamento")
            || montagem.PublicacoesDiscord.All(item => item.Tipo != DraftMontagemPublicacaoDiscordTipo.Cancelamento))
        {
            return null;
        }

        montagem.SolicitarRepublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Cancelamento, userId, null, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return DraftMontagemArquivamentoResultadoDto.FromEntity(montagem);
    }
}
