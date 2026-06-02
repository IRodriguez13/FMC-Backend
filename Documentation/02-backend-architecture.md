# 02 — Arquitectura backend

> **Última verificación:** 2026-06-02  
> **Fuente de verdad:** `Api/Program.cs`, `Application/DependencyInjection.cs`, `Infrastructure/DependencyInjection.cs`

## Capas

```
Api/           → HTTP (REST minimal endpoints, middleware, GraphQL)
Application/   → servicios, contratos DTO, reglas de aplicación
Infrastructure/→ EF Core, repos, JWT, BCrypt
Domain/        → entidades, enums, CabaServiceArea
Api.Tests/     → tests unitarios (Moq)
```

## Arranque (`Api/Program.cs`)

1. Serilog + DI (`AddApplicationServices`, `AddInfrastructure`, GraphQL)
2. JWT (`JwtOptions` en `appsettings`)
3. Pipeline: correlación → **ExceptionHandlingMiddleware** → Swagger (dev) → auth
4. `MapFmcEndpoints()` + `MapFmcGraphQL()`
5. **Migraciones** + SQLite PRAGMA (`journal_mode=WAL`, `busy_timeout=5000`)
6. **`DataSeeder.EnsureCabaDemoAsync`** — idempotente en cada arranque

## Persistencia

- **DbContext:** `Infrastructure/Persistence/AppDbContext.cs`
- **SQLite connection string:** `ConnectionStrings:Default`
  - Local dotnet: `fmc.db` (gitignored)
  - Docker: `Data Source=/data/fmc.db` → montaje `./docker-data`
- DI añade `Cache=Shared` y `Default Timeout=30` si no están en la cadena

## Entidades principales

| Entidad | Relación | Notas |
|---------|----------|-------|
| `ConsumerUser` | — | tier Free/Premium |
| `EnterpriseUser` | 1:1 `Cafeteria` | `SubscriptionTier` Standard/Premium |
| `Cafeteria` | — | `ListingActive`, coords, `DiscountPercent` |

## Middleware relevante

| Middleware | Archivo | Comportamiento |
|------------|---------|----------------|
| Exception handling | `Api/Middleware/ExceptionHandlingMiddleware.cs` | 4xx → warning; cancelación cliente → 499/504 sin ERR; ProblemDetails JSON |
| Correlation ID | `Api/Middleware/CorrelationIdMiddleware.cs` | Trazabilidad requests |

## GraphQL

Espejo parcial de REST en `Api/GraphQL/FmcQuery.cs` (`nearby`, perfil consumidor, cafetería enterprise). **El front usa solo REST.**

## Tests

```bash
make test   # 28 tests (estado 2026-06-02)
```

Suites clave: `CafeteriaDiscoveryServiceTests`, `DiscoveryTierResolverTests`, `CabaServiceAreaTests`, `GeoRankingTests`.
