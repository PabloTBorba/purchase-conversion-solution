namespace PurchaseConversion.Domain.Models;

public record ExchangeRateResult(bool Success, decimal? Rate, string? ErrorMessage);