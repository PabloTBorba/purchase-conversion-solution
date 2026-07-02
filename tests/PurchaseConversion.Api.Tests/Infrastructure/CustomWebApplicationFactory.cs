using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PurchaseConversion.Domain.Interfaces;
using PurchaseConversion.Domain.Models;
using PurchaseConversion.Infrastructure.Persistence;

namespace PurchaseConversion.Api.Tests.Infrastructure;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"purchase-conversion-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PurchaseDbContext>>();
            services.RemoveAll<PurchaseDbContext>();

            services.AddDbContext<PurchaseDbContext>(options =>
                options.UseSqlite($"Data Source={_databasePath}"));

            services.RemoveAll<IExchangeRateProvider>();
            services.AddSingleton<IExchangeRateProvider>(new FixedExchangeRateProvider(5.25m));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        try
        {
            if (File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }
        }
        catch
        {
            // no-op so the test results are not affected by the cleanup failure
        }
    }

    private sealed class FixedExchangeRateProvider(decimal rate) : IExchangeRateProvider
    {
        public Task<ExchangeRateResult> GetRateAsync(
            string currencyCode,
            DateOnly purchaseDate,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ExchangeRateResult(true, rate, null));
        }
    }
}