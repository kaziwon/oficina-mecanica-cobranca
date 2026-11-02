using OficinaMecanica.Cobranca.Domain.Exceptions;

namespace OficinaMecanica.Cobranca.Domain.Entities;

public class ItemOrcamento
{
    public Guid Id { get; private set; }
    public Guid OrcamentoId { get; private set; }
    public TipoItemOrcamento Tipo { get; private set; }
    public Guid ReferenciaId { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public int Quantidade { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public decimal Total => Quantidade * PrecoUnitario;

    private ItemOrcamento() { }

    public ItemOrcamento(TipoItemOrcamento tipo, Guid referenciaId, string nome, int quantidade, decimal precoUnitario)
    {
        if (!Enum.IsDefined(tipo) || referenciaId == Guid.Empty)
            throw new DadosInvalidosException("Informe o tipo e a referencia do item.");
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 150)
            throw new DadosInvalidosException("Informe o nome do item com ate 150 caracteres.");
        if (quantidade is < 1 or > 10000)
            throw new DadosInvalidosException("A quantidade deve estar entre 1 e 10000.");
        if (precoUnitario <= 0 || precoUnitario > 999999.99m || decimal.Round(precoUnitario, 2) != precoUnitario)
            throw new DadosInvalidosException("Informe um preco positivo de ate 999999.99 com no maximo duas casas decimais.");
        Id = Guid.NewGuid();
        Tipo = tipo;
        ReferenciaId = referenciaId;
        Nome = nome.Trim();
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
    }
}
