# Changelog histórico — FMC Backend

> **Fuente de verdad del código:** Git. Registro legible de feats y cambios de producto.  
> **Última verificación:** 2026-06-11  
> **Reglas del proyecto:** `.cursor/rules/fmc-changelog.mdc`, `.cursor/rules/fmc-unit-tests.mdc`

Entradas **más recientes arriba**.

---

## 2026-06-11 — Demo MVP funcional (cierre de oleada)

**Commits:** `eccdf28`, `4615755`, `5d8947f`, `96656ca`, `6cafd6c`  
**Pedido:** Demo usable end-to-end — fotos, perfil, descuentos Premium, migraciones, deploy.  
**Alcance:** backend, docs, ops

### Estado funcional (demo)

| Área | Comportamiento |
|------|----------------|
| Auth | Register/login consumer y enterprise; seed CABA |
| `/nearby` | Cover, rating, descuentos solo consumer Premium |
| Medios | Seed `.jpg`, redirect PNG legacy, `/media/` |
| Perfil | `GET/PUT /me`, `POST /me/avatar`, `PATCH /tier` |
| Fotos/reseñas | CRUD author-owned por cafetería |
| Ops | `make migrate`, `make run`, Docker, health, rate limit |

### Cambios clave

- Migración `ConsumerProfileFields` (`DisplayName`, `AvatarStorageKey`).
- `SeedImageFiles`: assets JPEG, cleanup PNG corruptos, redirect en pipeline.
- `make migrate` / `migrations-list`; `make run` aplica migrate antes del API.
- Discovery: `coverImageUrl`, ratings batch en `/nearby`.
- Demo: CORS env, rate limit auth/upload, `appsettings.Production`, Swagger guard.
- Reseñas CRUD author-owned; fix hash en seed.
- Logging EF design-time: `HostAbortedException` ya no loguea como Fatal.

### API nueva / ampliada

| Método | Ruta | Notas |
|--------|------|-------|
| PUT | `/api/consumer/me` | `{ displayName }` |
| POST | `/api/consumer/me/avatar` | multipart `file` |
| GET | `/api/cafeterias/{id}/photos` | listado |
| POST | `/api/cafeterias/{id}/photos` | upload |
| GET/POST/PATCH/DELETE | `/api/cafeterias/{id}/reviews` | CRUD author-owned |

### Validación

- `dotnet test`: **59/59 OK**
- `make migrate`: BD al día (mensaje `HostAbortedException` en consola = ruido EF, no error)
- Smoke manual: nearby con cover, media `/media/seed-*.jpg` 200

---

## 2026-06-09 — Redirección raíz `/` → Swagger en Development

**Commit:** `96656ca` (incluido en oleada perfil-demo)  
**Pedido:** Raíz `/` devolvía 404 en dev.  
**Alcance:** backend, ops

### Cambios

- `GET /` → redirect `/swagger/index.html` en Development.
- `make run` imprime URLs útiles (Swagger, GraphQL, nearby).

### Validación

- `curl /` → 302 Swagger; `dotnet test` OK

---

## 2026-06-09 — Fotos locales y reseñas por cafetería

**Commit:** `6cafd6c` — `feat(media): fotos locales, reseñas y reglas de proyecto`  
**Pedido:** Consumer y enterprise suben fotos y reseñas en cualquier plan.  
**Alcance:** backend

### Cambios

- Entidades `CafeteriaPhoto`, `CafeteriaReview`.
- `uploads/` servido en `/media/`; límite 5 MB; JPEG/PNG/WebP.
- Tests: `CafeteriaPhotoServiceTests`, `CafeteriaReviewServiceTests`.

### Validación

- `dotnet test`: OK

---

## 2026-06-02 — CABA, discovery Enterprise, docs y endurecimiento operativo

**Commit:** `1ba9f22` — `feat(fmc): CABA, discovery Enterprise, docs y endurecimiento operativo`  
**Alcance:** backend, docs, ops

### Cambios

- Área CABA, seed idempotente, Enterprise excluido de su propio nearby.
- Ponderación Enterprise Premium; descuentos solo consumer Premium.
- Middleware errores ProblemDetails; SQLite WAL.
- Makefile `reset-db`, `fix-docker-data-perms`.
- Documentación inicial `Documentation/`.

### Validación

- `dotnet test`: 28/28 OK

---

## 2026-05-16 — GraphQL y simplificación de estructura

**Commit:** `0d8991d`  
**Alcance:** backend

### Cambios

- Hot Chocolate `/graphql`; resolvers nearby, perfil, cafetería.
- Layout plano Api / Application / Domain / Infrastructure.

---

## 2026-05-12 — Serilog y smoke test completo

**Commit:** `e37c977`  
**Alcance:** backend, ops

### Cambios

- Serilog oficial; `make smoke-full` 38 assertions.

---

## Referencia rápida — cuentas seed

Contraseña: **`SeedPass-123`**

| Rol | Email |
|-----|--------|
| Consumidor Free | `consumidor@seed.fmc` |
| Consumidor Premium | `consumidor-premium@seed.fmc` |
| Enterprise Premium (Palermo) | `enterprise-premium@seed.fmc` |
| Enterprise Standard (San Telmo) | `enterprise-standard@seed.fmc` |

Ver `Documentation/06-dev-ops.md` para arranque local y troubleshooting.
