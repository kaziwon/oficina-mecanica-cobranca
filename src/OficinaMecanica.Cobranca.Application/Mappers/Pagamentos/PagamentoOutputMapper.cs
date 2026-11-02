using OficinaMecanica.Cobranca.Application.Outputs.Pagamentos;
using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.Application.Mappers.Pagamentos;

public static class PagamentoOutputMapper
{
    public static PagamentoOutput Mapear(Pagamento pagamento) => new()
    {
        Id = pagamento.Id,
        OrcamentoId = pagamento.OrcamentoId,
        OrdemServicoId = pagamento.OrdemServicoId,
        Valor = pagamento.Valor,
        Moeda = pagamento.Moeda,
        Status = pagamento.Status,
        CriadoEm = pagamento.CriadoEm
    };
}
