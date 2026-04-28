# FMC Backend

Find my coffee (**FMC**): API en `src/FMC.Api/` (`Fmc.Api.csproj`), solución `Fmc.sln`, tests `tests/Fmc.Api.Tests/`. SQLite por defecto en `fmc.db` (ignorado en `.gitignore`). Ver `Makefile` para `build`, `test`, `run`, etc.

## Reglas de negocio (resumen)

- Solo aparecen cafeterías **registradas** por cuenta Enterprise con **`ListingActive = true`** (listado completado).
- **Enterprise Premium** recibe mayor ponderación en `/nearby` frente a Enterprise Standard; consumidor Free y Premium ven el **mismo orden ponderado** (la ponderación no depende del tier del consumidor).
- **`discountPercent`** en cada ítem: solo se informa si el consumidor es **Premium**; usuarios Free no ven descuentos en la API.
- Cambio de plan Enterprise simulado: `PATCH /api/enterprise/cafeteria/subscription-tier` (devuelve JWT nuevo).

## Seed demo (BD vacía tras migraciones)

Al arrancar la API se ejecuta **`MigrateAsync`** y, si no hay cuentas Enterprise, el **seed en código** (`Program.cs`). Eso **versiona los datos de prueba en Git** igual que el esquema: cada dev que borre `docker-data/` (o clone fresco) obtiene **los mismos usuarios, emails y GUIDs** sin commitear ningún `.db`.

Contraseña común: `SeedPass-123`

| Rol | Email |
|-----|--------|
| Enterprise Premium | `enterprise-premium@seed.fmc` |
| Enterprise Standard | `enterprise-standard@seed.fmc` |
| Consumidor Free | `consumidor@seed.fmc` |
| Consumidor Premium | `consumidor-premium@seed.fmc` |

## ¿Qué puedes probar en la API? (no es CRUD completo de entidades)

- **Auth / alta**: registro y login de consumidor y Enterprise (`/api/auth/...`).
- **Consumidor** (JWT consumidor): `GET /api/consumer/me`, `PATCH /api/consumer/tier` (cambio de plan simulado + JWT nuevo). No hay DELETE ni PUT de perfil genérico.
- **Enterprise** (JWT Enterprise): `GET` / `PUT` de la cafetería propia (`/api/enterprise/cafeteria/me`), `PATCH .../subscription-tier` (plan Enterprise simulado).
- **Descubrimiento**: `GET /api/cafeterias/nearby` (anon o con JWT consumidor para límites Premium y descuentos).

Valoraciones o CRUD administrativo de todas las tablas **no** están en este backend; la demo se prueba con **Swagger**, **`make smoke`** o scripts propios contra esos endpoints.

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

- **Swagger**: `http://127.0.0.1:<puerto>/swagger` (por defecto puerto `5214`; configurable con `FMC_HTTP_PORT` en `.env`).
- **Consultar la BD en tu PC**: `sqlite3 docker-data/fmc.db` (o DBeaver / cualquier cliente SQLite apuntando a ese archivo).
- **Otros desarrolladores**: mismo flujo (`git clone`, `cp .env.example .env`, `make up`). Con **`docker-data/` vacío** (primera vez o tras borrar `fmc.db`), migraciones + seed dejan **los mismos usuarios demo** que arriba. No hace falta subir el `.db` al repo: el “snapshot” de datos está en el código del seed. Si alguien quiere conservar datos inventados entre sesiones, conserva su carpeta `docker-data/` local.

Imagen local etiquetada `fmc-api:local`; para publicarla en un registry: `docker tag fmc-api:local … && docker push …`.
