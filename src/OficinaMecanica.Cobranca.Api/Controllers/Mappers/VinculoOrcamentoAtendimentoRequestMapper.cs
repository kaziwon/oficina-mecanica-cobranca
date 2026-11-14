using OficinaMecanica.Cobranca.Controllers.DTOs;
using OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;

namespace OficinaMecanica.Cobranca.Controllers.Mappers;

public static class VinculoOrcamentoAtendimentoRequestMapper
{
    public static VinculoOrcamentoAtendimentoDto ParaCleanDto(this VinculoOrcamentoAtendimentoRequestDto dto)
        => new()
        {
            OrdemServicoId = dto.OrdemServicoId,
            ExecucaoId = dto.ExecucaoId,
            SagaId = dto.SagaId,
            PedidoGeracaoId = dto.PedidoGeracaoId,
            ValorTotal = dto.ValorTotal,
            CorrelationId = dto.CorrelationId
        };
}
