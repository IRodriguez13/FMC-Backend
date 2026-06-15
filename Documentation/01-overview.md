# 01 — Visión general

> **Última verificación:** 2026-06-09  
> **Fuente de verdad:** `README.md`, `Fmc.sln`, `../fmcfront/package.json`

## Qué es Find My Coffee (FMC)

Plataforma para **descubrir cafeterías en CABA** registradas por cuentas **Enterprise**, con consumidores **Free/Premium** y planes **Enterprise Standard/Premium** que afectan visibilidad en listados, cupones semanales y métricas del negocio.

## Repositorios

| Repo | Ruta típica | Rol |
|------|-------------|-----|
| **fmcbackend** | `/home/ivanr013/Escritorio/fmcbackend` | API .NET 8, SQLite, JWT, GraphQL opcional |
| **fmcfront** | `../fmcfront` (hermano) | SPA React 18 + Vite + Tailwind |

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
- Proxy dev: `/api` y `/media` → backend local

## Fuera de alcance (MVP actual)

- Menú de productos
- Pasarela de pago real (checkout simula tier vía PATCH + JWT nuevo)
- Canje/redención de cupones en el local
- Notificaciones push, analytics de impresiones en mapa

## Incluido en el MVP actual

- Auth consumer/enterprise, `/nearby`, fotos y reseñas por cafetería
- Favoritos en servidor + sync al login (cache local en front)
- Cupones semanales (enterprise Premium + beneficio FMC)
- Métricas enterprise (`/me/stats`), avatares consumer y enterprise

## Usuarios demo

Contraseña común seed: **`SeedPass-123`**

Ver tabla completa en [06-dev-ops.md](./06-dev-ops.md) y `Infrastructure/Persistence/DataSeeder.cs`.
