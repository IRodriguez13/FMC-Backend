# Changelog de contexto (Documentation)

> Registro de hitos **desde la perspectiva de documentación**. No reemplaza `git log`.

## 2026-06-02 — Documentación inicial + estado del producto

**Aprobado por:** mantenedor (mensaje explícito solicitud docs + rule).

### Backend

- API .NET 8 REST + GraphQL; SQLite WAL; seed CABA idempotente.
- Reglas CABA, discovery con boost Enterprise Premium, descuentos solo consumer Premium.
- Enterprise excluido de `/nearby` para su propia cafetería.
- Middleware: cancelaciones cliente no como 500; 4xx como warning.
- Tests: 28 passing.

### Frontend (FindMyCoffee-Frontend)

- Documentación propia en `FindMyCoffee-Frontend/Documentation/` (README raíz actualizado 2026-06-02).
- Integración REST, mapa Leaflet, Navbar «Hola, {nombre}».
- Ver también resumen en `fmcbackend/Documentation/05-frontend-integration.md`.

### Reglas Cursor

- `~/.cursor/rules/ir0-project-documentation.mdc` — docs solo con OK previo; verificar vigencia.

---

## Plantilla para próximas entradas

```markdown
## YYYY-MM-DD — <título feat>

**Aprobado por:** <usuario / mensaje>

### Cambios
- ...

### Docs actualizadas
- [ ] 03-api-rest.md
- [ ] ...
```
