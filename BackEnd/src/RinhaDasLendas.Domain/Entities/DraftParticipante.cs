using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Domain.Entities;

public sealed class DraftParticipante
{
    private DraftParticipante()
    {
    }

    public DraftParticipante(Guid jogadorId, DraftTime? time = null, bool capitao = false)
    {
        Id = Guid.NewGuid();
        JogadorId = jogadorId;
        Time = time;
        Capitao = capitao;
        DataCadastro = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid DraftSessaoId { get; private set; }
    public Guid JogadorId { get; private set; }
    public DraftTime? Time { get; private set; }
    public bool Capitao { get; private set; }
    public DateTimeOffset DataCadastro { get; private set; }
    public Jogador? Jogador { get; private set; }

    public bool Disponivel => Time is null;

    public void AtribuirTime(DraftTime time)
    {
        Time = time;
    }
}
