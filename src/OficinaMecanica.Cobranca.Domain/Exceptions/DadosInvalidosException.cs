namespace OficinaMecanica.Cobranca.Domain.Exceptions;

public class DadosInvalidosException : Exception
{
    public DadosInvalidosException(string message) : base(message) { }
}
