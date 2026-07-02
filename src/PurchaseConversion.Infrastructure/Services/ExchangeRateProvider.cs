using PurchaseConversion.Domain.Interfaces;
using PurchaseConversion.Domain.Models;
using PurchaseConversion.Infrastructure.DTO;
using System.Net.Http.Json;

namespace PurchaseConversion.Infrastructure.Services;

public sealed class ExchangeRateProvider(HttpClient client) : IExchangeRateProvider
{
    private const string RatesEndpoint = "v1/accounting/od/rates_of_exchange";
    private readonly HttpClient _client = client;

    /// <inheritdoc />
    public async Task<ExchangeRateResult> GetRateAsync(
        string currencyCode,
        DateOnly purchaseDate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return new ExchangeRateResult(false, null, "Currency code is required.");
        }

        var normalizedCurrency = currencyCode.Trim().ToUpperInvariant();
        var sixMonthsAgo = purchaseDate.AddMonths(-6);
        var requestUri = BuildRatesRequestUri(normalizedCurrency, sixMonthsAgo, purchaseDate);

        try
        {
            using var response = await _client.GetAsync(requestUri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new ExchangeRateResult(
                    false,
                    null,
                    $"Treasury API returned {(int)response.StatusCode} ({response.ReasonPhrase}).");
            }

            var data = await response.Content.ReadFromJsonAsync<TreasuryResponse>(cancellationToken);

            var rateRecord = data?.Data
                .Where(r => r.RecordDate is not null && r.Rate is not null)
                .OrderByDescending(r => r.RecordDate)
                .FirstOrDefault();

            if (rateRecord?.Rate is null)
            {
                return new ExchangeRateResult(false, null,
                    "No exchange rate data found for the specified currency and date range.");
            }

            return new ExchangeRateResult(true, rateRecord.Rate, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            return new ExchangeRateResult(false, null, "Treasury API request timed out.");
        }
        catch (HttpRequestException ex)
        {
            return new ExchangeRateResult(false, null, $"Failed to call Treasury API: {ex.Message}");
        }
    }

    private static string BuildRatesRequestUri(string currencyCode, DateOnly startDate, DateOnly endDate)
    {
        var filter =
            $"country_currency_desc:eq:{currencyCode}," +
            $"record_date:gte:{startDate:yyyy-MM-dd}," +
            $"record_date:lte:{endDate:yyyy-MM-dd}";

        var queryParts = new Dictionary<string, string>
        {
            ["filter"] = filter,
            ["sort"] = "-record_date",
            ["page[size]"] = "1",
            ["page[number]"] = "1",
            ["fields"] = "country_currency_desc,exchange_rate,record_date"
        };

        var query = string.Join("&",
            queryParts.Select(kvp =>
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        return $"{RatesEndpoint}?{query}";
    }
}