# 05 — Integración frontend

> **Última verificación:** 2026-06-02  
> **Fuente de verdad:** `FindMyCoffee-Frontend/src/`, `vite.config.js`  
> **Doc canónica del front:** [`FindMyCoffee-Frontend/Documentation/README.md`](../../FindMyCoffee-Frontend/Documentation/README.md)

Ruta del repo: **`../FindMyCoffee-Frontend`** respecto a fmcbackend.

Este archivo resume el acoplamiento **desde el backend**. Detalle de pantallas, estado y mapa → documentación del repo frontend (índice arriba).

## Arranque

```bash
cd FindMyCoffee-Frontend
npm install --legacy-peer-deps   # react-leaflet@4 requiere peer legacy
npm run dev                      # Vite, proxy /api → 5214
```

Variable `VITE_API_URL`: vacía en dev (usa proxy). En prod: URL absoluta del API.

## Capa API

| Módulo | Rol |
|--------|-----|
| `src/lib/apiClient.js` | `fetch` wrapper, `ApiError`, flag `sessionExpired` en 401/404 |
| `src/api/authApi.js` | login/register consumer & enterprise |
| `src/api/discoveryApi.js` | `GET /api/cafeterias/nearby` |
| `src/api/consumerApi.js` | perfil, PATCH tier |
| `src/api/enterpriseApi.js` | cafetería propia, subscription tier |
| `src/lib/cafeteriaMapper.js` | DTO backend → modelo UI |

## Estado global

| Contexto | Archivo | Responsabilidad |
|----------|---------|-----------------|
| `AuthProvider` | `context/AuthContext.jsx` | JWT en `localStorage`, hydrate perfil, logout, favoritos local |
| `CafeteriasProvider` | `context/CafeteriasContext.jsx` | `/nearby`, geolocalización, radio |

**Token en `/nearby`:** se envía para **consumer y enterprise** (enterprise excluye su local en backend).

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

## Mapa (`CafeteriasMap.jsx`)

- Leaflet + tiles CARTO Voyager (gratis).
- **Usuario:** círculo azul.
- **Cafeterías:** icono **taza** (`lib/mapCoffeeIcon.js`) — ámbar = Enterprise Premium, marrón = Standard.
- Montaje solo en cliente (`mounted` state) por SSR/hydration.

## Navbar

- Saludo **«Hola, {nombre}»** arriba a la derecha (`components/Navbar.jsx`).
- Consumidor: nombre = parte local del email; Enterprise: nombre de cafetería.

## Geolocalización

- `lib/geolocation.js` — fallback coords CABA (`lib/caba.js`) si el browser no entrega GPS.

## Limitaciones UI

- Favoritos: solo `localStorage` (`lib/authStorage.js`).
- Menú/reseñas en detalle: mock/placeholder, no vienen del API.
- Imágenes cover: Unsplash estático en mapper.

## Proxy y puerto

Si el backend no está en 5214, actualizar `vite.config.js` `server.proxy['/api'].target` o usar `VITE_API_URL`.
