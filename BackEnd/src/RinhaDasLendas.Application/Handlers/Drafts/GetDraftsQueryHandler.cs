using MediatR;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Queries.Drafts;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.Drafts;

public sealed class GetDraftsQueryHandler(IDraftRepository draftRepository)
    : IRequestHandler<GetDraftsQuery, PaginatedResponseDto<DraftResponseDto>>,
        IRequestHandler<GetDraftByIdQuery, DraftResponseDto?>
{
    public async Task<PaginatedResponseDto<DraftResponseDto>> Handle(GetDraftsQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var status = Enum.TryParse<DraftStatus>(query.Status, ignoreCase: true, out var parsedStatus) ? parsedStatus : (DraftStatus?)null;
        var totalItems = await draftRepository.CountAsync(query.Search, status, cancellationToken);
        var drafts = await draftRepository.ListAsync(query.Search, status, page, pageSize, cancellationToken);
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        return new PaginatedResponseDto<DraftResponseDto>(
            page,
            pageSize,
            drafts.Select(DraftResponseDto.FromEntity).ToList(),
            totalItems,
            totalPages);
    }

    public async Task<DraftResponseDto?> Handle(GetDraftByIdQuery query, CancellationToken cancellationToken)
    {
        var draft = await draftRepository.GetByIdAsync(query.DraftId, cancellationToken);
        return draft is null ? null : DraftResponseDto.FromEntity(draft);
    }
}
