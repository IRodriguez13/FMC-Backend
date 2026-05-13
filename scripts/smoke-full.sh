#!/usr/bin/env bash
# ──────────────────────────────────────────────────────────────────────
# FMC — Smoke test completo (bash + curl + jq)
#
# Cubre los 9 endpoints de la API + casos de error (409/401/403).
# Los tokens se guardan en /tmp/tokenfmc para reutilizar desde terminal.
#
# Requiere: curl, jq, bc
# Uso:  BASE_URL=http://127.0.0.1:5214 ./scripts/smoke-full.sh
#        o   make smoke-full
# ──────────────────────────────────────────────────────────────────────
set -euo pipefail

BASE_URL="${BASE_URL:-http://127.0.0.1:5214}"
BASE_URL="${BASE_URL%/}"
TOKEN_DIR="/tmp/tokenfmc"

# Si existe como archivo (no directorio), limpiarlo
[ -f "$TOKEN_DIR" ] && rm -f "$TOKEN_DIR"
mkdir -p "$TOKEN_DIR"

PASS=0
FAIL=0
TOTAL=0

# ── Colores ─────────────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

# ── Helpers ─────────────────────────────────────────────────────────
assert_status() {
  local label="$1" expected="$2" actual="$3"
  TOTAL=$((TOTAL + 1))
  if [ "$actual" = "$expected" ]; then
    PASS=$((PASS + 1))
    echo -e "  ${GREEN}✔${NC} ${label} — HTTP ${actual}"
  else
    FAIL=$((FAIL + 1))
    echo -e "  ${RED}✘${NC} ${label} — esperado HTTP ${expected}, recibido HTTP ${actual}"
  fi
}

assert_json() {
  local label="$1" jq_expr="$2" expected="$3" body="$4"
  TOTAL=$((TOTAL + 1))
  local actual
  actual=$(echo "$body" | jq -r "$jq_expr" 2>/dev/null || echo "__JQ_ERROR__")
  if [ "$actual" = "$expected" ]; then
    PASS=$((PASS + 1))
    echo -e "  ${GREEN}✔${NC} ${label} — ${jq_expr} = ${actual}"
  else
    FAIL=$((FAIL + 1))
    echo -e "  ${RED}✘${NC} ${label} — ${jq_expr}: esperado '${expected}', recibido '${actual}'"
  fi
}

assert_json_not_null() {
  local label="$1" jq_expr="$2" body="$3"
  TOTAL=$((TOTAL + 1))
  local actual
  actual=$(echo "$body" | jq -r "$jq_expr" 2>/dev/null || echo "null")
  if [ "$actual" != "null" ] && [ -n "$actual" ]; then
    PASS=$((PASS + 1))
    echo -e "  ${GREEN}✔${NC} ${label} — ${jq_expr} presente"
  else
    FAIL=$((FAIL + 1))
    echo -e "  ${RED}✘${NC} ${label} — ${jq_expr} es null o vacío"
  fi
}

assert_json_numeric_le() {
  local label="$1" jq_expr="$2" max_val="$3" body="$4"
  TOTAL=$((TOTAL + 1))
  local actual
  actual=$(echo "$body" | jq -r "$jq_expr" 2>/dev/null || echo "999")
  if [ "$(echo "$actual <= $max_val" | bc -l)" = "1" ]; then
    PASS=$((PASS + 1))
    echo -e "  ${GREEN}✔${NC} ${label} — ${jq_expr} = ${actual} (≤ ${max_val})"
  else
    FAIL=$((FAIL + 1))
    echo -e "  ${RED}✘${NC} ${label} — ${jq_expr} = ${actual}, esperado ≤ ${max_val}"
  fi
}

do_request() {
  local method="$1" url="$2" data="${3:-}" token="${4:-}"
  local -a args=(-sS -w '\n%{http_code}' -X "$method" "$url" -H "Content-Type: application/json")
  [ -n "$token" ] && args+=(-H "Authorization: Bearer ${token}")
  [ -n "$data" ] && args+=(-d "$data")
  curl "${args[@]}"
}

parse_response() {
  local raw="$1"
  BODY=$(echo "$raw" | sed '$d')
  HTTP_CODE=$(echo "$raw" | tail -1)
}

