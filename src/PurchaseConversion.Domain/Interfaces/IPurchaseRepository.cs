using PurchaseConversion.Domain.Entities;

namespace PurchaseConversion.Domain.Interfaces;

public interface IPurchaseRepository
{
    /// <summary>
    /// Adds a new purchase transaction to the repository and returns its unique identifier.
    /// </summary>
    /// <param name="purchase">The purchase transaction to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The unique identifier of the added purchase transaction.</returns>
    Task<Guid> AddAsync(PurchaseTransaction purchase, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a purchase transaction by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the purchase transaction.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The purchase transaction if found; otherwise, null.</returns>
    Task<PurchaseTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}