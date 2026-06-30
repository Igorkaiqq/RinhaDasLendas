using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DraftMontagemSubstituicaoResponseDto(
    Guid TimeId,
    Guid JogadorSaiuId,
    Guid ReservaEntrouId,
    string? JogadorSaiuNome,
    string? ReservaEntrouNome,
    string? Motivo,
    Guid ResponsavelUsuarioId,
    DateTimeOffset RegistradoEm)
{
    public static DraftMontagemSubstituicaoResponseDto FromEntity(DraftMontagemSubstituicao substituicao)
    {
        return new DraftMontagemSubstituicaoResponseDto(
            substituicao.TimeId,
            substituicao.JogadorSaiuId,
            substituicao.ReservaEntrouId,
            substituicao.JogadorSaiu?.NomeExibicao,
            substituicao.ReservaEntrou?.NomeExibicao,
            substituicao.Motivo,
            substituicao.ResponsavelUsuarioId,
            substituicao.RegistradoEm);
    }
}
