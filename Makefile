# FMC — Find my coffee (Makefile)
# Requiere: GNU Make, .NET SDK (dotnet en PATH)

DOTNET := dotnet
SLN := Fmc.sln
API_PROJ := src/FMC.Api/Fmc.Api.csproj
TESTS_PROJ := tests/Fmc.Api.Tests/Fmc.Api.Tests.csproj
URL ?= http://127.0.0.1:5214
ENV ?= Development

.DEFAULT_GOAL := help

.PHONY: help restore build build-release test test-watch clean run dev swagger url ci watch

help:
	@echo "FMC (Find my coffee) — objetivos:"
	@echo "  make restore      Restaurar paquetes NuGet"
	@echo "  make build        Compilar solución (Debug)"
	@echo "  make build-release  Compilar solución (Release)"
	@echo "  make test         Ejecutar tests unitarios"
	@echo "  make test-watch   dotnet watch test (re-ejecuta al cambiar código)"
	@echo "  make clean        dotnet clean"
	@echo "  make run          Levantar API con Swagger (ASPNETCORE_ENVIRONMENT=$(ENV))"
	@echo "  make dev / swagger  Alias de run — Swagger UI en $(URL)/swagger"
	@echo "  make url          Mostrar URL de Swagger"
	@echo "  make ci           build + test (pipeline local)"
	@echo "  make watch        dotnet watch run (API con recarga)"

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
