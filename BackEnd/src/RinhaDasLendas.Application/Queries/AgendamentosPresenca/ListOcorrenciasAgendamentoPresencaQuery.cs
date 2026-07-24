using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.AgendamentosPresenca;

public sealed record ListOcorrenciasAgendamentoPresencaQuery(Guid AgendamentoId, int Page = 1, int PageSize = 20)
    : IRequest<PaginatedResponseDto<OcorrenciaAgendamentoPresencaSummaryDto>?>;
