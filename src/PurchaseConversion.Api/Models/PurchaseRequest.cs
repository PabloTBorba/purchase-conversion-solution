namespace PurchaseConversion.Api.Models;

public class PurchaseRequest
{
    public string Description { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public decimal AmountUsd { get; set; }
}