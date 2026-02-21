# Nomnio.WebAPI

ASP.NET Core 10 Web API using Microsoft Orleans 10 (virtual actor model) for stateful grain-based caching of breached email data.

## Architecture

```
HTTP Request → EmailsController → IGrainFactory → CacheGrain (Orleans actor) → IDataSource
```

### Project Structure

| Project | Purpose |
|---|---|
| `Nomnio.WebAPI` | ASP.NET Core host — controllers, DI registration, middleware |
| `Nomnio.WebAPI.Contracts` | Shared interfaces (`ICacheGrain`, `IDataSource`), request/response DTOs |
| `Nomnio.WebAPI.Grains` | Orleans grain implementations (`CacheGrain`) with persistent state |
| `Nomnio.WebAPI.Services` | Utility services (`EmailService`, `InMemoryDataSource`) |
| `Nomnio.WebAPI.Tests` | xUnit tests with Orleans TestCluster integration |

### Key Patterns

- **Orleans Grains** are keyed by **domain** (e.g., `example.com`). Each `CacheGrain` stores a dictionary of breached emails for that domain using persistent state backed by Azure Blob Storage.
- **Cache TTL** is configurable via `appsettings.json` (`Cache:TtlMinutes`, default 5 minutes).
- **Stale-on-error**: If the data source fails and stale cached data exists, the grain returns the stale result.
- **Email normalization**: All emails are trimmed and lowercased via `EmailService.NormalizeEmail()`. Domains are extracted via `EmailService.ExtractDomain()`.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite) (Azure Blob Storage emulator) for grain persistence

Start Azurite before running the API:

```bash
azurite --silent
```

## Build & Run

```bash
# Build the solution
dotnet build Nomnio.WebAPI.sln

# Run the API (http://localhost:5130)
dotnet run --project Nomnio.WebAPI

# Run with HTTPS (https://localhost:7108)
dotnet run --project Nomnio.WebAPI --launch-profile https
```

Swagger UI is available at `/swagger` in Development environment.

## Run Tests

```bash
dotnet test
```

## API Endpoints

### GET /emails/{email}

Look up breach data for an email address. Returns `404` if not found.

```bash
curl http://localhost:5130/emails/user@example.com
```

### POST /emails/{email}

Add a breached email record. Returns `409` if the email already exists.

```bash
curl -X POST http://localhost:5130/emails/user@example.com \
  -H "Content-Type: application/json" \
  -d '{"details": "Breached in 2024 data leak"}'
```

## Configuration

| Setting | Default | Description |
|---|---|---|
| `ConnectionStrings:BlobStorage` | `UseDevelopmentStorage=true` | Azure Blob Storage connection string |
| `Cache:TtlMinutes` | `5` | Cache entry time-to-live in minutes |
