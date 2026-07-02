using Moq;
using Moq.Protected;
using PurchaseConversion.Infrastructure.DTO;
using PurchaseConversion.Infrastructure.Services;
using System.Globalization;
using System.Net;

namespace PurchaseConversion.Infrastructure.Tests.Services;

public sealed class ExchangeRateProviderTests
{
    [Fact]
    internal async Task GetRateAsync_WhenValidResponseWithRate_ReturnsSuccessfulResult()
    {
        // arrange
        var currencyCode = "EUR";
        var purchaseDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var expectedRate = 1.23456m;

        var treasuryResponse = new TreasuryResponse
        {
            Data =
            [
                new TreasuryRateRecord
                {
                    Country_Currency_Desc = currencyCode,
                    Record_Date = purchaseDate.ToString("yyyy-MM-dd"),
                    Exchange_Rate = expectedRate.ToString("F5", CultureInfo.InvariantCulture)
                }
            ]
        };

        var httpMessageHandler = new Mock<HttpMessageHandler>();
        httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(treasuryResponse),
                    System.Text.Encoding.UTF8,
                    "application/json")
            });

        var provider = CreateProvider(httpMessageHandler);

        // act
        var result = await provider.GetRateAsync(currencyCode, purchaseDate, CancellationToken.None);

        // assert
        Assert.True(result.Success);
        Assert.Equal(expectedRate, result.Rate);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    internal async Task GetRateAsync_WhenNoRateDataFound_ReturnsErrorResult()
    {
        // arrange
        var currencyCode = "XYZ";
        var purchaseDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var treasuryResponse = new TreasuryResponse
        {
            Data = []
        };

        var httpMessageHandler = new Mock<HttpMessageHandler>();
        httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(treasuryResponse),
                    System.Text.Encoding.UTF8,
                    "application/json")
            });

        var provider = CreateProvider(httpMessageHandler);

        // act
        var result = await provider.GetRateAsync(currencyCode, purchaseDate, CancellationToken.None);

        // assert
        Assert.False(result.Success);
        Assert.Null(result.Rate);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("No exchange rate data found", result.ErrorMessage);
    }

    [Fact]
    internal async Task GetRateAsync_WhenTreasuryApiUnavailable_ReturnsErrorResult()
    {
        // arrange
        var currencyCode = "EUR";
        var purchaseDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var httpMessageHandler = new Mock<HttpMessageHandler>();
        httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                ReasonPhrase = "Service Unavailable"
            });

        var provider = CreateProvider(httpMessageHandler);

        // act
        var result = await provider.GetRateAsync(currencyCode, purchaseDate, CancellationToken.None);

        // assert
        Assert.False(result.Success);
        Assert.Null(result.Rate);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Treasury API returned 503", result.ErrorMessage);
    }

    private static ExchangeRateProvider CreateProvider(Mock<HttpMessageHandler> handler)
    {
        var client = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://localhost/")
        };

        return new ExchangeRateProvider(client);
    }
}