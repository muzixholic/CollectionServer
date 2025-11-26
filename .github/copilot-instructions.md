# CollectionServer AI Coding Guidelines

## Architecture Overview
- **Clean Architecture**: API → Core → Infrastructure layers
- **Database-First Strategy**: PostgreSQL lookup (<500ms) → External API fallback (<2s)
- **Priority-Based Fallback**: Providers implement `IMediaProvider` with configurable priority
- **Media Types**: Books (ISBN), Movies (UPC/EAN), Music (UPC) with multiple external sources

## Key Patterns
- **Provider Registration**: Add new `IMediaProvider` implementations in `ServiceCollectionExtensions.AddExternalApiSettings()`
- **Barcode Validation**: Use `BarcodeValidator` singleton for format checking
- **Error Handling**: Korean error messages via `ErrorHandlingMiddleware`
- **Configuration**: API keys via `dotnet user-secrets`, priorities in `appsettings.json`

## Development Workflow
- **Database Migrations**: `dotnet ef database update --project src/CollectionServer.Infrastructure --startup-project src/CollectionServer.Api`
- **API Keys Setup**: `dotnet user-secrets set "ExternalApis:{Provider}:ApiKey" "key"`
- **Testing**: `dotnet test` (Unit/Integration/Contract tests separated)
- **Container Run**: `podman-compose up -d` for full stack

## Code Conventions
- **Async/Await**: All I/O operations use `CancellationToken`
- **Logging**: `ILogger<T>` injection with structured logging
- **HTTP Clients**: `IHttpClientFactory` with timeout and auth headers
- **Entity Mapping**: Direct mapping from API responses to `MediaItem` entities
- **Exception Handling**: Catch and log, return `null` for provider failures

## External API Integration
- **Timeout**: 30s default, configurable per provider
- **Retry Logic**: EF Core auto-retry, manual for APIs
- **Response Parsing**: `System.Text.Json` with case-insensitive deserialization
- **Rate Limiting**: 100 req/min (200 in prod) with queue

## Examples
- **New Provider**: Implement `IMediaProvider`, add to DI, configure in `ExternalApiSettings`
- **Barcode Support**: Check length (10/13) and prefixes (978/979 for ISBN)
- **Fallback Chain**: Service iterates providers by priority until success

## Testing Strategy
- **Unit Tests**: Mock external APIs, test validation logic
- **Integration Tests**: Real DB, mock HTTP clients
- **Contract Tests**: OpenAPI compliance validation