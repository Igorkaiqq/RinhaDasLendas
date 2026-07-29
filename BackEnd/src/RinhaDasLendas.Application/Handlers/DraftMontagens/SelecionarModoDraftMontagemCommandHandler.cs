using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class SelecionarModoDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    IValidator<SelecionarModoDraftMontagemRequestDto> validator)
    : IRequestHandler<SelecionarModoDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(
        SelecionarModoDraftMontagemCommand command,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        var modo = Enum.Parse<DraftMontagemModo>(command.Request.Modo, true);
        if (montagem.CicloVersao == DraftMontagemCicloVersao.ModoPosPresenca && montagem.Modo == modo)
        {
            return DraftMontagemResponseDto.FromEntity(montagem);
        }

        var jogadoresIds = montagem.Presencas
            .Where(item => item.Confirmada)
            .Select(item => item.JogadorId)
            .ToList();
        var jogadores = await repository.GetJogadoresByIdsAsync(jogadoresIds, cancellationToken);
        DraftMontagemHandlerHelpers.EnsureActivePlayers(jogadores, jogadoresIds);
        var versaoAnterior = montagem.VersaoEstado;
        montagem.SelecionarModo(
            modo,
            jogadores.Select(item => item.Id).ToHashSet());
        if (montagem.VersaoEstado != versaoAnterior)
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        return DraftMontagemResponseDto.FromEntity(updated);
    }
}
