#!/usr/bin/env bash
set -euo pipefail
. "$(dirname "${BASH_SOURCE[0]}")/common.sh"

usage() {
  cat <<'TEXT'
Проверяет, что дамп правда восстанавливается: разворачивает его во временную базу рядом с рабочей,
убеждается, что на месте таблицы и история миграций, и удаляет временную базу. Рабочую базу не трогает.

  bash deploy/scripts/verify-backup.sh <файл дампа>
TEXT
}

case "${1:-}" in
  -h | --help)
    usage
    exit 0
    ;;
  "")
    usage
    exit 1
    ;;
esac

dump="$1"
[ -s "$dump" ] || fail "файл дампа $dump пустой или его нет"
db_running || fail "контейнер базы не запущен, проверять негде"

scratch="verify_$(date -u '+%Y%m%d%H%M%S')_$$"

in_scratch() {
  compose exec -T db sh -c "$1" sh "$scratch" "${2:-}"
}

query() {
  in_scratch 'psql --username="$POSTGRES_USER" --dbname="$1" --no-psqlrc --tuples-only --no-align -v ON_ERROR_STOP=1 -c "$2"' "$1"
}

cleanup() {
  in_scratch 'dropdb --username="$POSTGRES_USER" --if-exists --force "$1"' >/dev/null 2>&1 || true
}
trap cleanup EXIT

log "Проверяю дамп: разворачиваю его во временную базу $scratch"
in_scratch 'createdb --username="$POSTGRES_USER" "$1"'
compose exec -T db sh -c 'pg_restore --username="$POSTGRES_USER" --dbname="$1" --exit-on-error --no-owner --no-privileges' sh "$scratch" <"$dump"

for schema in identity planning; do
  migrations="$(query "select count(*) from ${schema}.__ef_migrations_history")"
  [ "$migrations" -gt 0 ] || fail "в схеме $schema нет истории миграций, дамп неполный"
done

users="$(query 'select count(*) from identity.users')"
teams="$(query 'select count(*) from identity.workspaces')"
items="$(query 'select count(*) from planning.items')"
log "Дамп восстанавливается: пользователей $users, команд $teams, узлов $items"