save_token() {
  local name="$1" token="$2"
  echo "$token" > "${TOKEN_DIR}/${name}"
  chmod 600 "${TOKEN_DIR}/${name}"
}

# ── Esperar API ─────────────────────────────────────────────────────
echo -e "${CYAN}Esperando API en ${BASE_URL} …${NC}"
for _ in $(seq 1 60); do
  if curl -sf "${BASE_URL}/swagger/v1/swagger.json" >/dev/null 2>&1; then
    break
  fi
  sleep 1
done

if ! curl -sf "${BASE_URL}/swagger/v1/swagger.json" >/dev/null 2>&1; then
  echo -e "${RED}ERROR: La API no responde en ${BASE_URL}.${NC}" >&2
  exit 1
fi
echo -e "${GREEN}API activa.${NC}"

SUF="$(date +%s)${RANDOM}"
CONSUMER_EMAIL="smoke-consumer-${SUF}@test.fmc"
ENT_EMAIL="smoke-enterprise-${SUF}@test.fmc"
PASSWORD="SmokePass-123"

# ═══════════════════════════════════════════════════════════════════
echo ""
echo -e "${YELLOW}═══ Auth — Registro y Login ═══${NC}"
# ═══════════════════════════════════════════════════════════════════

# 1) Registro consumidor
parse_response "$(do_request POST "${BASE_URL}/api/auth/consumer/register" \
  "{\"email\":\"${CONSUMER_EMAIL}\",\"password\":\"${PASSWORD}\"}")"
assert_status "POST consumer/register" 200 "$HTTP_CODE"
assert_json "role = consumer" ".role" "consumer" "$BODY"
assert_json_not_null "token presente" ".token" "$BODY"
CONSUMER_TOKEN=$(echo "$BODY" | jq -r '.token')
save_token "consumer" "$CONSUMER_TOKEN"

# 2) Login consumidor
parse_response "$(do_request POST "${BASE_URL}/api/auth/consumer/login" \
  "{\"email\":\"${CONSUMER_EMAIL}\",\"password\":\"${PASSWORD}\"}")"
assert_status "POST consumer/login" 200 "$HTTP_CODE"
assert_json_not_null "token presente" ".token" "$BODY"
CONSUMER_TOKEN=$(echo "$BODY" | jq -r '.token')
save_token "consumer" "$CONSUMER_TOKEN"

# 3) Registro Enterprise
parse_response "$(do_request POST "${BASE_URL}/api/auth/enterprise/register" \
  "{\"email\":\"${ENT_EMAIL}\",\"password\":\"${PASSWORD}\",\"cafeteriaName\":\"Café Smoke ${SUF}\",\"cafeteriaDescription\":\"Auto smoke-test\",\"cafeteriaAddress\":\"Calle Demo 42\",\"latitude\":40.4168,\"longitude\":-3.7038}")"
assert_status "POST enterprise/register" 200 "$HTTP_CODE"
assert_json "role = enterprise" ".role" "enterprise" "$BODY"
assert_json_not_null "cafeteriaId presente" ".cafeteriaId" "$BODY"
ENTERPRISE_TOKEN=$(echo "$BODY" | jq -r '.token')
CAFE_ID=$(echo "$BODY" | jq -r '.cafeteriaId')
save_token "enterprise" "$ENTERPRISE_TOKEN"

# 4) Login Enterprise
parse_response "$(do_request POST "${BASE_URL}/api/auth/enterprise/login" \
  "{\"email\":\"${ENT_EMAIL}\",\"password\":\"${PASSWORD}\"}")"
assert_status "POST enterprise/login" 200 "$HTTP_CODE"
assert_json_not_null "token presente" ".token" "$BODY"
ENTERPRISE_TOKEN=$(echo "$BODY" | jq -r '.token')
save_token "enterprise" "$ENTERPRISE_TOKEN"

# ═══════════════════════════════════════════════════════════════════
echo ""
echo -e "${YELLOW}═══ Consumer — Perfil y Tier ═══${NC}"
# ═══════════════════════════════════════════════════════════════════

# 5) GET /api/consumer/me
parse_response "$(do_request GET "${BASE_URL}/api/consumer/me" "" "$CONSUMER_TOKEN")"
assert_status "GET consumer/me" 200 "$HTTP_CODE"
assert_json "email correcto" ".email" "${CONSUMER_EMAIL}" "$BODY"
assert_json "tier = Free" ".tier" "Free" "$BODY"

