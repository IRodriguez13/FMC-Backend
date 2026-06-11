# Changelog histórico — FMC Backend

> **Fuente de verdad del código:** Git. Este archivo es el registro legible de feats y cambios de producto.  
> **Última actualización:** 2026-06-11  
> **Reglas del proyecto:** `.cursor/rules/fmc-changelog.mdc`, `.cursor/rules/fmc-unit-tests.mdc`

Entradas **más recientes arriba**. Entradas retroactivas reconstruidas desde `git log`.

---

## 2026-06-11 — Perfil consumidor, fotos seed, migraciones y demo deploy

**Commit:** *(este commit)*  
**Pedido:** Perfil editable, fotos Palermo, descuentos Premium, `make migrate`, tests y documentación.  
**Alcance:** backend, frontend (`fmcfront`), docs, tests

### Backend
- `ConsumerUser`: `DisplayName`, `AvatarStorageKey`; migración `ConsumerProfileFields`.
- API: `PUT /api/consumer/me`, `POST /api/consumer/me/avatar`.
- Seed fotos: assets `.jpg`, cleanup PNG legacy, redirect `/media/seed-*.png` → `.jpg`.
- `make migrate` / `migrations-list`; `make run` ejecuta migrate antes del API.
- `/nearby`: `coverImageUrl`, ratings batch.
- Tests: `ConsumerProfileServiceTests` (10).

### Frontend
- Perfil: editar nombre, subir avatar; email solo lectura.
- Dark mode, descuentos solo Premium, fotos (`CafeCoverImage`, `resolveMediaUrl`).
- Vitest: `mediaUrl.test.js`, `cafeteriaMapper.test.js` (11 tests).

### Validación
- `dotnet test`: 55/55 OK
- `npm test` (fmcfront): 11/11 OK
- `npm run build` (fmcfront): OK

---

## 2026-06-09 — Redirección raíz `/` → Swagger en Development

**Commit:** *(pendiente de commit)*  
**Pedido:** `make run` reporta el puerto pero abrir la URL raíz devuelve 404.  
**Alcance:** backend, ops

### Cambios
- `GET /` redirige a `/swagger/index.html` cuando `ASPNETCORE_ENVIRONMENT=Development`.
- `make run` muestra URLs útiles (Swagger, GraphQL, nearby) y aviso si se abre solo la raíz.

### Validación
- `curl http://127.0.0.1:<puerto>/` → 302 a Swagger (Development)
- `curl .../swagger/index.html` → 200
- `dotnet test`: 45/45 OK

---

## 2026-06-09 — Política de tests obligatorios por feat

**Commit:** *(pendiente de commit)*  
**Pedido:** Todas las nuevas feats deben tener sus respectivos tests.  
**Alcance:** docs, ops (regla Cursor)

### Cambios
- Regla `.cursor/rules/fmc-unit-tests.mdc` (`alwaysApply: true`).
- Alcance mínimo, convenciones xUnit/Moq, validación en changelog.
- Tests retroactivos para feat fotos/reseñas (ver entrada siguiente).

### Validación
- `dotnet test`: 45/45 OK

---

## 2026-06-09 — Fotos locales y reseñas por cafetería

**Commit:** *(pendiente de commit)*  
**Pedido:** Usuarios consumer y enterprise, en cualquier plan, pueden subir fotos del local y reseñas.  
**Alcance:** backend

### Cambios
- Entidades `CafeteriaPhoto` y `CafeteriaReview` con autor (`AuthorUserId`, `AuthorRole`).
- Almacenamiento local de imágenes (`uploads/`, servidas en `/media/`).
- Servicios `CafeteriaPhotoService` y `CafeteriaReviewService` sin restricción por tier/plan.
- Migración EF `CafeteriaPhotosAndReviews`.
- Config `Media` en `appsettings.json` (tamaño máx. 5 MB, JPEG/PNG/WebP).
- Tests: `CafeteriaPhotoServiceTests` (8), `CafeteriaReviewServiceTests` (8).

### API / contrato
| Método | Ruta | Auth |
|--------|------|------|
| `GET` | `/api/cafeterias/{id}/photos` | No |
| `POST` | `/api/cafeterias/{id}/photos` | JWT consumer o enterprise |
| `GET` | `/api/cafeterias/{id}/reviews` | No |
| `POST` | `/api/cafeterias/{id}/reviews` | JWT consumer o enterprise |

- Reseña: rating 1–5, texto opcional; una por autor/rol por cafetería (POST actualiza).
- Foto: `multipart/form-data`, campo `file`.

