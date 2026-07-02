using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PurchaseConversion.Api.Models;
using PurchaseConversion.Api.Models.Responses;
using PurchaseConversion.Api.Tests.Common.Attributes;
using PurchaseConversion.Api.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace PurchaseConversion.Api.Tests;

public sealed class PurchaseApiTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory = factory;

    [Theory, AutoApiData]
    internal async Task CreatePurchase_WhenRequestIsValid_ReturnsCreated(
        PurchaseRequest request)
    {
        // arrange
        request.Description = string.IsNullOrWhiteSpace(request.Description)
            ? "Test purchase"
            : request.Description[..Math.Min(50, request.Description.Length)];

        request.TransactionDate = DateTime.UtcNow;

        request.AmountUsd = request.AmountUsd <= 0
            ? 10.12m
            : decimal.Round(request.AmountUsd, 2, MidpointRounding.AwayFromZero);

        var client = _factory.CreateClient();

        var versioningOptions = _factory.Services
            .GetRequiredService<IOptions<ApiVersioningOptions>>()
            .Value;

        var apiVersion = versioningOptions.DefaultApiVersion.ToString();
        var endpoint = $"/api/v{apiVersion}/purchases";

        // act
        var response = await client.PostAsJsonAsync(
            endpoint,
            request,
            cancellationToken: TestContext.Current.CancellationToken);

        var created = await response.Content.ReadFromJsonAsync<PurchaseCreatedResponse>(
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);

        Assert.NotNull(response.Headers.Location);
        Assert.Equal($"/api/v{apiVersion}/purchases/{created.Id}", response.Headers.Location!.OriginalString);
    }

    [Fact]
    internal async Task CreatePurchase_WhenDescriptionIsBlank_ReturnsBadRequest()
    {
        // arrange
        var client = _factory.CreateClient();

        var versioningOptions = _factory.Services
            .GetRequiredService<IOptions<ApiVersioningOptions>>()
            .Value;

        var apiVersion = versioningOptions.DefaultApiVersion.ToString();

        var request = new PurchaseRequest
        {
            Description = "   ",
            TransactionDate = DateTime.UtcNow,
            AmountUsd = 10.12m
        };

        var endpoint = $"/api/v{apiVersion}/purchases";

        // act
        var response = await client.PostAsJsonAsync(
            endpoint,
            request,
            cancellationToken: TestContext.Current.CancellationToken);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(error);
        Assert.Contains("Description must be 1–50 characters.", error.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-10)]
    internal async Task CreatePurchase_WhenAmountIsLowerOrEqualToZero_ReturnsBadRequest(decimal invalidAmount)
    {
        // arrange
        var client = _factory.CreateClient();

        var versioningOptions = _factory.Services
            .GetRequiredService<IOptions<ApiVersioningOptions>>()
            .Value;

        var apiVersion = versioningOptions.DefaultApiVersion.ToString();

        var request = new PurchaseRequest
        {
            Description = "Invalid purchase",
            TransactionDate = DateTime.UtcNow,
            AmountUsd = invalidAmount
        };

        var endpoint = $"/api/v{apiVersion}/purchases";

        // act
        var response = await client.PostAsJsonAsync(
            endpoint,
            request,
            cancellationToken: TestContext.Current.CancellationToken);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(error);
        Assert.Contains("Amount must be positive.", error.Error);
    }

    [Fact]
    internal async Task ConvertPurchase_WhenPurchaseExists_ReturnsOk()
    {
        // arrange
        var client = _factory.CreateClient();

        var versioningOptions = _factory.Services
            .GetRequiredService<IOptions<ApiVersioningOptions>>()
            .Value;

        var apiVersion = versioningOptions.DefaultApiVersion.ToString();

        var createRequest = new PurchaseRequest
        {
            Description = "Test purchase",
            TransactionDate = new DateTime(2026, 07, 01, 0, 0, 0, DateTimeKind.Utc),
            AmountUsd = 10.12m
        };

        var createEndpoint = $"/api/v{apiVersion}/purchases";
        var createResponse = await client.PostAsJsonAsync(
            createEndpoint,
            createRequest,
            cancellationToken: TestContext.Current.CancellationToken);

        var created = await createResponse.Content.ReadFromJsonAsync<PurchaseCreatedResponse>(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(created);

        var convertEndpoint = $"/api/v{apiVersion}/purchases/{created.Id}/convert/brl";

        // act
        var convertResponse = await client.GetAsync(
            convertEndpoint,
            cancellationToken: TestContext.Current.CancellationToken);

        var converted = await convertResponse.Content.ReadFromJsonAsync<ConvertedPurchaseResponse>(
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, convertResponse.StatusCode);
        Assert.NotNull(converted);

        Assert.Equal(created.Id, converted.Id);
        Assert.Equal("Test purchase", converted.Description);
        Assert.Equal(DateOnly.FromDateTime(createRequest.TransactionDate), converted.TransactionDate);
        Assert.Equal(10.12m, converted.AmountUsd);
        Assert.Equal("BRL", converted.TargetCurrency);
        Assert.Equal(5.25m, converted.ExchangeRate);
        Assert.Equal(53.13m, converted.ConvertedAmount);
    }

    [Fact]
    internal async Task ConvertPurchase_WhenPurchaseDoesNotExist_ReturnsNotFound()
    {
        // arrange
        var client = _factory.CreateClient();

        var versioningOptions = _factory.Services
            .GetRequiredService<IOptions<ApiVersioningOptions>>()
            .Value;

        var apiVersion = versioningOptions.DefaultApiVersion.ToString();
        var purchaseId = Guid.NewGuid();
        const string currency = "BRL";
        var endpoint = $"/api/v{apiVersion}/purchases/{purchaseId}/convert/{currency}";

        // act
        var response = await client.GetAsync(
            endpoint,
            cancellationToken: TestContext.Current.CancellationToken);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("Record not found: Purchase not found.", error.Error);
    }

    [Fact]
    internal async Task ConvertPurchase_WhenIdIsInvalidGuid_ReturnsNotFound()
    {
        // arrange
        var client = _factory.CreateClient();

        var versioningOptions = _factory.Services
            .GetRequiredService<IOptions<ApiVersioningOptions>>()
            .Value;

        var apiVersion = versioningOptions.DefaultApiVersion.ToString();
        var endpoint = $"/api/v{apiVersion}/purchases/not-a-guid/convert/BRL";

        // act
        var response = await client.GetAsync(
            endpoint,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    internal async Task ConvertPurchase_WhenCurrencyIsBlank_ReturnsBadRequest()
    {
        // arrange
        var client = _factory.CreateClient();

        var versioningOptions = _factory.Services
            .GetRequiredService<IOptions<ApiVersioningOptions>>()
            .Value;

        var apiVersion = versioningOptions.DefaultApiVersion.ToString();

        var createRequest = new PurchaseRequest
        {
            Description = "Test purchase",
            TransactionDate = DateTime.UtcNow,
            AmountUsd = 10.12m
        };

        var createEndpoint = $"/api/v{apiVersion}/purchases";
        var createResponse = await client.PostAsJsonAsync(
            createEndpoint,
            createRequest,
            cancellationToken: TestContext.Current.CancellationToken);

        var created = await createResponse.Content.ReadFromJsonAsync<PurchaseCreatedResponse>(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(created);

        var convertEndpoint = $"/api/v{apiVersion}/purchases/{created.Id}/convert/%20";

        // act
        var response = await client.GetAsync(
            convertEndpoint,
            cancellationToken: TestContext.Current.CancellationToken);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(error);
        Assert.Contains("Currency is required.", error.Error);
    }
}