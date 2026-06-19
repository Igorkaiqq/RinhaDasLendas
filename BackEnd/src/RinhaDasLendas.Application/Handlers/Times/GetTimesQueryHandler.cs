using MediatR;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Queries.Times;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.Times;

public sealed class GetTimesQueryHandler(ITimeRepository timeRepository)
    : IRequestHandler<GetTimesQuery, PaginatedResponseDto<TimeResponseDto>>,
        IRequestHandler<GetTimeByIdQuery, TimeResponseDto?>
{
    public async Task<PaginatedResponseDto<TimeResponseDto>> Handle(GetTimesQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var status = Enum.TryParse<TimeStatus>(query.Status, ignoreCase: true, out var parsedStatus)
            ? parsedStatus
            : (TimeStatus?)null;
        var totalItems = await timeRepository.CountAsync(query.Search, status, cancellationToken);
        var times = await timeRepository.ListAsync(query.Search, status, page, pageSize, cancellationToken);
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        return new PaginatedResponseDto<TimeResponseDto>(
            page,
            pageSize,
            times.Select(TimeResponseDto.FromEntity).ToList(),
            totalItems,
            totalPages);
    }

    public async Task<TimeResponseDto?> Handle(GetTimeByIdQuery query, CancellationToken cancellationToken)
    {
        var time = await timeRepository.GetByIdAsync(query.TimeId, cancellationToken);
        return time is null ? null : TimeResponseDto.FromEntity(time);
    }
}
