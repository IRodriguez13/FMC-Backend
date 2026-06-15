# 03 — API REST

> **Última verificación:** 2026-06-09  
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

**Respuesta:** `NearbyCafeteriasResponse` — items con `subscriptionTier`, `distanceMeters`, `coverImageUrl`, `averageRating`, `reviewCount`, `discountPercent` (null si viewer Free).

**Comportamiento con JWT Enterprise:** mismo listado que anon/consumer (incluye **su propio** local si está activo). El tier consumidor del viewer sigue siendo **Free** (límites y sin `discountPercent`).

**Cupones por cafetería:** `GET /{cafeteriaId}/coupons` — requiere JWT consumidor **Premium**; devuelve beneficio FMC + cupones del negocio vigentes esta semana.

---

## Consumidor — `/api/consumer` (rol `consumer`)

| Método | Ruta | Body | Respuesta |
|--------|------|------|-----------|
| GET | `/me` | — | `ConsumerProfileDto` |
| PUT | `/me` | `{ displayName? }` | `ConsumerProfileDto` actualizado |
| POST | `/me/avatar` | `multipart/form-data` campo `file` | `ConsumerProfileDto` con `avatarUrl` |
| PATCH | `/tier` | `{ tier }` | `{ token, profile }` — JWT nuevo |

### Favoritos — `/api/consumer/me/favorites`

| Método | Ruta | Body | Respuesta |
|--------|------|------|-----------|
| GET | `/me/favorites` | — | `ConsumerFavoritesResponse` (ítems con nombre, cover, rating, tier) |
| GET | `/me/favorites/ids` | — | `{ cafeteriaIds: Guid[] }` |
| PUT | `/me/favorites/sync` | `Guid[]` (IDs locales) | merge servidor ∪ local → `{ cafeteriaIds }` |
| PUT | `/me/favorites/{cafeteriaId}` | — | 204 — agrega favorito |
| DELETE | `/me/favorites/{cafeteriaId}` | — | 204 — quita favorito |

Persistencia: entidad `ConsumerFavorite` (1 fila por consumidor + cafetería).

**`ConsumerProfileDto`:** `id`, `email`, `displayName`, `tier`, `avatarUrl` (null si sin foto).

- `displayName`: si no está guardado, el servicio deriva la parte local del email.
- **Email no editable** por API (identificador de acceso).
- Avatar: JPEG/PNG/WebP; rate limit `upload`.

404 `KeyNotFoundException` si el `sub` del JWT no existe (p. ej. tras `reset-db`).

---

## Enterprise — `/api/enterprise/cafeteria` (rol `enterprise`)

| Método | Ruta | Body | Respuesta |
|--------|------|------|-----------|
| GET | `/me` | — | `EnterpriseCafeteriaDto` (`avatarUrl` incluido) |
| PUT | `/me` | datos cafetería | DTO actualizado; coords deben estar en CABA para `ListingActive` |
| POST | `/me/avatar` | `multipart/form-data` campo `file` | DTO con `avatarUrl` |
| DELETE | `/me/avatar` | — | DTO sin avatar |
| PATCH | `/subscription-tier` | `{ subscriptionTier }` | `{ token, role, cafeteriaId, enterpriseSubscriptionTier }` |
| GET | `/me/stats` | — | `EnterpriseCafeteriaStatsDto` (rating, reseñas, fotos, favoritos de usuarios, cupones semana, plan) |
| GET | `/coupons` | — | `EnterpriseCouponDto[]` (gestión; crear requiere Premium) |
| POST | `/coupons` | `EnterpriseCouponCreateRequest` | 201 — máx. 3 cupones/semana (Premium) |
| DELETE | `/coupons/{couponId}` | — | 204 |

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
