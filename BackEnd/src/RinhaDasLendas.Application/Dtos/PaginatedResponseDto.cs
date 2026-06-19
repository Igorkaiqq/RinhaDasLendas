namespace RinhaDasLendas.Application.Dtos;

public sealed record PaginatedResponseDto<T>(
    int Page,
    int PageSize,
    IReadOnlyCollection<T> Items,
    int TotalItems = 0,
    int TotalPages = 0);
