using Microsoft.EntityFrameworkCore;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.Infrastructure.Persistence.Models;

namespace OficinaMecanica.Cobranca.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Orcamento> Orcamentos => Set<Orcamento>();
    public DbSet<ItemOrcamento> ItensOrcamento => Set<ItemOrcamento>();
    public DbSet<HistoricoOrcamento> HistoricoOrcamento => Set<HistoricoOrcamento>();
    public DbSet<Pagamento> Pagamentos => Set<Pagamento>();
    public DbSet<MensagemInbox> Inbox => Set<MensagemInbox>();
    public DbSet<MensagemOutbox> Outbox => Set<MensagemOutbox>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MensagemInbox>(entity =>
        {
            entity.Property(item => item.Id).ValueGeneratedNever();
            entity.Property(item => item.Hash).HasMaxLength(64).IsRequired();
        });
        modelBuilder.Entity<MensagemOutbox>(entity =>
        {
            entity.Property(item => item.Id).ValueGeneratedNever();
            entity.Property(item => item.Body).IsRequired();
            entity.HasIndex(item => new { item.PublicadaEm, item.CriadaEm });
        });
        modelBuilder.Entity<Orcamento>(entity =>
        {
            entity.Property(item => item.Id).ValueGeneratedNever();
            entity.Property(item => item.Diagnostico).HasMaxLength(2000).IsRequired();
            entity.Property(item => item.CorrelationId).HasMaxLength(100);
            entity.Property(item => item.MotivoRecusa).HasMaxLength(1000);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(item => item.ValorTotal).HasPrecision(11, 2);
            entity.Property(item => item.Versao).IsConcurrencyToken();
            entity.HasIndex(item => item.OrdemServicoId).IsUnique();
            entity.HasIndex(item => item.ExecucaoId).IsUnique();
            entity.HasIndex(item => new { item.Status, item.CriadoEm });
            entity.HasMany(item => item.Itens).WithOne().HasForeignKey(item => item.OrcamentoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(item => item.Historico).WithOne().HasForeignKey(item => item.OrcamentoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Pagamento).WithOne().HasForeignKey<Pagamento>(item => item.OrcamentoId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ItemOrcamento>(entity =>
        {
            entity.Property(item => item.Id).ValueGeneratedNever();
            entity.Property(item => item.Tipo).HasConversion<string>().HasMaxLength(20);
            entity.Property(item => item.Nome).HasMaxLength(150).IsRequired();
            entity.Property(item => item.PrecoUnitario).HasPrecision(8, 2);
            entity.Ignore(item => item.Total);
            entity.HasIndex(item => new { item.OrcamentoId, item.Tipo, item.ReferenciaId }).IsUnique();
        });
        modelBuilder.Entity<HistoricoOrcamento>(entity =>
        {
            entity.Property(item => item.Id).ValueGeneratedNever();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(item => item.Motivo).HasMaxLength(1000);
        });
        modelBuilder.Entity<Pagamento>(entity =>
        {
            entity.Property(item => item.Id).ValueGeneratedNever();
            entity.Property(item => item.Valor).HasPrecision(11, 2);
            entity.Property(item => item.Moeda).HasMaxLength(3).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(30);
        });
    }
}
