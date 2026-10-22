using Microsoft.EntityFrameworkCore;

namespace Restaurante.Server;

public sealed class EstoqueDb(DbContextOptions<EstoqueDb> options) : DbContext(options)
{
    public DbSet<LancheEstoque> Lanches => Set<LancheEstoque>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<LancheEstoque>().ToTable("Lanches", table =>
        {
            table.HasCheckConstraint("CK_Estoque", "Estoque >= 0");
            table.HasCheckConstraint("CK_Preco", "PrecoCentavos >= 0");
        });
    }
}

public sealed class LancheEstoque
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public long PrecoCentavos { get; set; }
    public int Estoque { get; set; }
}
