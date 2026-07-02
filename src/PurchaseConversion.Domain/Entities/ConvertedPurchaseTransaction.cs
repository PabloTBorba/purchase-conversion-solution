namespace PurchaseConversion.Domain.Entities;

public record ConvertedPurchaseTransaction(
    Guid Id,
    string Description,
    DateOnly TransactionDate,
    decimal AmountUsd,
    string TargetCurrency,
    decimal ExchangeRate,
    decimal ConvertedAmount);