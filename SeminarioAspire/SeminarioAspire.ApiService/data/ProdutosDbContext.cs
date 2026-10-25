using Microsoft.EntityFrameworkCore;
using SeminarioAspire.ApiService.Models;

namespace SeminarioAspire.ApiService.Data;

public class ProdutosDbContext : DbContext
{
    public ProdutosDbContext(DbContextOptions<ProdutosDbContext> options)
        : base(options)
    {
    }

    public DbSet<Produto> Produtos => Set<Produto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Produto>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Nome)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(p => p.Preco)
                .HasPrecision(10, 2);
        });
    }
}