using Microsoft.EntityFrameworkCore;
using PurchaseConversion.Domain.Services;
using PurchaseConversion.Domain.Interfaces;
using PurchaseConversion.Infrastructure.Persistence;
using PurchaseConversion.Infrastructure.Repositories;
using PurchaseConversion.Infrastructure.Services;
using PurchaseConversion.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// DbContext (SQLite file, no external server)
builder.Services.AddDbContext<PurchaseDbContext>(options =>
    options.UseSqlite("Data Source=purchases.db"));

// Domain + infrastructure
builder.Services.AddScoped<IPurchaseRepository, PurchaseRepository>();
builder.Services.AddScoped<PurchaseService>();

var treasuryApiBaseUrl = builder.Configuration["TreasuryApi:BaseUrl"]?.Trim();
if (string.IsNullOrWhiteSpace(treasuryApiBaseUrl))
{
    throw new InvalidOperationException("Configuration key 'TreasuryApi:BaseUrl' is required.");
}

if (!treasuryApiBaseUrl.EndsWith('/'))
{
    treasuryApiBaseUrl += "/";
}

var treasuryApiTimeoutSeconds = builder.Configuration.GetValue<int?>("TreasuryApi:TimeoutSeconds") ?? 30;
if (treasuryApiTimeoutSeconds <= 0)
{
    throw new InvalidOperationException("Configuration key 'TreasuryApi:TimeoutSeconds' must be greater than zero.");
}

builder.Services.AddHttpClient<IExchangeRateProvider, ExchangeRateProvider>(client =>
{
    client.BaseAddress = new Uri(treasuryApiBaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(treasuryApiTimeoutSeconds);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();
builder.Services.AddApiVersioningServices();

var app = builder.Build();

// Ensure DB exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PurchaseDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.MapPurchaseEndpoints();

app.Run();
