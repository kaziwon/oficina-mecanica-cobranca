using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Mappers.Orcamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Orcamentos;
using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.Application.UseCases.Orcamentos;

public class ObterTodosOrcamentosUseCase
{
    private readonly IOrcamentoGateway _orcamentos;
    public ObterTodosOrcamentosUseCase(IOrcamentoGateway orcamentos) { _orcamentos = orcamentos; }

    public List<OrcamentoOutput> Executar(StatusOrcamento? status, Guid? ordemServicoId)
    {
        if (status.HasValue && !Enum.IsDefined(status.Value))
            throw new ValidacaoException("Status de orcamento invalido.");
        if (ordemServicoId == Guid.Empty) throw new ValidacaoException("Informe um ID de OS valido.");
        return _orcamentos.ObterTodos(status, ordemServicoId).Select(OrcamentoOutputMapper.Mapear).ToList();
    }
}
