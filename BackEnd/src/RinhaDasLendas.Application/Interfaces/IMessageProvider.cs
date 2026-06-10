namespace RinhaDasLendas.Application.Interfaces;

public interface IMessageProvider
{
    string GetMessage(string messageCode);

    string GetMessage(string messageCode, string cultureCode);
}
