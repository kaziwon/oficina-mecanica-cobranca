using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.Application.Gateways;

public interface IPagamentoIntegracaoGateway
{
    bool FoiProcessada(MensagemIntegracao pedido);
    Orcamento? ObterOrcamento(Guid id);
    MensagemIntegracao? ObterEventoAprovacao(Guid id);
    void Registrar(MensagemIntegracao pedido, Orcamento orcamento);
}
