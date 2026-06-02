# 03 — API REST

> **Última verificación:** 2026-06-02  
> **Fuente de verdad:** `Api/Endpoints/FmcEndpoints.cs`, `Application/Contracts/*.cs`

Base URL local: `http://127.0.0.1:5214` (Docker: mismo puerto host vía `FMC_HTTP_PORT`).

Auth: header `Authorization: Bearer {jwt}` salvo endpoints `AllowAnonymous`.

Enums JSON en **camelCase/string** (`JsonStringEnumConverter`).

---

## Auth — `/api/auth`

| Método | Ruta | Auth | Body | Respuesta |
|--------|------|------|------|-----------|
| POST | `/consumer/register` | — | `{ email, password }` | `{ token, role, ... }` |
| POST | `/consumer/login` | — | `{ email, password }` | idem |
| POST | `/enterprise/register` | — | ver `EnterpriseRegisterRequest` | JWT Enterprise + `cafeteriaId` |
| POST | `/enterprise/login` | — | `{ email, password }` | idem |

**Claims JWT Enterprise:** `sub` (user id), `role=enterprise`, `cafeteria_id`, `enterprise_subscription_tier`.

**Claims JWT Consumer:** `sub`, `role=consumer`, `tier` (Free/Premium).

---

## Descubrimiento — `/api/cafeterias`

| Método | Ruta | Auth | Query | Notas |
|--------|------|------|-------|-------|
| GET | `/nearby` | opcional | `lat`, `lng`, `radiusKm?` | Anon = tier Free; consumer JWT = límites/descuentos Premium |

**Respuesta:** `NearbyCafeteriasResponse` — items con `subscriptionTier`, `distanceMeters`, `discountPercent` (null si viewer Free).

**Comportamiento con JWT Enterprise:** excluye la cafetería del claim `cafeteria_id` (solo competencia).

Implementación: `DiscoveryTierResolver.ExcludeOwnCafeteriaId` + `NearbyQuery.ExcludeCafeteriaId`.

---

## Consumidor — `/api/consumer` (rol `consumer`)

| Método | Ruta | Body | Respuesta |
|--------|------|------|-----------|
| GET | `/me` | — | `ConsumerProfileDto` |
| PATCH | `/tier` | `{ tier }` | `{ token, profile }` — JWT nuevo |

404 `KeyNotFoundException` si el `sub` del JWT no existe (p. ej. tras `reset-db`).

---

## Enterprise — `/api/enterprise/cafeteria` (rol `enterprise`)

| Método | Ruta | Body | Respuesta |
|--------|------|------|-----------|
| GET | `/me` | — | `EnterpriseCafeteriaDto` |
| PUT | `/me` | datos cafetería | DTO actualizado; coords deben estar en CABA para `ListingActive` |
| PATCH | `/subscription-tier` | `{ subscriptionTier }` | `{ token, role, cafeteriaId, enterpriseSubscriptionTier }` |

---

## Errores

Formato **ProblemDetails** JSON vía middleware global.

| Excepción | HTTP |
|-----------|------|
| `UnauthorizedAccessException` | 401 |
| `KeyNotFoundException` | 404 |
| `InvalidOperationException` | 409 |
| `ArgumentException` (incl. fuera CABA) | 400 |
| `OperationCanceledException` (cliente abortó) | 499 / 504 |

Swagger: `/swagger` en Development.
