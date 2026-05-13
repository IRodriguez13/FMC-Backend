# FMC — Find my coffee (Makefile)
# Requiere: GNU Make, .NET SDK (dotnet en PATH)
# Opcional: Docker / Docker Compose para `make up`

-include .env
export

DOTNET := dotnet
SLN := Fmc.sln
API_PROJ := src/FMC.Api/Fmc.Api.csproj
TESTS_PROJ := tests/Fmc.Api.Tests/Fmc.Api.Tests.csproj

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

.PHONY: help restore build build-release test test-watch clean run dev swagger url ci watch up down logs smoke smoke-full docker-build

help:
	@echo "FMC (Find my coffee) — objetivos:"
	@echo "  make restore      Restaurar paquetes NuGet"
	@echo "  make build        Compilar solución (Debug)"
	@echo "  make build-release  Compilar solución (Release)"
	@echo "  make test         Ejecutar tests unitarios"
	@echo "  make test-watch   dotnet watch test (re-ejecuta al cambiar código)"
	@echo "  make clean        dotnet clean"
	@echo "  make run          API local (dotnet) — Swagger $(URL)/swagger"
	@echo "                      (si PORT no está definido: primer puerto libre $(PICK_START)-$(PICK_END))"
	@echo "  make run PORT=5281   Forzar puerto (si está ocupado, dotnet fallará)"
	@echo "  make up           Docker Compose: API + SQLite en ./docker-data/fmc.db"
	@echo "  make down         docker compose down"
	@echo "  make logs         docker compose logs -f api"
	@echo "  make smoke        Bash: alta consumidor + Enterprise + nearby ($(SMOKE_URL))"
	@echo "  make smoke-full   Bash: smoke completo 9 endpoints + errores + tokens en /tmp/tokenfmc"
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

run dev swagger:
	@echo "FMC Swagger UI: $(URL)/swagger"
	ASPNETCORE_ENVIRONMENT=$(ENV) $(DOTNET) run --project $(API_PROJ) --urls "$(URL)" --no-launch-profile

url:
	@echo "$(URL)/swagger"

ci: build-release
	$(DOTNET) test $(SLN) -c Release --no-build --verbosity minimal

watch:
	ASPNETCORE_ENVIRONMENT=$(ENV) $(DOTNET) watch run --project $(API_PROJ) --urls "$(URL)" --no-launch-profile

docker-build:
	docker compose build api

up:
	docker compose up -d --build

down:
	docker compose down

logs:
	docker compose logs -f api

smoke:
	chmod +x scripts/smoke-register.sh
	BASE_URL="$(SMOKE_URL)" ./scripts/smoke-register.sh

smoke-full:
	chmod +x scripts/smoke-full.sh
	BASE_URL="$(SMOKE_URL)" ./scripts/smoke-full.sh
