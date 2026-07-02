namespace PurchaseConversion.Domain.Entities;

public record PurchaseTransaction(
    Guid Id,
    string Description,
    DateOnly TransactionDate,
    decimal AmountUsd);