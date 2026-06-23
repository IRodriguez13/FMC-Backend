# 04 — Reglas de negocio

> **Última verificación:** 2026-06-09  
> **Fuente de verdad:** `Domain/Constants/CabaServiceArea.cs`, `Application/Services/CafeteriaDiscoveryService.cs`, `Application/Services/GeoRanking.cs`, `Application/Services/CouponWeek.cs`, `Application/Configuration/DiscoveryOptions.cs`

## Área de servicio: solo CABA

- Bounding box WGS84 en `CabaServiceArea` (aprox. -34.705…-34.527 lat, -58.535…-58.335 lng).
- Centro referencia (Obelisco): `-34.6037`, `-58.3816`.
- `/nearby` con query fuera de CABA → `ArgumentException` → HTTP 400.
- Alta/edición Enterprise: coordenadas en CABA requeridas para activar listado (`LocationValidation`).

## Listado en descubrimiento

Una cafetería aparece en `/nearby` solo si:

1. Tiene cuenta **Enterprise** vinculada.
2. **`ListingActive = true`** (registro completado).

Repo: `ICafeteriaRepository.GetListedForDiscoveryAsync`.

**Propio local:** Enterprise **sí** aparece en `/nearby` (mismo criterio que el resto). No hay exclusión por `cafeteria_id`.

## Tiers consumidor (afectan descubrimiento)

| Tier | Radio max default | Resultados max | Descuentos en API |
|------|-------------------|----------------|-------------------|
| Free | 5 km | 10 | No (`discountPercent` null) |
| Premium | 15 km | 50 | Sí + cupones semanales en ficha |

Resolución: JWT consumer → `DiscoveryTierResolver.FromHttpContext`. Anon/Enterprise viewer → **Free** (límites Free).

Config: `DiscoveryOptions` en `appsettings` sección `Discovery`.

## Tiers Enterprise (ponderación)

| Tier | Efecto en `/nearby` | Cupones del negocio |
|------|---------------------|---------------------|
| Standard | Orden por distancia real | No puede publicar cupones custom |
| Premium | **Boost virtual** (~2500 m por defecto) en distancia de ordenamiento | Hasta **3 cupones/semana** |

Implementación: `GeoRanking.EffectiveSortDistanceMeters` — Premium aparece más arriba a igual distancia real.

**Importante:** el orden ponderado es **igual** para consumidor Free y Premium; el tier consumidor no cambia el boost Enterprise.

## Descuentos y cupones semanales

**Semana:** lunes 00:00 → domingo 23:59 (`America/Argentina/Buenos_Aires`) — `CouponWeek.CurrentBounds()`.

| Origen | Quién lo financia | Quién lo ve |
|--------|-------------------|-------------|
| **Beneficio FMC** (`Cafeteria.DiscountPercent`) | Plataforma (plan consumidor Premium) | Consumidor **Premium** |
| **Cupón del negocio** (`EnterpriseCoupon`) | Enterprise **Premium** | Consumidor **Premium** en `GET /api/cafeterias/{id}/coupons` |

- `discountPercent` (0–100): configurable en panel enterprise; solo se **expone en API** a consumidor Premium.
- Cupones negocio: CRUD `POST/GET/DELETE /api/enterprise/cafeteria/coupons` (crear solo Enterprise Premium).

## Favoritos

- Persistidos en servidor: `ConsumerFavorite` (1 fila por consumidor + cafetería).
- API: `GET/PUT/DELETE /api/consumer/me/favorites`, `PUT /me/favorites/sync`.
- Enterprise ve **conteo agregado** en `GET /api/enterprise/cafeteria/me/stats` (`favoriteCount` = usuarios que guardaron el local).

## Reseñas y fotos

- **Reseñas:** consumidor o enterprise; 1 reseña por autor y local; foto opcional.
- **Galería del local:** solo fotos subidas por enterprise dueño; portada en `/nearby` = última foto enterprise.
- **Avatares:** consumidor (`/api/consumer/me/avatar`) y enterprise (`/api/enterprise/cafeteria/me/avatar`).

## Seed demo

`DataSeeder.EnsureCabaCatalogAsync` — idempotente; **22 cafeterías** + usuarios seed en CABA (`CabaCatalogSeed.cs`), cupones demo, fotos/reseñas desde `Api/SeedAssets` y avatares enterprise Premium. GUIDs fijos en catálogo.

## Lo que NO hace el backend

- Bloquear que un Enterprise viewer vea `discountPercent` de otros (no aplica: viewer enterprise = límites Free, sin descuentos en API).
- Pasarela de pago real ni canje de cupones en el local.
- Menú de productos, notificaciones push, analytics de impresiones en mapa.
