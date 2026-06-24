using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Domain.Entities;

public sealed class DraftMontagemEscolha
{
    private DraftMontagemEscolha()
    {
    }

    public DraftMontagemEscolha(int sequencia, Guid timeId, Guid capitaoId, Guid? jogadorId, DraftMontagemEscolhaTipo tipo, DateTimeOffset registradoEm)
    {
        Id = Guid.NewGuid();
        Sequencia = sequencia;
        TimeId = timeId;
        CapitaoId = capitaoId;
        JogadorId = jogadorId;
        Tipo = tipo;
        RegistradoEm = registradoEm;
    }

    public Guid Id { get; private set; }
    public Guid DraftMontagemId { get; private set; }
    public int Sequencia { get; private set; }
    public Guid TimeId { get; private set; }
    public Guid CapitaoId { get; private set; }
    public Guid? JogadorId { get; private set; }
    public DraftMontagemEscolhaTipo Tipo { get; private set; }
    public DateTimeOffset RegistradoEm { get; private set; }
    public Jogador? Capitao { get; private set; }
    public Jogador? Jogador { get; private set; }
}
