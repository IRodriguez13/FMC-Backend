#!/usr/bin/env bash
# Snapshot SQLite para demo (docker-data o Api/fmc.db local).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
STAMP="$(date +%Y%m%d-%H%M%S)"
OUT_DIR="${ROOT}/docker-data/backups"
mkdir -p "$OUT_DIR"

if [[ -f "${ROOT}/docker-data/fmc.db" ]]; then
  SRC="${ROOT}/docker-data/fmc.db"
elif [[ -f "${ROOT}/Api/fmc.db" ]]; then
  SRC="${ROOT}/Api/fmc.db"
elif [[ -f "${ROOT}/fmc.db" ]]; then
  SRC="${ROOT}/fmc.db"
else
  echo "No se encontró fmc.db (docker-data/ ni Api/)." >&2
  exit 1
fi

DEST="${OUT_DIR}/fmc-${STAMP}.db"
cp "$SRC" "$DEST"
echo "Backup: $DEST"