### Validación
- `dotnet build`: OK
- `dotnet test`: 45/45 OK (`CafeteriaPhotoServiceTests`, `CafeteriaReviewServiceTests`)

---

## 2026-06-02 — CABA, discovery Enterprise, docs y endurecimiento operativo

**Commit:** `1ba9f22` — `feat(fmc): CABA, discovery Enterprise, docs y endurecimiento operativo`  
**Alcance:** backend, docs, ops

### Cambios
- Área de servicio CABA (`CabaServiceArea`) + validación en altas y `/nearby`.
- Seed idempotente CABA con GUIDs fijos (`DataSeeder.EnsureCabaDemoAsync`).
- Enterprise excluido de `/nearby` para su propia cafetería (`ExcludeCafeteriaId`).
- Ponderación ranking Enterprise Premium; descuentos solo visibles a consumer Premium.
- Middleware de errores: 4xx warning; cancelaciones cliente 499/504.
- SQLite WAL, `Cache=Shared`, `busy_timeout`.
- Serilog simplificado; sin HTTPS redirect en Development.
- Makefile: `reset-db`, `fix-docker-data-perms`.
- Documentación inicial en `Documentation/` (overview, arquitectura, API, reglas, dev-ops).

### Validación
- `dotnet test`: 28/28 OK

---

## 2026-05-16 — GraphQL y simplificación de estructura

**Commit:** `0d8991d` — `feat(api): integrate GraphQL query layer and simplify codebase directory structure`  
**Alcance:** backend

### Cambios
- Hot Chocolate v13: endpoint `/graphql`, playground en Development.
- Resolvers: `GetNearbyCafeterias`, `GetConsumerProfile`, `GetMyCafeteria`.
- Layout plano: sin `src/` ni `tests/`; proyectos `Api/`, `Application/`, `Domain/`, `Infrastructure/`.
- Contratos aplanados en `Application/Contracts/`.
- Tests unitarios `FmcQueryTests`.

### Validación
- Smoke 38 assertions + 18 unit tests OK

---

## 2026-05-12 — Serilog y smoke test completo

**Commit:** `e37c977` — `feat: agregar Serilog como logger oficial y smoke test completo de 38 assertions`  
**Alcance:** backend, ops

### Cambios
- Serilog.AspNetCore como logger oficial (bootstrap, request logging, config en `appsettings.json`).
- Smoke test `make smoke-full`: 38 assertions (auth, perfil, tier, cafetería, nearby, errores).
- Tokens en `/tmp/tokenfmc/` para reutilización manual.
- Dockerfile actualizado para arquitectura en capas.

### Validación
- Smoke 38/38 OK

---

## 2026-05-02 — Clean Architecture en 4 capas

**Commit:** `f74ff99` — `refactor: separar proyecto monolítico en Clean Architecture (Domain, Application, Infrastructure, Api)`  
**Alcance:** backend

### Cambios
- Proyectos: `Domain`, `Application`, `Infrastructure`, `Api` con dependencias estrictas.
- Entidades y enums en Domain; servicios e interfaces en Application; EF/JWT/bcrypt en Infrastructure.
- `Program.cs` simplificado; DI vía `AddApplicationServices()` / `AddInfrastructure()`.
- `EnterpriseAuthService` usa repositorios + `IUnitOfWork` (sin DbContext directo).
- `LocationValidation.IsValidLocation()` + test.

### Validación
- `dotnet test`: 15/15 OK

---

## 2026-04-28 — Demo usable con Swagger y Docker

**Commit:** `87183d0` — `Primer commit de demo usable con swagger y db en docker local`  
**Alcance:** backend, ops

### Cambios
- API ejecutable con Swagger UI.
- Base SQLite en Docker local.

---

## 2026-04-28 — Inicialización del proyecto FMC API

**Commit:** `6de36d5` — `Initialize FMC API project with core structure…`  
**Alcance:** backend

### Cambios
- Solution, proyectos, `.gitignore`, Makefile.
- DbContext y migraciones iniciales (`ConsumerUser`, `EnterpriseUser`, `Cafeteria`).
- Endpoints base: auth consumer/enterprise y descubrimiento `/nearby`.

---

## Plantilla (próximas entradas)

```markdown
## YYYY-MM-DD — <título breve>

**Commit:** `<hash>` — `<asunto>` *(omitir si sin commit)*  
**Pedido:** <requerimiento del mantenedor>  
**Alcance:** backend | frontend | docs | ops

### Cambios
- …

### API / contrato *(si aplica)*
- …

### Validación
- …
```
