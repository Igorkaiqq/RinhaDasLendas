using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class CreateDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    IValidator<CreateDraftMontagemRequestDto> validator) : IRequestHandler<CreateDraftMontagemCommand, DraftMontagemResponseDto>
{
    public async Task<DraftMontagemResponseDto> Handle(CreateDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var jogadoresIds = command.Request.JogadoresIds.ToList();
        var jogadores = await repository.GetJogadoresByIdsAsync(jogadoresIds, cancellationToken);
        DraftMontagemHandlerHelpers.EnsureActivePlayers(jogadores, jogadoresIds);
        var capitaes = DraftMontagemHandlerHelpers.ResolveCapitaes(command.Request);

        var montagem = new DraftMontagem(
            command.Request.Nome,
            command.Request.Observacoes,
            command.Request.TamanhoEquipe,
            command.Request.SortearCapitaes ? DraftMontagemCriterioCapitaes.Sorteio : DraftMontagemCriterioCapitaes.Manual,
            jogadoresIds,
            capitaes);
        montagem.ConfigurarEncerramentoPresenca(command.Request.HorarioEncerramentoPresenca);
        montagem.ConfigurarPublicacaoDiscord(command.Request.DiscordGuildId, null);

        await repository.AddAsync(montagem, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        var created = await repository.GetByIdAsync(montagem.Id, cancellationToken) ?? montagem;
        return DraftMontagemResponseDto.FromEntity(created);
    }
}