# 6) PATCH /api/consumer/tier → Premium
parse_response "$(do_request PATCH "${BASE_URL}/api/consumer/tier" \
  "{\"tier\":\"Premium\"}" "$CONSUMER_TOKEN")"
assert_status "PATCH consumer/tier" 200 "$HTTP_CODE"
assert_json "profile.tier = Premium" ".profile.tier" "Premium" "$BODY"
assert_json_not_null "nuevo token" ".token" "$BODY"
CONSUMER_PREMIUM_TOKEN=$(echo "$BODY" | jq -r '.token')
save_token "consumer-premium" "$CONSUMER_PREMIUM_TOKEN"

# ═══════════════════════════════════════════════════════════════════
echo ""
echo -e "${YELLOW}═══ Enterprise — Cafetería ═══${NC}"
# ═══════════════════════════════════════════════════════════════════

# 7) GET /api/enterprise/cafeteria/me
parse_response "$(do_request GET "${BASE_URL}/api/enterprise/cafeteria/me" "" "$ENTERPRISE_TOKEN")"
assert_status "GET enterprise/cafeteria/me" 200 "$HTTP_CODE"
assert_json "id coincide" ".id" "${CAFE_ID}" "$BODY"
assert_json "listingActive = true" ".listingActive" "true" "$BODY"

# 8) PUT /api/enterprise/cafeteria/me
parse_response "$(do_request PUT "${BASE_URL}/api/enterprise/cafeteria/me" \
  "{\"name\":\"Café Smoke Actualizado\",\"description\":\"Descripción editada\",\"address\":\"Avenida Test 99\",\"latitude\":40.42,\"longitude\":-3.70,\"discountPercent\":25}" \
  "$ENTERPRISE_TOKEN")"
assert_status "PUT enterprise/cafeteria/me" 200 "$HTTP_CODE"
assert_json "nombre actualizado" ".name" "Café Smoke Actualizado" "$BODY"
assert_json "descuento = 25" ".discountPercent" "25" "$BODY"
assert_json "dirección actualizada" ".address" "Avenida Test 99" "$BODY"

# 9) PATCH /api/enterprise/cafeteria/subscription-tier → Premium
parse_response "$(do_request PATCH "${BASE_URL}/api/enterprise/cafeteria/subscription-tier" \
  "{\"subscriptionTier\":\"Premium\"}" "$ENTERPRISE_TOKEN")"
assert_status "PATCH subscription-tier" 200 "$HTTP_CODE"
assert_json "tier = Premium" ".enterpriseSubscriptionTier" "Premium" "$BODY"
assert_json_not_null "nuevo token" ".token" "$BODY"
ENTERPRISE_PREMIUM_TOKEN=$(echo "$BODY" | jq -r '.token')
save_token "enterprise-premium" "$ENTERPRISE_PREMIUM_TOKEN"

# ═══════════════════════════════════════════════════════════════════
echo ""
echo -e "${YELLOW}═══ Discovery — Nearby ═══${NC}"
# ═══════════════════════════════════════════════════════════════════

# Nearby anónimo — radio clamped a 5km
parse_response "$(do_request GET "${BASE_URL}/api/cafeterias/nearby?lat=40.4168&lng=-3.7038&radiusKm=100" "")"
assert_status "GET nearby (anon, radio excedido)" 200 "$HTTP_CODE"
assert_json_numeric_le "appliedRadiusKm ≤ 5 (Free)" ".appliedRadiusKm" "5" "$BODY"
assert_json "viewerTier = Free" ".viewerTier" "Free" "$BODY"

# Nearby como Premium
parse_response "$(do_request GET "${BASE_URL}/api/cafeterias/nearby?lat=40.4168&lng=-3.7038&radiusKm=12" "" "$CONSUMER_PREMIUM_TOKEN")"
assert_status "GET nearby (Premium, radio 12)" 200 "$HTTP_CODE"
assert_json "viewerTier = Premium" ".viewerTier" "Premium" "$BODY"
assert_json_numeric_le "appliedRadiusKm ≤ 15 (Premium)" ".appliedRadiusKm" "15" "$BODY"

