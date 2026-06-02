# FMC — Documentación de proyecto

> **Última verificación:** 2026-06-02  
> **Alcance:** backend (`fmcbackend`) + frontend integrado (`../FindMyCoffee-Frontend`)

Documentación de **contexto para desarrollo y agentes**. No sustituye el README operativo de la raíz; amplía reglas de negocio, arquitectura y contratos.

## Índice

| Archivo | Contenido |
|---------|-----------|
| [01-overview.md](./01-overview.md) | Qué es FMC, repos, stack, límites del MVP |
| [02-backend-architecture.md](./02-backend-architecture.md) | Capas .NET, persistencia, arranque, middleware |
| [03-api-rest.md](./03-api-rest.md) | Endpoints REST, auth JWT, payloads |
| [04-business-rules.md](./04-business-rules.md) | CABA, tiers, descubrimiento, exclusión Enterprise |
| [05-frontend-integration.md](./05-frontend-integration.md) | Acoplamiento API ↔ UI (resumen) |
| [06-dev-ops.md](./06-dev-ops.md) | Make, Docker, seed, smoke, troubleshooting |
| [changelog.md](./changelog.md) | Hitos documentados (actualizar por feat) |

**Frontend (repo hermano):** [`../FindMyCoffee-Frontend/Documentation/README.md`](../FindMyCoffee-Frontend/Documentation/README.md) — doc canónica de la SPA.

## Política de vigencia

1. **Fuente de verdad:** el código en Git. Si doc y código difieren, gana el código hasta que se actualice la doc con aprobación del mantenedor.
2. **Campo «Última verificación»:** fecha en la que un humano o agente contrastó el archivo contra rutas citadas.
3. **Señales de obsolescencia:** commits que tocan `Api/Endpoints/`, `Application/Services/`, contratos, `FindMyCoffee-Frontend/src/api/` o reglas en `Domain/` sin actualizar `Documentation/`.
4. **Antes de confiar en estas docs:** revisar `git log -1 --oneline -- <ruta citada>` o leer el archivo fuente.

## Creación / actualización

Solo con **aprobación explícita** del mantenedor por feat (regla global `ir0-project-documentation.mdc` en `~/.cursor/rules/`).
