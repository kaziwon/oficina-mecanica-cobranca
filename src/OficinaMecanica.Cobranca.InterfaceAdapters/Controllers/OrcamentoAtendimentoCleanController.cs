using OficinaMecanica.Cobranca.Application.Inputs.Orcamentos;
using OficinaMecanica.Cobranca.Application.UseCases.Orcamentos;
using OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;
using OficinaMecanica.Cobranca.InterfaceAdapters.Presenters;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.Controllers;

public class OrcamentoAtendimentoCleanController(ObterOrcamentoAtendimentoUseCase obter, ResponderOrcamentoAtendimentoUseCase responder)
{
    public OrcamentoResponseDto? Obter(Guid id, VinculoOrcamentoAtendimentoDto vinculo)
    {
        var output = obter.Executar(id, Mapear(vinculo));
        return output is null ? null : OrcamentoAtendimentoPresenter.Apresentar(output);
    }
    public bool Responder(Guid id, VinculoOrcamentoAtendimentoDto vinculo, bool aprovado, string? motivo)
        => responder.Executar(id, Mapear(vinculo), aprovado, motivo);
    private static VinculoOrcamentoAtendimentoInput Mapear(VinculoOrcamentoAtendimentoDto dto)
        => new(dto.OrdemServicoId, dto.ExecucaoId, dto.SagaId, dto.PedidoGeracaoId, dto.ValorTotal, dto.CorrelationId);
}
