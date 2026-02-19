# TMS

A simple ticket management system built with .NET 9 and Blazor WebAssembly.

## Project overview
- **API**: ASP.NET Core Web API (`TMS.Api`) exposing ticket endpoints, seed data, and CORS configuration.
- **Application**: Domain-facing services and DTOs (`TMS.Application`), including `TicketService` for ticket workflows.
- **Domain**: Core entities and abstractions (`TMS.Domain`) such as `SupportTicket`, `SupportMember`, `Tag`, and repository interfaces.
- **Infrastructure**: EF Core persistence (`TMS.Infrastructure`) with `TmsDbContext`, repositories, migrations, and encryption helpers.
- **Client**: Blazor WebAssembly front-end (`TMS.Client`) with dashboard, ticket submission (attachments/tags), and lanes view.
- **Tests**: Unit tests for application logic (`TMS.Tests`).

## Solution structure
- `TMS.Api/` – Web API, DI wiring, CORS, Serilog, EF migrations, seed data.
- `TMS.Application/` – Services, contracts (requests/responses), and application-level abstractions.
- `TMS.Domain/` – Entities, enums, and repository interfaces.
- `TMS.Infrastructure/` – EF Core DbContext, repositories, migrations, encryption utilities.
- `TMS.Client/` – Blazor WebAssembly UI, pages (`Index`, `Tickets`), shared layout, client config.
- `TMS.Tests/` – Test project for application/services.

## Getting started (dev)
1) Run the API:
```bash
cd TMS.Api
dotnet ef database update
dotnet run
```
2) Run the client:
```bash
cd TMS.Client
dotnet run
```

## Key endpoints
- `POST /api/tickets` – Create a ticket (supports tags/attachment base64).
- `GET /api/tickets` – List tickets (for lanes view).
- `GET /api/tickets/dashboard` – Dashboard stats.
- `POST /api/tickets/{id}/tags` – Attach a tag.
- `DELETE /api/tickets/{id}/tags/{tagId}` – Detach a tag.

## Notes
- CORS origins are configured in `TMS.Api/appsettings.Development.json` under `Cors:AllowedOrigins`.
- Client API base URL is set in `TMS.Client/wwwroot/appsettings.json`.
