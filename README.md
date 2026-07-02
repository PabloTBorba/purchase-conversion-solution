# Purchase Conversion Service

## Overview

This project implements a small, production-style .NET 10 service that:

- Stores purchase transactions in USD.
- Retrieves purchases converted to a specified currency using the U.S. Treasury Reporting Rates of Exchange.
- Enforces financial rigor (decimal types, rounding).
- Has no external database server dependency (SQLite file-based DB).
- The SQLite database is created and migrated automatically on application startup.
- Includes automated tests and Swagger/OpenAPI documentation.

> Note: this application uses the U.S. Treasury API to obtain exchange rates. Internet access is required for the conversion functionality.

## Tech Stack

- .NET 10
- ASP.NET Core Minimal API
- EF Core + SQLite
- Swashbuckle (Swagger)
- xUnit, Moq

## Getting Started

### Prerequisites

- .NET 10 SDK installed

### Restore and Build

```bash
dotnet restore
dotnet build
```

### Run the Application

```bash
dotnet run --project src/PurchaseConversion.Api
```

### Tests

This project contains unit and integration tests. To run all tests, execute:
```bash
dotnet test
```

## Running in Development

The Swagger UI is enabled only in `Development`.

### Visual Studio

Open the solution and run the `PurchaseConversion.Api` project using the `http` or `https` profile from `launchSettings.json`. The browser will open automatically on `/swagger`.

### Terminal

You can also run the application from the terminal using the following commands:

```bash
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project src/PurchaseConversion.Api
```

Then open:

- `https://localhost:7264/swagger` or
- `http://localhost:5055/swagger`

## Architectural Choices

### Layered Architecture

The solution uses a lightweight three‑layer architecture:

- **Domain** — business rules, entities, and interfaces  
- **Infrastructure** — persistence and external API integrations  
- **API** — composition root, DI setup, and HTTP endpoints  

This structure keeps the project clean, testable, and easy to run while avoiding unnecessary complexity.

### Why No Application Layer?

The challenge’s scope is intentionally small: one write operation and one read operation.  
Introducing a dedicated Application layer (as seen in full Clean Architecture or CQRS setups) would add ceremony without solving a real problem.

The Domain layer already expresses the business rules clearly, and the Infrastructure layer cleanly isolates external concerns. The architecture is intentionally simple but still follows solid separation‑of‑concerns principles.

If the system grows to include more workflows, reporting, or complex orchestration, an Application layer could be introduced without disrupting the existing design.

### External API Integration

The Domain defines the **IExchangeRateProvider** interface to express the need for exchange rates.

The Infrastructure layer provides the **TreasuryExchangeRateProvider** implementation, which uses a typed `HttpClient` registered in the API layer. This respects dependency inversion and keeps external communication out of the Domain.

### Security

The challenge emphasizes a “plug‑and‑play” experience with no external dependencies or setup steps.  
For that reason, the API is intentionally left open.

In a production environment, the API would be protected using:

- API keys for service‑to‑service communication  
- OAuth2/JWT for user‑facing clients  
- mTLS or signed tokens for internal microservices  
- Rate limiting and abuse protection via an API gateway  

These mechanisms were intentionally omitted to keep the project simple and focused on the core business logic.