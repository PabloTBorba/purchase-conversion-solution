using PurchaseConversion.Domain.Models;

namespace PurchaseConversion.Domain.Interfaces;

public interface IExchangeRateProvider
{
    /// <summary>
    /// Gets the exchange rate for a given currency code and purchase date, based on information
    /// provided by an external US Treasury service.
    /// </summary>
    /// <param name="currencyCode">The currency code to get the exchange rate for.</param>
    /// <param name="purchaseDate">The date of the purchase.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="ExchangeRateResult"/> containing the exchange rate information.</returns>
    Task<ExchangeRateResult> GetRateAsync(
        string currencyCode,
        DateOnly purchaseDate,
        CancellationToken cancellationToken = default);
}
