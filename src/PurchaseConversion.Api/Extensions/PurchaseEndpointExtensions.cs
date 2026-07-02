using PurchaseConversion.Api.Models;
using PurchaseConversion.Api.Models.Responses;
using PurchaseConversion.Domain.Services;

namespace PurchaseConversion.Api.Extensions;

public static class PurchaseEndpointsExtensions
{
    public static IEndpointRouteBuilder MapPurchaseEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.BuildApiVersionSet();
        var v1 = app.MapGroup("/api/v{version:apiVersion}")
            .WithApiVersionSet(versionSet);

        v1.MapPost("/purchases", async (
            HttpContext httpContext,
            PurchaseRequest req,
            PurchaseService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var id = await service.StorePurchaseAsync(
                    req.Description,
                    DateOnly.FromDateTime(req.TransactionDate),
                    req.AmountUsd,
                    cancellationToken);

                var version = httpContext.Request.RouteValues["version"]?.ToString() ?? "1.0";
                return Results.Created($"/api/v{version}/purchases/{id}", new PurchaseCreatedResponse(id));

            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse($"Invalid request: {ex.Message}"));
            }
        })
        .WithName("CreatePurchase")
        .WithSummary("Store a purchase transaction in USD.")
        .WithDescription("Stores a purchase with description, date, and USD amount.")
        .Produces<PurchaseCreatedResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .MapToApiVersion(1, 0);

        v1.MapGet("/purchases/{id:guid}/convert/{currency}",
            async (Guid id, string currency, PurchaseService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.ConvertAsync(id, currency, cancellationToken);
                    return Results.Ok(result);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new ErrorResponse($"Invalid request: {ex.Message}"));
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.NotFound(new ErrorResponse($"Record not found: {ex.Message}"));
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new ErrorResponse($"Invalid operation: {ex.Message}"));
                }
            })
        .WithName("ConvertPurchase")
        .WithSummary("Retrieve a purchase converted to a target currency.")
        .WithDescription("Returns the purchase with original USD amount, exchange rate used, and converted amount.")
        .Produces<ConvertedPurchaseResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .MapToApiVersion(1, 0);

        return app;
    }
}