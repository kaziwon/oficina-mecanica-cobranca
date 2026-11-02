using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.Gateways;

public class PagamentoGateway : IPagamentoGateway
{
    private readonly IPagamentoDataSource _source;
    public PagamentoGateway(IPagamentoDataSource source) { _source = source; }
    public Pagamento? ObterPorId(Guid id) => _source.ObterPorId(id);
}