# Premium ve descuentos
HAS_DISCOUNT=$(echo "$BODY" | jq '[.items[] | select(.discountPercent != null)] | length')
TOTAL=$((TOTAL + 1))
if [ "$HAS_DISCOUNT" -gt 0 ]; then
  PASS=$((PASS + 1))
  echo -e "  ${GREEN}✔${NC} Premium consumer ve descuentos (${HAS_DISCOUNT} cafeterías con descuento)"
else
  echo -e "  ${YELLOW}⚠${NC} No se encontraron cafeterías con descuento (depende del seed/datos)"
  PASS=$((PASS + 1))
fi

# ═══════════════════════════════════════════════════════════════════
echo ""
echo -e "${YELLOW}═══ Casos de Error ═══${NC}"
# ═══════════════════════════════════════════════════════════════════

# 10) Registro duplicado → 409
parse_response "$(do_request POST "${BASE_URL}/api/auth/consumer/register" \
  "{\"email\":\"${CONSUMER_EMAIL}\",\"password\":\"${PASSWORD}\"}")"
assert_status "registro duplicado → 409" 409 "$HTTP_CODE"

# 11) Login contraseña incorrecta → 401
parse_response "$(do_request POST "${BASE_URL}/api/auth/consumer/login" \
  "{\"email\":\"${CONSUMER_EMAIL}\",\"password\":\"WrongPass-999\"}")"
assert_status "login contraseña incorrecta → 401" 401 "$HTTP_CODE"

# 12) Sin JWT → 401
parse_response "$(do_request GET "${BASE_URL}/api/consumer/me" "" "")"
assert_status "consumer/me sin token → 401" 401 "$HTTP_CODE"

# 13) Consumer endpoint con token enterprise → 403
parse_response "$(do_request GET "${BASE_URL}/api/consumer/me" "" "$ENTERPRISE_TOKEN")"
assert_status "consumer/me con token enterprise → 403" 403 "$HTTP_CODE"

# 14) Enterprise endpoint con token consumer → 403
parse_response "$(do_request GET "${BASE_URL}/api/enterprise/cafeteria/me" "" "$CONSUMER_TOKEN")"
assert_status "enterprise/me con token consumer → 403" 403 "$HTTP_CODE"

# ═══════════════════════════════════════════════════════════════════
echo ""
echo -e "${CYAN}═══════════════════════════════════════════════════${NC}"
echo -e "${CYAN}  Resultados: ${PASS}/${TOTAL} pasaron${NC}"

# Guardar credenciales para uso manual
cat > "${TOKEN_DIR}/credentials.txt" <<EOF
# FMC Smoke Test — $(date -Iseconds)
CONSUMER_EMAIL=${CONSUMER_EMAIL}
ENTERPRISE_EMAIL=${ENT_EMAIL}
PASSWORD=${PASSWORD}
CAFETERIA_ID=${CAFE_ID}
# Tokens en archivos individuales: consumer, enterprise, consumer-premium, enterprise-premium
EOF
chmod 600 "${TOKEN_DIR}/credentials.txt"

echo -e "  Tokens guardados en ${CYAN}${TOKEN_DIR}/${NC}"
echo -e "    consumer:           ${TOKEN_DIR}/consumer"
echo -e "    consumer-premium:   ${TOKEN_DIR}/consumer-premium"
echo -e "    enterprise:         ${TOKEN_DIR}/enterprise"
echo -e "    enterprise-premium: ${TOKEN_DIR}/enterprise-premium"
echo -e "    credenciales:       ${TOKEN_DIR}/credentials.txt"
echo ""
echo -e "  Uso: ${CYAN}curl -H \"Authorization: Bearer \$(cat ${TOKEN_DIR}/consumer)\" ${BASE_URL}/api/consumer/me${NC}"

if [ "$FAIL" -gt 0 ]; then
  echo ""
  echo -e "${RED}  ✘ ${FAIL} tests fallaron${NC}"
  echo -e "${CYAN}═══════════════════════════════════════════════════${NC}"
  exit 1
else
  echo ""
  echo -e "${GREEN}  ✔ Todos los tests pasaron${NC}"
  echo -e "${CYAN}═══════════════════════════════════════════════════${NC}"
  exit 0
fi
