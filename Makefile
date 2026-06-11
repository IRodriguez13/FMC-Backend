# FMC — Find my coffee (Makefile)
# Requiere: GNU Make, .NET SDK (dotnet en PATH)
# Opcional: Docker / Docker Compose para `make up`

-include .env
export

DOTNET := dotnet
SLN := Fmc.sln
API_PROJ := Api/Api.csproj
TESTS_PROJ := Api.Tests/Api.Tests.csproj

# Primer puerto libre en 127.0.0.1 si PORT no viene por línea de comando ni por variable de entorno ya definida en Make.
PICK_START ?= 5214
PICK_END ?= 5230
ifndef PORT
PORT := $(shell bash scripts/pick-free-port.sh $(PICK_START) $(PICK_END))
endif
FMC_HTTP_PORT ?= $(PORT)
URL ?= http://127.0.0.1:$(PORT)
# Para smoke / enlaces cuando el puerto publicado es el de Compose (.env → FMC_HTTP_PORT)
SMOKE_URL ?= http://127.0.0.1:$(FMC_HTTP_PORT)

ENV ?= Development

.DEFAULT_GOAL := help

.PHONY: help restore build build-release test test-watch clean migrate migrations-list run dev swagger url ci watch up down logs smoke smoke-full docker-build reset-db fix-docker-data-perms backup-db

help:
	@echo "FMC (Find my coffee) — objetivos:"
	@echo "  make restore      Restaurar paquetes NuGet"
	@echo "  make build        Compilar solución (Debug)"
	@echo "  make build-release  Compilar solución (Release)"
	@echo "  make test         Ejecutar tests unitarios"
	@echo "  make test-watch   dotnet watch test (re-ejecuta al cambiar código)"
	@echo "  make clean        dotnet clean"
	@echo "  make migrate      Aplicar migraciones EF (Api/fmc.db) antes de levantar la API"
	@echo "  make migrations-list  Migraciones pendientes / aplicadas"
	@echo "  make run          migrate + API local (dotnet) — Swagger $(URL)/swagger"
	@echo "                      (si PORT no está definido: primer puerto libre $(PICK_START)-$(PICK_END))"
	@echo "  make run PORT=5281   Forzar puerto (si está ocupado, dotnet fallará)"
	@echo "  make up           Docker Compose: API + SQLite en ./docker-data/fmc.db"
	@echo "  make down         docker compose down"
	@echo "  make reset-db     down + borrar docker-data/fmc.db (sin sudo)"
	@echo "  make fix-docker-data-perms  Si rm falla: chown docker-data al usuario actual"
	@echo "  make logs         docker compose logs -f api"
	@echo "  make smoke        Bash: alta consumidor + Enterprise + nearby ($(SMOKE_URL))"
	@echo "  make smoke-full   Bash: smoke completo 9 endpoints + errores + tokens en /tmp/tokenfmc"
	@echo "  make backup-db    Copia fmc.db a docker-data/backups/"
	@echo "  make docker-build Imagen fmc-api:local (sin levantar)"
	@echo "  make dev / swagger  Alias de run local — $(URL)/swagger"
	@echo "  make url          URL Swagger (local)"
	@echo "  make ci           build + test"
	@echo "  make watch        dotnet watch run (local)"

restore:
	$(DOTNET) restore $(SLN)

build: restore
	$(DOTNET) build $(SLN) -c Debug --no-restore

build-release: restore
	$(DOTNET) build $(SLN) -c Release --no-restore

test: build
	$(DOTNET) test $(SLN) -c Debug --no-build --verbosity normal

test-watch:
	$(DOTNET) watch test --project $(TESTS_PROJ)

clean:
	$(DOTNET) clean $(SLN)

migrate: build
	@echo "Aplicando migraciones EF (SQLite relativa al directorio Api/)…"
	$(DOTNET) ef database update --project Infrastructure/Infrastructure.csproj --startup-project $(API_PROJ)

migrations-list: build
	$(DOTNET) ef migrations list --project Infrastructure/Infrastructure.csproj --startup-project $(API_PROJ)

run dev swagger: migrate
	@echo "FMC API: $(URL)"
	@echo "  Swagger UI: $(URL)/swagger  (la raíz / redirige aquí en Development)"
	@echo "  GraphQL:    $(URL)/graphql"
	@echo "  Nearby:     $(URL)/api/cafeterias/nearby?lat=-34.6037&lng=-58.3816"
	@echo ""
	@echo "Si el navegador muestra 404, probá $(URL)/swagger (no solo $(URL)/)."
	FMC_DISABLE_HTTPS_REDIRECT=1 ASPNETCORE_ENVIRONMENT=$(ENV) $(DOTNET) run --project $(API_PROJ) --urls "$(URL)" --no-launch-profile

url:
	@echo "$(URL)/swagger"

ci: build-release
	$(DOTNET) test $(SLN) -c Release --no-build --verbosity minimal

watch: migrate
	FMC_DISABLE_HTTPS_REDIRECT=1 ASPNETCORE_ENVIRONMENT=$(ENV) $(DOTNET) watch run --project $(API_PROJ) --urls "$(URL)" --no-launch-profile

docker-build:
	docker compose build api

up:
	docker compose up -d --build

down:
	docker compose down

# Borra la SQLite montada en ./docker-data (evita 'Permiso denegado' tras sudo make).
reset-db: down
	@docker run --rm -v "$(CURDIR)/docker-data:/data" alpine:3.20 sh -c 'rm -f /data/fmc.db /data/fmc.db-shm /data/fmc.db-wal' 2>/dev/null || \
		rm -f docker-data/fmc.db docker-data/fmc.db-shm docker-data/fmc.db-wal 2>/dev/null || true
	@echo "BD eliminada. Siguiente: make up (regenera seed CABA)."

fix-docker-data-perms:
	@echo "Si hace falta contraseña de sudo, es por archivos creados como root."
	sudo chown -R "$$(id -u):$$(id -g)" docker-data

logs:
	docker compose logs -f api

smoke:
	chmod +x scripts/smoke-register.sh
	BASE_URL="$(SMOKE_URL)" ./scripts/smoke-register.sh

smoke-full:
	chmod +x scripts/smoke-full.sh
	BASE_URL="$(SMOKE_URL)" ./scripts/smoke-full.sh

backup-db:
	chmod +x scripts/backup-db.sh
	./scripts/backup-db.sh
