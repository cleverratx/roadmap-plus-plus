#!/usr/bin/env bash

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

run_script() {
  local name="$1"
  shift
  bash "$ROOT/deploy/scripts/$name.sh" "$@"
}

compose() {
  docker compose --project-directory "$ROOT" -f "$ROOT/compose.yaml" -f "$ROOT/deploy/compose.prod.yaml" "$@"
}

log() {
  printf '[%s] %s\n' "$(date '+%Y-%m-%d %H:%M:%S')" "$*"
}

fail() {
  log "ОШИБКА: $*" >&2
  exit 1
}

setting() {
  local name="$1" fallback="$2" from_file=""
  if [ -f "$ROOT/.env" ]; then
    from_file="$(grep -E "^${name}=" "$ROOT/.env" | tail -n 1 | cut -d= -f2-)" || true
  fi
  printf '%s' "${!name:-${from_file:-$fallback}}"
}

backup_dir() {
  local dir
  dir="$(setting BACKUP_DIR "$ROOT/backups")"
  mkdir -p "$dir"
  printf '%s' "$dir"
}

db_running() {
  [ -n "$(compose ps --status running --quiet db 2>/dev/null)" ]
}

wait_for_api() {
  for _ in $(seq 1 60); do
    if [ "$(compose exec -T nginx wget -q -O - http://127.0.0.1/api/health 2>/dev/null)" = "Healthy" ]; then
      return 0
    fi
    sleep 2
  done
  fail "API не ответил Healthy за две минуты, смотрите логи: docker compose -f compose.yaml -f deploy/compose.prod.yaml logs api"
}
