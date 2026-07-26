using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Application.Dtos;

public sealed record ArquivarDraftMontagemRequestDto(string Motivo, long VersaoEstado);

public sealed record RestaurarDraftMontagemRequestDto(long VersaoEstado);

public sealed record DraftMontagemArquivamentoResultadoDto(
    Guid Id,
    string Status,
    bool Arquivado,
    long VersaoEstado)
{
    public static DraftMontagemArquivamentoResultadoDto FromEntity(DraftMontagem montagem) =>
        new(montagem.Id, montagem.Status.ToString(), montagem.Arquivado, montagem.VersaoEstado);
}

public sealed record DraftMontagemArquivamentoDto(
    DraftMontagemResponseDto Draft,
    DateTimeOffset? ArquivadoEm,
    Guid? ArquivadoPorUsuarioId,
    string? MotivoArquivamento,
    IReadOnlyCollection<DraftMontagemAcaoAdministrativaResponseDto> Acoes)
{
    public static DraftMontagemArquivamentoDto FromEntity(DraftMontagem montagem) => new(
        DraftMontagemResponseDto.FromEntity(montagem),
        montagem.ArquivadoEm,
        montagem.ArquivadoPorUsuarioId,
        montagem.MotivoArquivamento,
        montagem.AcoesAdministrativas
            .Where(acao => acao.Tipo is "Arquivamento" or "Restauracao" or "CancelamentoPorArquivamento")
            .OrderBy(acao => acao.RegistradoEm)
            .Select(DraftMontagemAcaoAdministrativaResponseDto.FromEntity)
            .ToList());
}
