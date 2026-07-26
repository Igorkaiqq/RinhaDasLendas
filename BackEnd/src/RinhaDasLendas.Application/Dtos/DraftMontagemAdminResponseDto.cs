using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DraftMontagemAdminResponseDto(
    Guid Id,
    string Nome,
    string? Observacoes,
    string Status,
    string Modo,
    int TamanhoEquipe,
    int QuantidadeTimes,
    int QuantidadeReservas,
    string CriterioCapitaes,
    Guid? TurnoAtualTimeId,
    Guid? TurnoAtualCapitaoId,
    int? TurnoSequencia,
    DateTimeOffset? TurnoIniciadoEm,
    DateTimeOffset? TurnoExpiraEm,
    int DuracaoTurnoSegundos,
    DateTimeOffset? HorarioEncerramentoPresenca,
    string? DiscordGuildId,
    string? DiscordPresenceMessageId,
    string? OrdemEscolhaModo,
    bool PresencaContinuadaManualmente,
    IReadOnlyCollection<DraftMontagemPresencaResponseDto> Presencas,
    IReadOnlyCollection<DraftMontagemTimeResponseDto> Times,
    IReadOnlyCollection<DraftMontagemParticipanteResponseDto> Livres,
    IReadOnlyCollection<DraftMontagemParticipanteResponseDto> Reservas,
    IReadOnlyCollection<DraftMontagemEscolhaResponseDto> Escolhas,
    IReadOnlyCollection<DraftMontagemSubstituicaoResponseDto> Substituicoes,
    IReadOnlyCollection<DraftMontagemPublicacaoDiscordAdminResponseDto> PublicacoesDiscord,
    IReadOnlyCollection<DraftMontagemAcaoAdministrativaResponseDto> AcoesAdministrativas,
    string? MotivoCancelamento,
    DateTimeOffset DataCadastro,
    DateTimeOffset DataAtualizacao)
{
    public static DraftMontagemAdminResponseDto FromEntity(DraftMontagem montagem)
    {
        var participantes = montagem.Participantes.ToList();
        var cancelamentoOriginadoPorArquivamento = montagem.AcoesAdministrativas.Any(
            acao => acao.Tipo == "CancelamentoPorArquivamento");
        return new DraftMontagemAdminResponseDto(
            montagem.Id,
            montagem.Nome,
            montagem.Observacoes,
            montagem.Status.ToString(),
            montagem.Modo.ToString(),
            montagem.TamanhoEquipe,
            montagem.QuantidadeTimes,
            montagem.QuantidadeReservas,
            montagem.CriterioCapitaes.ToString(),
            montagem.TurnoAtualTimeId,
            montagem.TurnoAtualCapitaoId,
            montagem.TurnoSequencia,
            montagem.TurnoIniciadoEm,
            montagem.TurnoExpiraEm,
            montagem.DuracaoTurnoSegundos,
            montagem.HorarioEncerramentoPresenca,
            montagem.DiscordGuildId,
            montagem.DiscordPresenceMessageId,
            montagem.OrdemEscolhaModo?.ToString(),
            montagem.PresencaContinuadaManualmente,
            montagem.Presencas.OrderBy(presenca => presenca.OrdemFinal ?? presenca.OrdemManual ?? presenca.OrdemConfirmacao).Select(DraftMontagemPresencaResponseDto.FromEntity).ToList(),
            montagem.Times.OrderBy(time => time.Ordem).Select(time => DraftMontagemTimeResponseDto.FromEntity(time, participantes)).ToList(),
            participantes.Where(participante => participante.Estado == DraftMontagemParticipanteEstado.Livre).OrderBy(participante => participante.Ordem).Select(DraftMontagemParticipanteResponseDto.FromEntity).ToList(),
            participantes.Where(participante => participante.Estado == DraftMontagemParticipanteEstado.Reserva).OrderBy(participante => participante.Ordem).Select(DraftMontagemParticipanteResponseDto.FromEntity).ToList(),
            montagem.Escolhas.OrderBy(escolha => escolha.Sequencia).Select(DraftMontagemEscolhaResponseDto.FromEntity).ToList(),
            montagem.Substituicoes.OrderBy(substituicao => substituicao.RegistradoEm).Select(DraftMontagemSubstituicaoResponseDto.FromEntity).ToList(),
            montagem.PublicacoesDiscord.OrderBy(publicacao => publicacao.Tipo).Select(DraftMontagemPublicacaoDiscordAdminResponseDto.FromEntity).ToList(),
            montagem.AcoesAdministrativas
                .Where(acao => acao.Tipo is not "Arquivamento" and not "Restauracao" and not "CancelamentoPorArquivamento")
                .OrderBy(acao => acao.RegistradoEm)
                .Select(DraftMontagemAcaoAdministrativaResponseDto.FromEntity)
                .ToList(),
            cancelamentoOriginadoPorArquivamento ? null : montagem.MotivoCancelamento,
            montagem.DataCadastro,
            montagem.DataAtualizacao);
    }
}

public sealed record DraftMontagemAcaoAdministrativaResponseDto(Guid Id, string Tipo, Guid ResponsavelUsuarioId, Guid? JogadorAlvoId, string? Motivo, DateTimeOffset RegistradoEm)
{
    public static DraftMontagemAcaoAdministrativaResponseDto FromEntity(DraftMontagemAcaoAdministrativa acao)
    {
        return new DraftMontagemAcaoAdministrativaResponseDto(acao.Id, acao.Tipo, acao.ResponsavelUsuarioId, acao.JogadorAlvoId, acao.Motivo, acao.RegistradoEm);
    }
}

public sealed record DraftMontagemPublicacaoDiscordAdminResponseDto(
    Guid Id,
    string Tipo,
    string Status,
    string? GuildId,
    string? ChannelId,
    string? MessageId,
    string? UltimoErroCodigo,
    Guid? ClaimId,
    DateTimeOffset? ClaimExpiraEm,
    DateTimeOffset? PublicadaEm,
    DateTimeOffset UltimaTentativaEm)
{
    public static DraftMontagemPublicacaoDiscordAdminResponseDto FromEntity(DraftMontagemPublicacaoDiscord publicacao)
    {
        return new DraftMontagemPublicacaoDiscordAdminResponseDto(
            publicacao.Id,
            publicacao.Tipo.ToString(),
            publicacao.Status.ToString(),
            publicacao.GuildId,
            publicacao.ChannelId,
            publicacao.MessageId,
            publicacao.UltimoErroCodigo,
            publicacao.ClaimId,
            publicacao.ClaimExpiraEm,
            publicacao.PublicadaEm,
            publicacao.UltimaTentativaEm);
    }
}
