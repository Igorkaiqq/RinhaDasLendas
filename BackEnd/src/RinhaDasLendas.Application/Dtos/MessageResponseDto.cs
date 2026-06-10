using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record MessageResponseDto<TData>(
    string MessageCode,
    string Message,
    MessageCategory MessageCategory,
    TData? Data = default,
    object? Details = null);
