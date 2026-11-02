using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.Application.Gateways;

public interface IPagamentoGateway
{
    Pagamento? ObterPorId(Guid id);
}
