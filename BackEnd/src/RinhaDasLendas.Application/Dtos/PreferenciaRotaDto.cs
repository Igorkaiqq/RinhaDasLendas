namespace RinhaDasLendas.Application.Dtos;

public sealed record PreferenciaRotaDto(
    string Rota,
    int Prioridade,
    bool NaoJogoNemLascando);
