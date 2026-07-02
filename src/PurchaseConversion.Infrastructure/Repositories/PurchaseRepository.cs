using Microsoft.EntityFrameworkCore;
using PurchaseConversion.Domain.Entities;
using PurchaseConversion.Domain.Interfaces;
using PurchaseConversion.Infrastructure.Persistence;

namespace PurchaseConversion.Infrastructure.Repositories;

public sealed class PurchaseRepository(PurchaseDbContext db) : IPurchaseRepository
{
    private readonly PurchaseDbContext _db = db;

    /// <inheritdoc />
    public async Task<Guid> AddAsync(PurchaseTransaction purchase, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(purchase);
        cancellationToken.ThrowIfCancellationRequested();

        _db.Purchases.Add(purchase);
        await _db.SaveChangesAsync(cancellationToken);
        return purchase.Id;
    }

    /// <inheritdoc />
    public async Task<PurchaseTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _db.Purchases
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}