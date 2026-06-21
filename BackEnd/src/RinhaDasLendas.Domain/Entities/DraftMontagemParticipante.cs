using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Domain.Entities;

public sealed class DraftMontagemParticipante
{
    private DraftMontagemParticipante()
    {
    }

    public DraftMontagemParticipante(Guid jogadorId, DraftMontagemParticipanteEstado estado, int ordem)
    {
        Id = Guid.NewGuid();
        JogadorId = jogadorId;
        Estado = estado;
        Ordem = ordem;
        DataCadastro = DateTimeOffset.UtcNow;
        DataAtualizacao = DataCadastro;
    }

    public Guid Id { get; private set; }
    public Guid DraftMontagemId { get; private set; }
    public Guid JogadorId { get; private set; }
    public Guid? TimeId { get; private set; }
    public DraftMontagemParticipanteEstado Estado { get; private set; }
    public bool Capitao { get; private set; }
    public Rota? RotaContextual { get; private set; }
    public int Ordem { get; private set; }
    public DateTimeOffset DataCadastro { get; private set; }
    public DateTimeOffset DataAtualizacao { get; private set; }
    public Jogador? Jogador { get; private set; }

    public void AtribuirLivre(int ordem, Rota? rotaContextual)
    {
        TimeId = null;
        Estado = DraftMontagemParticipanteEstado.Livre;
        Capitao = false;
        Ordem = ordem;
        RotaContextual = rotaContextual;
        Touch();
    }

    public void AtribuirReserva(int ordem, Rota? rotaContextual)
    {
        TimeId = null;
        Estado = DraftMontagemParticipanteEstado.Reserva;
        Capitao = false;
        Ordem = ordem;
        RotaContextual = rotaContextual;
        Touch();
    }

    public void AtribuirTime(Guid timeId, int ordem, bool capitao, Rota? rotaContextual)
    {
        TimeId = timeId;
        Estado = DraftMontagemParticipanteEstado.Time;
        Capitao = capitao;
        Ordem = ordem;
        RotaContextual = rotaContextual;
        Touch();
    }

    public void DefinirCapitao(bool capitao)
    {
        Capitao = capitao;
        Touch();
    }

    private void Touch()
    {
        DataAtualizacao = DateTimeOffset.UtcNow;
    }
}
