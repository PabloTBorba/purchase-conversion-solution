using PurchaseConversion.Domain.Entities;
using PurchaseConversion.Domain.Interfaces;

namespace PurchaseConversion.Domain.Services;

public sealed class PurchaseService(IPurchaseRepository purchaseRepository, IExchangeRateProvider exchangeRateProvider)
{
    private readonly IPurchaseRepository _purchaseRepository = purchaseRepository;
    private readonly IExchangeRateProvider _exchangeRateProvider = exchangeRateProvider;

    /// <summary>
    /// Stores a new purchase transaction in the database.
    /// </summary>
    /// <param name="description">The description of the purchase.</param>
    /// <param name="date">The date of the purchase.</param>
    /// <param name="amountUsd">The amount of the purchase in USD.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The unique identifier of the stored purchase transaction.</returns>
    public async Task<Guid> StorePurchaseAsync(
        string description,
        DateOnly date,
        decimal amountUsd,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(description) || description.Length > 50)
        {
            throw new ArgumentException("Description must be 1–50 characters.", nameof(description));
        }
            
        if (amountUsd <= 0)
        {
            throw new ArgumentException("Amount must be positive.", nameof(amountUsd));
        }

        description = description.Trim();
        amountUsd = decimal.Round(amountUsd, 2, MidpointRounding.AwayFromZero);
        var purchase = new PurchaseTransaction(Guid.NewGuid(), description, date, amountUsd);

        return await _purchaseRepository.AddAsync(purchase, cancellationToken);
    }

    /// <summary>
    /// Converts a stored purchase transaction to a specified currency using the exchange rate at the time of the transaction.
    /// </summary>
    /// <param name="id">The unique identifier of the purchase transaction.</param>
    /// <param name="currency">The target currency code.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ConvertedPurchaseTransaction"/> containing the converted purchase information.</returns>
    public async Task<ConvertedPurchaseTransaction> ConvertAsync(
        Guid id,
        string currency,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        var normalizedCurrency = currency.Trim().ToUpperInvariant();
        var purchase = await _purchaseRepository.GetByIdAsync(id, cancellationToken) 
            ?? throw new KeyNotFoundException("Purchase not found.");

        var rateResult = await _exchangeRateProvider.GetRateAsync(
            normalizedCurrency,
            purchase.TransactionDate,
            cancellationToken);

        if (!rateResult.Success || rateResult.Rate is null)
        {
            throw new InvalidOperationException(rateResult.ErrorMessage ?? "Conversion not available.");
        }

        var converted = decimal.Round(
            purchase.AmountUsd * rateResult.Rate.Value,
            2,
            MidpointRounding.AwayFromZero);

        return new ConvertedPurchaseTransaction(
            purchase.Id,
            purchase.Description,
            purchase.TransactionDate,
            purchase.AmountUsd,
            normalizedCurrency,
            rateResult.Rate.Value,
            converted);
    }
}
