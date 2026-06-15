# FMC Backend

Find my coffee (**FMC**): API en `Api/` (`Fmc.Api.csproj`), solución `Fmc.sln`, tests `Api.Tests/`. SQLite por defecto en `fmc.db` (ignorado en `.gitignore`). Ver `Makefile` para `build`, `test`, `run`, etc.

**Documentación ampliada (arquitectura, API, reglas, front):** [`Documentation/README.md`](./Documentation/README.md)

## Reglas de negocio (resumen)

Referencia rápida. Detalle y fuentes en [`Documentation/04-business-rules.md`](./Documentation/04-business-rules.md).

### Área y listado

| Regla | Comportamiento |
|-------|----------------|
| **Zona** | Solo **CABA** (`CabaServiceArea`). `/nearby` fuera del bbox → 400. Alta/edición enterprise: coords en CABA para `ListingActive`. |
| **Visible en Explorar/Mapa** | Cafetería con cuenta **Enterprise** y **`ListingActive = true`** (nombre válido, coords ≠ 0, dentro de CABA). |
| **Propio local** | Enterprise **sí** ve su cafetería en `/nearby` (mismo listado que el resto). |

### Tiers consumidor

| Tier | Radio máx. | Resultados máx. | Beneficios |
|------|------------|-----------------|------------|
| **Free** | 5 km | 10 | Sin `discountPercent` en API; sin cupones |
| **Premium** | 15 km | 50 | Ve `%` del local; cupones semanales (ver abajo) |

Sin JWT consumidor (anon o enterprise en mapa) → límites **Free**. Config: `DiscoveryOptions` en `appsettings`.

### Tiers Enterprise

| Tier | Efecto en `/nearby` | Cupones del negocio |
|------|---------------------|---------------------|
| **Standard** | Orden por distancia real | No puede publicar cupones custom |
| **Premium** | Boost virtual ~**2500 m** en ordenamiento | Hasta **3 cupones/semana** (% , monto fijo ARS o 2x1) |

El boost Enterprise es **igual** para viewers consumidor Free y Premium.

### Descuentos y cupones semanales

**Semana:** lunes 00:00 → domingo 23:59 (`America/Argentina/Buenos_Aires`).

| Origen | Quién lo financia | Quién lo ve | Condición |
|--------|-------------------|-------------|-----------|
| **Beneficio FMC** | Plataforma (plan consumidor Premium) | Consumidor **Premium** | `Cafeteria.DiscountPercent` > 0 |
| **Cupón del negocio** | Enterprise **Premium** | Consumidor **Premium** | Publicado por el dueño esta semana |

- `discountPercent` (0–100): cualquier enterprise lo configura en su panel; solo se **expone en API** a consumidor Premium.
- Cupones del negocio: CRUD en `POST/GET/DELETE /api/enterprise/cafeteria/coupons` (solo enterprise Premium).
- Listado público para ficha: `GET /api/cafeterias/{id}/coupons` (requiere JWT consumidor Premium para ver datos).

### Favoritos

- Persistidos en servidor: `ConsumerFavorite` (1 fila por consumidor + cafetería).
- API: `GET/PUT/DELETE /api/consumer/me/favorites`, `PUT /me/favorites/sync` (merge al login).
- Enterprise ve **conteo** en `GET /api/enterprise/cafeteria/me/stats`.

### Contenido y perfiles

| Recurso | Regla |
|---------|--------|
| **Galería del local** | Solo fotos subidas por **enterprise** dueño; portada = última foto enterprise |
| **Reseñas** | Consumidor o enterprise; 1 reseña por autor y local; foto opcional en reseña |
| **Avatar consumidor** | `POST/DELETE /api/consumer/me/avatar`; persiste en storage |
| **Avatar enterprise** | `POST/DELETE /api/enterprise/cafeteria/me/avatar`; foto del negocio en navbar y panel |

### Métricas enterprise

`GET /api/enterprise/cafeteria/me/stats`: valoración, reseñas, fotos, favoritos, cupones activos esta semana, plan y vigencia semanal.

### Cambio de plan (demo)

