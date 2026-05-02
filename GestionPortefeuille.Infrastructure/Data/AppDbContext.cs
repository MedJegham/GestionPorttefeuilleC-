using GestionPortefeuille.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionPortefeuille.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<PortfolioBudget> PortfolioBudgets => Set<PortfolioBudget>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();
    public DbSet<PortfolioValuePoint> PortfolioValuePoints => Set<PortfolioValuePoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Asset>()
            .HasIndex(a => a.Symbol)
            .IsUnique();

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Asset)
            .WithMany(a => a.Transactions)
            .HasForeignKey(t => t.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PriceHistory>()
            .HasOne(p => p.Asset)
            .WithMany(a => a.PriceHistories)
            .HasForeignKey(p => p.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PriceHistory>()
            .HasIndex(p => new { p.AssetId, p.Date })
            .IsUnique();

        modelBuilder.Entity<PortfolioValuePoint>()
            .HasIndex(p => p.Date)
            .IsUnique();
    }
}
