using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.Application.Gateways;

public interface IOrcamentoIntegracaoGateway
{
    bool FoiProcessada(MensagemIntegracao pedido);
    bool ExisteOrcamento(Guid ordemServicoId, Guid execucaoId);
    void Registrar(MensagemIntegracao pedido, Orcamento? orcamento, MensagemIntegracao resposta);
}
