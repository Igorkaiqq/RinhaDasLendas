using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Application.Dtos;

public sealed record TimeResponseDto(
    Guid Id,
    string Nome,
    string Tag,
    string? Observacoes,
    string Status,
    TimeCapitaoDto? Capitao,
    int QuantidadeJogadores,
    IReadOnlyCollection<TimeMembroDto> Membros,
    DateTimeOffset DataCadastro,
    DateTimeOffset DataAtualizacao)
{
    public static TimeResponseDto FromEntity(Time time)
    {
        var membros = time.Membros
            .OrderBy(membro => membro.Jogador?.NomeExibicao ?? string.Empty)
            .Select(membro => new TimeMembroDto(
                membro.JogadorId,
                membro.Jogador?.NomeExibicao ?? string.Empty,
                membro.Principal,
                time.CapitaoId == membro.JogadorId))
            .ToList();

        var capitao = membros.FirstOrDefault(membro => membro.Capitao);

        return new TimeResponseDto(
            time.Id,
            time.Nome,
            time.Tag,
            time.Observacoes,
            time.Status.ToString(),
            capitao is null ? null : new TimeCapitaoDto(capitao.JogadorId, capitao.NomeExibicao),
            membros.Count,
            membros,
            time.DataCadastro,
            time.DataAtualizacao);
    }
}

public sealed record TimeCapitaoDto(Guid Id, string NomeExibicao);

public sealed record TimeMembroDto(Guid JogadorId, string NomeExibicao, bool Principal, bool Capitao);
