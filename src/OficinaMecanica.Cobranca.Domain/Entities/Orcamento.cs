using OficinaMecanica.Cobranca.Domain.Exceptions;

namespace OficinaMecanica.Cobranca.Domain.Entities;

public class Orcamento
{
    public Guid Id { get; private set; }
    public Guid OrdemServicoId { get; private set; }
    public Guid ExecucaoId { get; private set; }
    public Guid? SagaId { get; private set; }
    public Guid? PedidoGeracaoId { get; private set; }
    public string? CorrelationId { get; private set; }
    public string Diagnostico { get; private set; } = string.Empty;
    public StatusOrcamento Status { get; private set; }
    public decimal ValorTotal { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? RespondidoEm { get; private set; }
    public string? MotivoRecusa { get; private set; }
    public long Versao { get; private set; }
    public List<ItemOrcamento> Itens { get; private set; } = [];
    public List<HistoricoOrcamento> Historico { get; private set; } = [];
    public Pagamento? Pagamento { get; private set; }

    private Orcamento() { }

    public void VincularSaga(Guid sagaId, Guid pedidoId, string correlationId)
    {
        if (sagaId == Guid.Empty || pedidoId == Guid.Empty || SagaId.HasValue
            || string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 100 || correlationId.Any(char.IsControl))
            throw new DadosInvalidosException("Vinculo de Saga invalido ou ja registrado.");
        SagaId = sagaId;
        PedidoGeracaoId = pedidoId;
        CorrelationId = correlationId;
    }

    public static Orcamento Criar(Guid ordemServicoId, Guid execucaoId, string diagnostico, List<ItemOrcamento> itens)
    {
        if (ordemServicoId == Guid.Empty || execucaoId == Guid.Empty)
            throw new DadosInvalidosException("Informe os IDs da OS e da execucao.");
        if (string.IsNullOrWhiteSpace(diagnostico) || diagnostico.Trim().Length > 2000)
            throw new DadosInvalidosException("Informe o diagnostico com ate 2000 caracteres.");
        if (itens is null || itens.Count is < 1 or > 100 || itens.Any(item => item is null))
            throw new DadosInvalidosException("Informe entre 1 e 100 itens.");
        if (itens.Select(item => (item.Tipo, item.ReferenciaId)).Distinct().Count() != itens.Count)
            throw new DadosInvalidosException("Um item nao pode se repetir no mesmo orcamento.");
        var total = itens.Sum(item => item.Total);
        if (total > 999999999.99m)
            throw new DadosInvalidosException("O total do orcamento excede o limite de 999999999.99.");

        var agora = DateTime.UtcNow;
        return new Orcamento
        {
            Id = Guid.NewGuid(),
            OrdemServicoId = ordemServicoId,
            ExecucaoId = execucaoId,
            Diagnostico = diagnostico.Trim(),
            Itens = itens.ToList(),
            ValorTotal = total,
            CriadoEm = agora,
            Status = StatusOrcamento.AguardandoAprovacao,
            Versao = 1,
            Historico = [new HistoricoOrcamento(StatusOrcamento.AguardandoAprovacao, agora)]
        };
    }

    public bool Aprovar()
    {
        if (Status == StatusOrcamento.Aprovado) return false;
        if (Status != StatusOrcamento.AguardandoAprovacao)
            throw new RegraNegocioException("Somente um orcamento aguardando aprovacao pode ser aprovado.");
        RegistrarResposta(StatusOrcamento.Aprovado);
        return true;
    }

    public bool Recusar(string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 1000)
            throw new DadosInvalidosException("Informe o motivo da recusa com ate 1000 caracteres.");
        motivo = motivo.Trim();
        if (Status == StatusOrcamento.Recusado && MotivoRecusa == motivo) return false;
        if (Status != StatusOrcamento.AguardandoAprovacao)
            throw new RegraNegocioException("O orcamento ja foi respondido e nao pode ser recusado novamente.");
        MotivoRecusa = motivo;
        RegistrarResposta(StatusOrcamento.Recusado, motivo);
        return true;
    }

    public bool RegistrarPagamentoPendente()
    {
        if (Status != StatusOrcamento.Aprovado)
            throw new RegraNegocioException("O orcamento precisa estar aprovado para registrar um pagamento.");
        if (Pagamento is not null) return false;
        Pagamento = new Pagamento(Id, OrdemServicoId, ValorTotal);
        Versao++;
        return true;
    }

    private void RegistrarResposta(StatusOrcamento status, string? motivo = null)
    {
        Status = status;
        RespondidoEm = DateTime.UtcNow;
        Historico.Add(new HistoricoOrcamento(status, RespondidoEm.Value, motivo));
        Versao++;
    }
}
