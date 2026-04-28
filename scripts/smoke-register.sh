#!/usr/bin/env bash
# Registra un consumidor y una cuenta Enterprise con cafetería (prueba de humo contra la API FMC).
# Los registros exitosos se guardan en la SQLite que esté usando la API (misma BD que verías con sqlite3 o Swagger después).
# Requiere curl. Opcional: jq para JSON legible.
# Uso: BASE_URL=http://127.0.0.1:5214 ./scripts/smoke-register.sh
set -euo pipefail

BASE_URL="${BASE_URL:-http://127.0.0.1:5214}"
BASE_URL="${BASE_URL%/}"

pretty_json() {
  if command -v jq >/dev/null 2>&1; then
    jq .
  else
    cat
  fi
}

echo "Esperando API en ${BASE_URL} …"
for _ in $(seq 1 60); do
  if curl -sf "${BASE_URL}/swagger/v1/swagger.json" >/dev/null 2>&1 \
    || curl -sf "${BASE_URL}/swagger/index.html" >/dev/null 2>&1; then
    break
  fi
  sleep 1
done

if ! curl -sf "${BASE_URL}/swagger/v1/swagger.json" >/dev/null 2>&1 \
  && ! curl -sf "${BASE_URL}/swagger/index.html" >/dev/null 2>&1; then
  echo "ERROR: La API no responde en ${BASE_URL}. ¿Está levantada (make run / make up)?" >&2
  exit 1
fi

SUF="$(date +%s)$RANDOM"
CONSUMER_EMAIL="consumer-smoke-${SUF}@test.fmc"
ENT_EMAIL="enterprise-smoke-${SUF}@test.fmc"
PASS="SmokePass-123"

echo ""
echo "=== 1) Registro consumidor ==="
curl -sS -X POST "${BASE_URL}/api/auth/consumer/register" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"${CONSUMER_EMAIL}\",\"password\":\"${PASS}\"}" | pretty_json

echo ""
echo "=== 2) Registro Enterprise + cafetería ==="
curl -sS -X POST "${BASE_URL}/api/auth/enterprise/register" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"${ENT_EMAIL}\",\"password\":\"${PASS}\",\"cafeteriaName\":\"Cafetería smoke ${SUF}\",\"cafeteriaDescription\":\"Registro automático smoke-test\",\"cafeteriaAddress\":\"Calle Demo 1\",\"latitude\":40.4168,\"longitude\":-3.7038}" | pretty_json

echo ""
echo "=== 3) Login consumidor ==="
curl -sS -X POST "${BASE_URL}/api/auth/consumer/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"${CONSUMER_EMAIL}\",\"password\":\"${PASS}\"}" | pretty_json

echo ""
echo "=== 4) Login Enterprise ==="
curl -sS -X POST "${BASE_URL}/api/auth/enterprise/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"${ENT_EMAIL}\",\"password\":\"${PASS}\"}" | pretty_json

echo ""
echo "=== 5) Nearby (anon, Madrid centro) ==="
curl -sS "${BASE_URL}/api/cafeterias/nearby?lat=40.4168&lng=-3.7038" | pretty_json

echo ""
echo "=== Resumen — usuarios creados (misma contraseña para ambos) ==="
echo "  Consumidor   ${CONSUMER_EMAIL}"
echo "  Enterprise   ${ENT_EMAIL}"
echo "  Password     ${PASS}"

echo ""
echo "OK — Smoke terminado."
echo ""
echo "Persistencia: estos usuarios y la cafetería quedaron en la base SQLite del proceso API."
echo "  • Docker (make up): revisá ./docker-data/fmc.db o GET /api/cafeterias/nearby con lat/lng cercanos."
echo "  • dotnet local: el archivo suele ser fmc.db según ConnectionStrings (a menudo junto al proyecto API)."
echo "  • Seed demo (@seed.fmc): solo si la BD estaba vacía de Enterprise al primer arranque; el smoke añade filas aparte."
