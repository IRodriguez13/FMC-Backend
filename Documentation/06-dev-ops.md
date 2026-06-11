# 06 — Dev, ops y troubleshooting

> **Última verificación:** 2026-06-11  
> **Fuente de verdad:** `Makefile`, `docker-compose.yml`, `README.md`

## Comandos backend (fmcbackend)

| Comando | Uso |
|---------|-----|
| `make up` | Docker Compose build + API en background |
| `make down` | Parar contenedor |
| `make reset-db` | Borrar SQLite en `docker-data/` (sin sudo) |
| `make fix-docker-data-perms` | Si `docker-data/` quedó como root tras `sudo make` |
| `make logs` | Logs contenedor |
| `make test` | Tests unitarios |
| `make migrate` | Aplicar migraciones EF (`Api/fmc.db`) |
| `make migrations-list` | Listar migraciones EF |
| `make run` | `migrate` + API local dotnet (puerto libre 5214–5230 o `PORT=`) |
| `make smoke` / `make smoke-full` | Scripts bash contra API |

## Stack completo local

```bash
# Terminal 1 — backend
cd fmcbackend
make fix-docker-data-perms   # solo si hace falta
make reset-db && make up

# Terminal 2 — frontend
cd ../fmcfront
npm install --legacy-peer-deps
npm run dev
```

Swagger: `http://127.0.0.1:5214/swagger`

## Seed — cuentas demo

Contraseña: **`SeedPass-123`**

| Rol | Email |
|-----|--------|
| Enterprise Premium (Palermo) | `enterprise-premium@seed.fmc` |
| Enterprise Standard (San Telmo) | `enterprise-standard@seed.fmc` |
| Enterprise Premium (Recoleta) | `enterprise-recoleta@seed.fmc` |
| Enterprise Standard (Caballito) | `enterprise-caballito@seed.fmc` |
| Consumidor Free | `consumidor@seed.fmc` |
| Consumidor Premium | `consumidor-premium@seed.fmc` |

## SQLite

| Entorno | Archivo |
|---------|---------|
| Docker | `./docker-data/fmc.db` |
| dotnet local | `./Api/fmc.db` o raíz según cwd |

Consulta: `sqlite3 docker-data/fmc.db`

## Problemas frecuentes

| Síntoma | Causa probable | Acción |
|---------|----------------|--------|
| `/nearby` vacío | Coords fuera CABA o BD sin seed CABA | `make reset-db && make up`; front usa fallback Obelisco |
| 404 `/consumer/me` tras reset | JWT viejo en localStorage | Logout o borrar storage; re-login seed |
| 500 + TaskCanceled en logs | Fetch abortado (StrictMode) o cliente cerró | Normal tras fix middleware; ignorar si 499 |
| `rm docker-data`: permiso denegado | `sudo make` previo | `make fix-docker-data-perms` |
| Mapa gris | CSS altura / react-leaflet | Ver `index.css` `.fmc-map-shell` |
| Front no llega al API | Proxy 5214 vs puerto real | Alinear `vite.config.js`, `VITE_DEV_API_TARGET` y `FMC_HTTP_PORT` |
| API crash al arrancar (`AvatarStorageKey`) | Migración pendiente | `make migrate` antes de `make run` |
| Fotos seed 1×1 o rotas | PNG legacy con bytes JPEG | `make migrate && make run`; URLs `/media/seed-*.jpg`; redirect PNG→JPG en API |
| Tarjetas sin imagen (ícono roto) | Front apunta a otro puerto que el API | En `fmcfront/.env`: `VITE_DEV_API_TARGET=http://127.0.0.1:<puerto make run>`; reiniciar `npm run dev` |

## Docker

- Imagen: `fmc-api:local`
- Puerto host: `FMC_HTTP_PORT` (default 5214) → 8080 contenedor
- Env JWT: `JWT_KEY` en `.env` (mín. 32 chars)

## Usuario local «Juan» (solo dev)

Existe en **`Api/fmc.db`** (dotnet local), **no** en Docker seed: email `juan`, password `root`. No confundir con cuentas seed.
