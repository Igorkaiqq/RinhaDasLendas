namespace RinhaDasLendas.Application.Dtos;

public sealed record PaginatedResponseDto<T>(
    int Page,
    int PageSize,
    IReadOnlyCollection<T> Items);
