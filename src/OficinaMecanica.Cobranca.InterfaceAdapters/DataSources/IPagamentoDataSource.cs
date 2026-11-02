using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

public interface IPagamentoDataSource
{
    Pagamento? ObterPorId(Guid id);
}