| Acción | Endpoint | Efecto |
|--------|----------|--------|
| Consumidor → Premium | `PATCH /api/consumer/tier` | JWT nuevo con tier |
| Enterprise → Premium | `PATCH /api/enterprise/cafeteria/subscription-tier` | JWT nuevo + boost/cupones |

### Lo que este backend no modela

Pasarela de pago real, canje/redención de cupones en local, menú, notificaciones push, analytics de impresiones en mapa.

## Seed demo (BD vacía tras migraciones)

Al arrancar la API se ejecuta **`MigrateAsync`** y, si no hay cuentas Enterprise, el **seed en código** (`Program.cs`). Eso **versiona los datos de prueba en Git** igual que el esquema: cada dev que borre `docker-data/` (o clone fresco) obtiene **los mismos usuarios, emails y GUIDs** sin commitear ningún `.db`.

Contraseña común: `SeedPass-123`

Cuatro cafeterías seed en barrios de **CABA** (Palermo, San Telmo, Recoleta, Caballito), centradas en el Obelisco para `/nearby` y el mapa.

| Rol | Email |
|-----|--------|
| Enterprise Premium (Palermo) | `enterprise-premium@seed.fmc` |
| Enterprise Standard (San Telmo) | `enterprise-standard@seed.fmc` |
| Enterprise Premium (Recoleta) | `enterprise-recoleta@seed.fmc` |
| Enterprise Standard (Caballito) | `enterprise-caballito@seed.fmc` |
| Consumidor Free | `consumidor@seed.fmc` |
| Consumidor Premium | `consumidor-premium@seed.fmc` |

## ¿Qué puedes probar en la API? (no es CRUD completo de entidades)

- **Auth / alta**: registro y login de consumidor y Enterprise (`/api/auth/...`).
- **Consumidor** (JWT consumidor): perfil, avatar, favoritos, tier Premium.
- **Enterprise** (JWT Enterprise): cafetería propia, fotos, cupones (Premium), métricas, plan Enterprise.
- **Descubrimiento**: `/nearby`, fotos/reseñas/cupones por cafetería.

### Puerto ocupado (`address already in use`)

Con **`make run`** / **`make watch`**, si **no** defines **`PORT`**, Make elige el **primer puerto libre** entre **5214 y 5230** en `127.0.0.1` (script `scripts/pick-free-port.sh`), así no choca con Docker u otro `dotnet` en 5214. Para forzar uno concreto: `make run PORT=5281`.

## Docker (API + SQLite en disco)

SQLite sigue siendo **un archivo**; no hay servidor SQLite en red. En Compose el archivo queda en **`./docker-data/fmc.db`** (montaje en el contenedor en `/data/fmc.db`) para que puedas abrirlo desde tu máquina sin entrar al contenedor.

```bash
cp .env.example .env          # opcional: FMC_HTTP_PORT, JWT_KEY
make up                       # docker compose up -d --build
make smoke                    # registros de prueba (usa FMC_HTTP_PORT / .env)
make logs                     # ver logs del contenedor api
make down                     # parar
```

**No uses `sudo make`** (deja `docker-data/` como root). Para resetear la BD: `make reset-db && make up`. Si `rm` falla con permiso denegado: `make fix-docker-data-perms` y luego `make reset-db`.

- **Swagger**: `http://127.0.0.1:<puerto>/swagger` (por defecto puerto `5214`; configurable con `FMC_HTTP_PORT` en `.env`).
- **Consultar la BD en tu PC**: `sqlite3 docker-data/fmc.db` (o DBeaver / cualquier cliente SQLite apuntando a ese archivo).
- **Otros desarrolladores**: mismo flujo (`git clone`, `cp .env.example .env`, `make up`). Con **`docker-data/` vacío** (primera vez o tras borrar `fmc.db`), migraciones + seed dejan **los mismos usuarios demo** que arriba. No hace falta subir el `.db` al repo: el “snapshot” de datos está en el código del seed. Si alguien quiere conservar datos inventados entre sesiones, conserva su carpeta `docker-data/` local.

Imagen local etiquetada `fmc-api:local`; para publicarla en un registry: `docker tag fmc-api:local … && docker push …`.
