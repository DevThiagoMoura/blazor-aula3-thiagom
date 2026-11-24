using LojaBlazor.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaBlazor.Data;

// O DbContext é a "sessão" com o banco: abre a conexão, acompanha as
// mudanças feitas nos objetos e as transforma em SQL no SaveChanges.
public class LojaContext(DbContextOptions<LojaContext> options) : DbContext(options)
{
    // Cada DbSet vira uma tabela. DbSet<Produto> => tabela Produtos.
    public DbSet<Produto> Produtos => Set<Produto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Produto>(produto =>
        {
            produto.Property(p => p.Nome)
                .HasMaxLength(100);

            // decimal(10,2): até 99.999.999,99 com exatamente 2 casas decimais.
            produto.Property(p => p.Preco)
                .HasPrecision(10, 2);
        });
    }
}
