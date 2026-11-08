using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.UnitTests.Fakes;

public class OrcamentoIntegracaoGatewayFake : IOrcamentoIntegracaoGateway
{
    public bool Processada { get; set; }
    public bool Existe { get; set; }
    public bool Falhar { get; set; }
    public Orcamento? Orcamento { get; private set; }
    public MensagemIntegracao? Resposta { get; private set; }
    public bool FoiProcessada(MensagemIntegracao pedido) => Processada;
    public bool ExisteOrcamento(Guid os, Guid execucao) => Existe;
    public void Registrar(MensagemIntegracao pedido, Orcamento? orcamento, MensagemIntegracao resposta)
    {
        if (Falhar) throw new InvalidOperationException("Banco indisponivel");
        Orcamento = orcamento; Resposta = resposta; Processada = true;
    }
}
