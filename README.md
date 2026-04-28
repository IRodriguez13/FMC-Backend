# FMC Backend

Find my coffee (**FMC**): API en `src/FMC.Api/` (`Fmc.Api.csproj`), solución `Fmc.sln`, tests `tests/Fmc.Api.Tests/`. SQLite por defecto en `fmc.db` (ignorado en `.gitignore`). Ver `Makefile` para `build`, `test`, `run`, etc.

## Reglas de negocio (resumen)

- Solo aparecen cafeterías **registradas** por cuenta Enterprise con **`ListingActive = true`** (listado completado).
- **Enterprise Premium** recibe mayor ponderación en `/nearby` frente a Enterprise Standard; consumidor Free y Premium ven el **mismo orden ponderado** (la ponderación no depende del tier del consumidor).
- **`discountPercent`** en cada ítem: solo se informa si el consumidor es **Premium**; usuarios Free no ven descuentos en la API.
- Cambio de plan Enterprise simulado: `PATCH /api/enterprise/cafeteria/subscription-tier` (devuelve JWT nuevo).

## Seed demo (BD vacía tras migraciones)

Contraseña común: `SeedPass-123`

| Rol | Email |
|-----|--------|
| Enterprise Premium | `enterprise-premium@seed.fmc` |
| Enterprise Standard | `enterprise-standard@seed.fmc` |
| Consumidor Free | `consumidor@seed.fmc` |
