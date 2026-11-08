using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Inputs.Orcamentos;
using OficinaMecanica.Cobranca.Application.Mappers.Orcamentos;
using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.Domain.Exceptions;

namespace OficinaMecanica.Cobranca.Application.UseCases.Orcamentos;

public class ProcessarPedidoOrcamentoUseCase
{
    private readonly IOrcamentoIntegracaoGateway _integracao;
    public ProcessarPedidoOrcamentoUseCase(IOrcamentoIntegracaoGateway integracao) { _integracao = integracao; }

    public void Executar(MensagemIntegracao pedido)
    {
        MensagemValidator.Validar(pedido, "GerarOrcamento");
        var dados = pedido.Payload;
        if (!pedido.CausationId.HasValue || !dados.ExecucaoId.HasValue || dados.Itens is null
            || dados.DescricaoSolicitacao is not null || dados.OrcamentoId.HasValue || dados.ValorTotal.HasValue
            || dados.Codigo is not null || dados.Motivo is not null
            || dados.DiagnosticoIniciadoEm.HasValue || dados.DiagnosticoConcluidoEm.HasValue)
            throw new ValidacaoException("Contrato do pedido de orcamento invalido.");
        if (_integracao.FoiProcessada(pedido)) return;
        Orcamento? orcamento = null;
        string? codigo = null;
        string? motivo = null;
        if (_integracao.ExisteOrcamento(dados.OrdemServicoId, dados.ExecucaoId.Value))
        {
            if (_integracao.FoiProcessada(pedido)) return;
            codigo = "ConflitoDeVinculo";
            motivo = "A OS ou execucao ja possui um orcamento de outro pedido.";
        }
        else
        {
            // Somente recusas de negocio viram eventos. Falhas de banco continuam sendo retentadas.
            try
            {
                orcamento = OrcamentoInputMapper.Mapear(new CriarOrcamentoInput
                {
                    OrdemServicoId = dados.OrdemServicoId,
                    ExecucaoId = dados.ExecucaoId.Value,
                    Diagnostico = dados.Diagnostico ?? string.Empty,
                    Itens = dados.Itens.Select(item => new ItemOrcamentoInput
                    {
                        Tipo = item.Tipo switch
                        {
                            "Servico" => TipoItemOrcamento.Servico,
                            "PecaInsumo" => TipoItemOrcamento.PecaInsumo,
                            _ => throw new DadosInvalidosException("Tipo de item desconhecido.")
                        },
                        ReferenciaId = item.ReferenciaId,
                        Nome = item.Nome,
                        Quantidade = item.Quantidade,
                        PrecoUnitario = item.PrecoUnitario
                    }).ToList()
                });
                orcamento.VincularSaga(pedido.SagaId, pedido.MessageId);
            }
            catch (Exception exception) when (exception is ValidacaoException or DadosInvalidosException)
            { codigo = "DadosInvalidos"; motivo = exception.Message; }
        }
        var resposta = new MensagemIntegracao
        {
            MessageId = Guid.NewGuid(),
            Type = orcamento is null ? "GeracaoOrcamentoRecusada" : "OrcamentoGerado",
            SagaId = pedido.SagaId,
            CausationId = pedido.MessageId,
            OccurredAt = DateTime.UtcNow,
            CorrelationId = pedido.CorrelationId,
            Payload = new DadosMensagem
            {
                OrdemServicoId = dados.OrdemServicoId,
                ExecucaoId = dados.ExecucaoId,
                OrcamentoId = orcamento?.Id,
                ValorTotal = orcamento?.ValorTotal,
                Codigo = codigo,
                Motivo = motivo
            }
        };
        _integracao.Registrar(pedido, orcamento, resposta);
    }
}
