#!/usr/bin/env bash
set -euo pipefail
. "$(dirname "${BASH_SOURCE[0]}")/common.sh"

usage() {
  cat <<'TEXT'
Заменяет рабочую базу содержимым дампа.

  bash deploy/scripts/restore.sh <файл дампа> [--yes]

Порядок такой: дамп проверяется во временной базе, с текущей базы снимается страховочный дамп,
API останавливается, база пересоздаётся из дампа, API запускается снова. Без --yes скрипт
попросит ввести имя базы для подтверждения.
TEXT
}

dump=""
confirmed=false
for arg in "$@"; do
  case "$arg" in
    -h | --help)
      usage
      exit 0
      ;;
    --yes) confirmed=true ;;
    *) dump="$arg" ;;
  esac
done

if [ -z "$dump" ]; then
  usage
  exit 1
fi
[ -s "$dump" ] || fail "файл дампа $dump пустой или его нет"
db_running || fail "контейнер базы не запущен"

database="$(compose exec -T db sh -c 'printf %s "$POSTGRES_DB"')"

if [ "$confirmed" != true ]; then
  printf 'База %s будет заменена содержимым %s.\nВведите имя базы, чтобы продолжить: ' "$database" "$dump"
  read -r answer
  [ "$answer" = "$database" ] || fail "имя не совпало, ничего не меняю"
fi

run_script verify-backup "$dump"

log "Снимаю страховочный дамп текущей базы"
run_script backup before-restore

log "Останавливаю API"
compose stop api

log "Пересоздаю базу $database из $dump"
compose exec -T db sh -c 'dropdb --username="$POSTGRES_USER" --force "$POSTGRES_DB" && createdb --username="$POSTGRES_USER" "$POSTGRES_DB"'
compose exec -T db sh -c 'pg_restore --username="$POSTGRES_USER" --dbname="$POSTGRES_DB" --exit-on-error --no-owner --no-privileges' <"$dump"

log "Запускаю API"
compose start api
wait_for_api
log "База восстановлена из $dump"
