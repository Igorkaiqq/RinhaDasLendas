using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Domain.ValueObjects;

public sealed class ParticipanteEsperadoSerieInput
{
    public ParticipanteEsperadoSerieInput(Guid jogadorId, string nomeSnapshot, int ordem)
    {
        JogadorId = jogadorId;
        NomeSnapshot = nomeSnapshot;
        Ordem = ordem;
    }

    public Guid JogadorId { get; }
    public string NomeSnapshot { get; }
    public int Ordem { get; }
}

public sealed class LadoSerieInput
{
    public LadoSerieInput(
        int ordem,
        LadoSerieTipo tipo,
        Guid origemId,
        string nomeSnapshot,
        string? tagSnapshot,
        Guid? capitaoJogadorId,
        string? capitaoNomeSnapshot,
        IReadOnlyCollection<ParticipanteEsperadoSerieInput> participantesEsperados)
    {
        Ordem = ordem;
        Tipo = tipo;
        OrigemId = origemId;
        NomeSnapshot = nomeSnapshot;
        TagSnapshot = tagSnapshot;
        CapitaoJogadorId = capitaoJogadorId;
        CapitaoNomeSnapshot = capitaoNomeSnapshot;
        ParticipantesEsperados = Array.AsReadOnly(participantesEsperados?.ToArray() ?? []);
    }

    public int Ordem { get; }
    public LadoSerieTipo Tipo { get; }
    public Guid OrigemId { get; }
    public string NomeSnapshot { get; }
    public string? TagSnapshot { get; }
    public Guid? CapitaoJogadorId { get; }
    public string? CapitaoNomeSnapshot { get; }
    public IReadOnlyCollection<ParticipanteEsperadoSerieInput> ParticipantesEsperados { get; }
}
