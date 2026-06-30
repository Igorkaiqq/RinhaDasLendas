using MediatR;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Queries.DraftMontagens;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class GetDraftMontagensQueryHandler(IDraftMontagemRepository repository) : IRequestHandler<GetDraftMontagensQuery, PaginatedResponseDto<DraftMontagemResumoDto>>
{
    public async Task<PaginatedResponseDto<DraftMontagemResumoDto>> Handle(GetDraftMontagensQuery query, CancellationToken cancellationToken)
    {
        var status = Enum.TryParse<DraftMontagemStatus>(query.Status, true, out var parsed) ? parsed : (DraftMontagemStatus?)null;
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var montagens = await repository.ListAsync(query.Search, status, page, pageSize, cancellationToken);
        var total = await repository.CountAsync(query.Search, status, cancellationToken);

        return new PaginatedResponseDto<DraftMontagemResumoDto>(
            page,
            pageSize,
            montagens.Select(montagem => new DraftMontagemResumoDto(
                montagem.Id,
                montagem.Nome,
                montagem.Status.ToString(),
                montagem.Modo.ToString(),
                montagem.TamanhoEquipe,
                montagem.QuantidadeTimes,
                montagem.QuantidadeReservas,
                montagem.HorarioEncerramentoPresenca,
                montagem.DiscordGuildId,
                montagem.DiscordPresenceMessageId,
                montagem.OrdemEscolhaModo?.ToString(),
                montagem.PresencaContinuadaManualmente,
                montagem.DataCadastro,
                montagem.DataAtualizacao)).ToList(),
            total,
            (int)Math.Ceiling(total / (double)pageSize));
    }
}
