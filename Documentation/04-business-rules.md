# 04 — Reglas de negocio

> **Última verificación:** 2026-06-02  
> **Fuente de verdad:** `Domain/Constants/CabaServiceArea.cs`, `Application/Services/CafeteriaDiscoveryService.cs`, `Application/Services/GeoRanking.cs`, `Application/Configuration/DiscoveryOptions.cs`

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

## Tiers consumidor (afectan descubrimiento)

| Tier | Radio max default | Resultados max | Descuentos en API |
|------|-------------------|----------------|-------------------|
| Free | 5 km | 10 | No (`discountPercent` null) |
| Premium | 15 km | 50 | Sí |

Resolución: JWT consumer → `DiscoveryTierResolver.FromHttpContext`. Anon/Enterprise viewer → **Free** (límites Free).

Config: `DiscoveryOptions` en `appsettings` sección `Discovery`.

## Tiers Enterprise (ponderación)

| Tier | Efecto en `/nearby` |
|------|---------------------|
| Standard | Orden por distancia real |
| Premium | **Boost virtual** (~2500 m por defecto) en distancia de ordenamiento |

Implementación: `GeoRanking.EffectiveSortDistanceMeters` — Premium aparece más arriba a igual distancia real.

**Importante:** el orden ponderado es **igual** para consumidor Free y Premium; el tier consumidor no cambia el boost Enterprise.

## Enterprise viendo competencia

- Con JWT Enterprise en `/nearby`, se excluye **su propia** cafetería (`ExcludeCafeteriaId`).
- Objetivo: mapa/listado muestra rivales, no el local propio.

## Descuentos

- Campo `Cafeteria.DiscountPercent` (0–100).
- Solo visible en respuesta si el **viewer** es consumidor **Premium**.

## Seed demo

`DataSeeder.EnsureCabaDemoAsync` — idempotente; corrige datos viejos (p. ej. coords Madrid) y asegura 4 cafeterías + usuarios seed en CABA. GUIDs fijos en `DataSeeder.cs`.

## Lo que NO hace el backend

- Bloquear que un Enterprise vea precios/descuentos de otros (no aplica descuentos a Enterprise viewer).
- Favoritos, menú, reviews.
