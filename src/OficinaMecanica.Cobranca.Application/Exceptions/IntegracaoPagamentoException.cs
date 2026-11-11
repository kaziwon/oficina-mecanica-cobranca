namespace OficinaMecanica.Cobranca.Application.Exceptions;

public class IntegracaoPagamentoException : Exception
{
    public IntegracaoPagamentoException(string mensagem, Exception? inner = null) : base(mensagem, inner) { }
}
