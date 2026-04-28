#!/usr/bin/env bash
# Imprime el primer puerto TCP libre en 127.0.0.1 dentro del rango [inicio, fin] (por defecto 5214–5230).
# Usado por `make run` / `make watch` cuando PORT no está definido.
set -euo pipefail
START="${1:-5214}"
END="${2:-5230}"

if command -v python3 >/dev/null 2>&1; then
  python3 -c "
import socket, sys
start, end = int(sys.argv[1]), int(sys.argv[2])
for p in range(start, end + 1):
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    try:
        s.bind(('127.0.0.1', p))
        print(p)
        sys.exit(0)
    except OSError:
        pass
    finally:
        s.close()
sys.stderr.write('pick-free-port: sin puerto libre en %s-%s\\n' % (start, end))
sys.exit(1)
" "$START" "$END"
  exit 0
fi

for p in $(seq "$START" "$END"); do
  if bash -c "echo >/dev/tcp/127.0.0.1/$p" 2>/dev/null; then
    continue
  fi
  echo "$p"
  exit 0
done

echo "pick-free-port: sin puerto libre en ${START}-${END}" >&2
exit 1
