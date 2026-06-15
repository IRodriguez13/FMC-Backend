# 05 — Integración frontend

> **Última verificación:** 2026-06-09  
> **Fuente de verdad:** `../fmcfront/src/`, `vite.config.js`  
> **Doc canónica del front:** [`../fmcfront/Documentation/README.md`](../fmcfront/Documentation/README.md)

Ruta del repo: **`../fmcfront`** respecto a fmcbackend.

Este archivo resume el acoplamiento **desde el backend**. Detalle de pantallas, estado y mapa → documentación del repo frontend (índice arriba).

## Arranque

```bash
cd fmcfront
npm install --legacy-peer-deps   # react-leaflet@4 requiere peer legacy
npm run dev                      # Vite, proxy /api y /media
npm test                         # Vitest (mediaUrl, cafeteriaMapper, userFacingError)
```

Variable `VITE_API_URL`: vacía en dev (usa proxy). En prod: URL absoluta del API.

## Capa API

| Módulo | Rol |
|--------|-----|
| `src/lib/apiClient.js` | `fetch` wrapper, `ApiError`, `sessionExpired` solo en 401 o 404 con detail de auth |
| `src/api/authApi.js` | login/register consumer & enterprise |
| `src/api/discoveryApi.js` | `GET /api/cafeterias/nearby` |
| `src/api/consumerApi.js` | perfil, avatar, tier, favoritos (GET/sync/PUT/DELETE) |
| `src/api/cafeteriaMediaApi.js` | fotos y reseñas por cafetería |
| `src/api/enterpriseApi.js` | cafetería propia, avatar, stats, cupones, subscription tier |
| `src/lib/cafeteriaMapper.js` | DTO backend → modelo UI |
| `src/lib/favoriteMapper.js` | DTO favoritos → tarjeta/lista |
| `src/lib/userFacingError.js` | mensajes UX desde `detail` del backend |

## Estado global

| Contexto | Archivo | Responsabilidad |
|----------|---------|-----------------|
| `AuthProvider` | `context/AuthContext.jsx` | JWT en `localStorage`, hydrate perfil, logout, favoritos (servidor + cache local) |
| `CafeteriasProvider` | `context/CafeteriasContext.jsx` | `/nearby`, geolocalización, radio |
| `ThemeProvider` | `context/ThemeContext.jsx` | modo claro/oscuro |

**Token en `/nearby`:** se envía siempre que exista (consumer **y** enterprise). El backend **no** excluye el local propio del listado.

**Favoritos (modelo híbrido):**

1. `fmc_favorites` en `localStorage` — cache offline y estado inmediato en UI.
2. Al login/registro/hydrate: `PUT /api/consumer/me/favorites/sync` fusiona IDs locales con servidor.
3. `toggleFavorite` actualiza local + `PUT/DELETE` en API (consumer autenticado).

**AbortController:** hydrate auth y load cafeterías cancelan fetch al desmontar (StrictMode).

## Rutas (`App.jsx`)

| Ruta | Página | Navbar |
|------|--------|--------|
| `/` | Home | sí |
| `/explore` | Explore | sí |
| `/map` | MapView | sí |
| `/cafe/:id` | CafeDetail | sí |
| `/login`, `/register`, `/register-business` | auth | no |
| `/profile`, `/favorites` | consumer | sí |
| `/enterprise` | EnterpriseDashboard | sí |
| `/checkout/consumer-premium`, `/checkout/enterprise-premium` | PaymentCheckout | no |
| `/demo`, `/terms` | onboarding / legales | parcial |

## Mapa (`CafeteriasMap.jsx`)

- Leaflet + tiles CARTO Voyager (gratis).
- **Usuario:** círculo azul.
- **Cafeterías:** icono **taza** (`lib/mapCoffeeIcon.js`) — ámbar = Enterprise Premium, marrón = Standard.
- Enterprise logueado ve **todos** los locales listados (incluido el propio si está activo).
- Montaje solo en cliente (`mounted` state) por SSR/hydration.

## Navbar

- Saludo **«Hola, {nombre}»** arriba a la derecha (`components/Navbar.jsx`).
- Consumidor: `displayName` del API o parte local del email; Enterprise: nombre de cafetería + avatar si existe.
- Menú usuario con variantes `dark:`.

## Geolocalización

- `lib/geolocation.js` — fallback coords CABA (`lib/caba.js`) si el browser no entrega GPS.

## Descuentos y cupones (consumidor Premium)

- La API oculta `discountPercent` y cupones a viewers Free.
- Tras activar Premium, `CafeteriasContext.refetch(newToken)` debe usar el JWT nuevo.
- UI: badges en tarjetas, filtro «Con descuento» en Explore, cupones + PDF en CafeDetail.

## Perfil consumidor

- Editable: `displayName`, avatar (`POST /api/consumer/me/avatar`).
- Favoritos: lista desde `GET /me/favorites` (no solo cache `/nearby`).
- Solo lectura: email (identificador de acceso).

## Enterprise dashboard

- Métricas: `GET /me/stats` — «Guardados por usuarios» = conteo server-side de favoritos del local.
- Cupones semanales: CRUD si plan Premium.
- Avatar negocio: `ProfileAvatarEditor` compartido con perfil consumidor.

## Limitaciones UI

- Historial de visitas / direcciones guardadas: placeholder en perfil.
- Detalle cafetería resuelve ID desde cache `/nearby` (no hay GET por id).

## Proxy y puerto

Si el backend no está en 5214, actualizar `VITE_DEV_API_TARGET` en `.env` o `vite.config.js` (`server.proxy` `/api` y `/media`).
