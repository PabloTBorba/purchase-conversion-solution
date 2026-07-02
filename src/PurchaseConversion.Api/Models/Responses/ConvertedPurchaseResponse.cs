namespace PurchaseConversion.Api.Models.Responses;

public record ConvertedPurchaseResponse(
    Guid Id,
    string Description,
    DateOnly TransactionDate,
    decimal AmountUsd,
    string TargetCurrency,
    decimal ExchangeRate,
    decimal ConvertedAmount);
