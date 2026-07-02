using Microsoft.EntityFrameworkCore;
using PurchaseConversion.Domain.Entities;

namespace PurchaseConversion.Infrastructure.Persistence;

public class PurchaseDbContext(DbContextOptions<PurchaseDbContext> options) : DbContext(options)
{
    public DbSet<PurchaseTransaction> Purchases => Set<PurchaseTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<PurchaseTransaction>();

        entity.HasKey(p => p.Id);

        entity.Property(p => p.Description)
              .IsRequired()
              .HasMaxLength(50);

        entity.Property(p => p.AmountUsd)
              .HasColumnType("decimal(18,2)")
              .IsRequired();

        entity.Property(p => p.TransactionDate)
              .HasConversion(
                  d => d.ToString("yyyy-MM-dd"),
                  s => DateOnly.Parse(s));
    }
}
