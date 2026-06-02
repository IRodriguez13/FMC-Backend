# 01 — Visión general

> **Última verificación:** 2026-06-02  
> **Fuente de verdad:** `README.md`, `Fmc.sln`, `FindMyCoffee-Frontend/package.json`

## Qué es Find My Coffee (FMC)

Plataforma para **descubrir cafeterías en CABA** registradas por cuentas **Enterprise**, con consumidores **Free/Premium** y planes **Enterprise Standard/Premium** que afectan visibilidad en listados.

## Repositorios

| Repo | Ruta típica | Rol |
|------|-------------|-----|
| **fmcbackend** | `/home/ivanr013/Escritorio/fmcbackend` | API .NET 8, SQLite, JWT, GraphQL opcional |
| **FindMyCoffee-Frontend** | `../FindMyCoffee-Frontend` (hermano) | SPA React 18 + Vite + Tailwind |

El workspace Cursor actual suele ser **fmcbackend**; el front vive en directorio hermano, no submódulo.

## Stack

**Backend**

- .NET 8 minimal API (`Api/Program.cs`)
- Capas: `Domain` → `Application` → `Infrastructure` → `Api`
- EF Core + SQLite (`fmc.db` local o `docker-data/fmc.db`)
- JWT Bearer, Swagger, Serilog, Hot Chocolate GraphQL (`Api/GraphQL/`)

**Frontend**

- Vite, React 18, React Router, Tailwind
- Leaflet + react-leaflet 4.x (mapa OSM/CARTO, sin API key)
- Proxy dev: `/api` → `http://127.0.0.1:5214`

## Fuera de alcance (MVP actual)

- Menú, reseñas, valoraciones reales en API
- CRUD admin de todas las entidades
- Favoritos en backend (solo `localStorage` en front)
- Pagos reales (cambio de tier simulado vía PATCH + JWT nuevo)

## Usuarios demo

Contraseña común seed: **`SeedPass-123`**

Ver tabla completa en [06-dev-ops.md](./06-dev-ops.md) y `Infrastructure/Persistence/DataSeeder.cs`.
