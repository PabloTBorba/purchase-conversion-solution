using System.Globalization;

namespace PurchaseConversion.Infrastructure.DTO;

public class TreasuryResponse
{
    public List<TreasuryRateRecord> Data { get; set; } = [];
}

public class TreasuryRateRecord
{
    public string Country_Currency_Desc { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string Record_Date { get; set; } = string.Empty;
    public string Exchange_Rate { get; set; } = string.Empty;

    public DateOnly? RecordDate =>
        DateOnly.TryParseExact(
            Record_Date,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var value)
            ? value
            : null;

    public decimal? Rate =>
        decimal.TryParse(
            Exchange_Rate,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var value)
            ? value
            : null;
}
