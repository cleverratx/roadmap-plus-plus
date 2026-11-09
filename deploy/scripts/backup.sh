#!/usr/bin/env bash
set -euo pipefail
. "$(dirname "${BASH_SOURCE[0]}")/common.sh"

usage() {
  cat <<'TEXT'
Снимает дамп базы, проверяет, что он восстанавливается, и кладёт его в каталог бэкапов.

  bash deploy/scripts/backup.sh [метка]

Метка попадает в имя файла, например before-deploy. Настройки берутся из окружения или из .env:
  BACKUP_DIR           куда класть дампы, по умолчанию backups/ в корне репозитория
  BACKUP_KEEP_DAYS     сколько дней хранить старые дампы, по умолчанию 14
  BACKUP_RSYNC_TARGET  куда ещё скопировать дамп через rsync, например backup@storage:/srv/roadmap/
TEXT
}

case "${1:-}" in
  -h | --help)
    usage
    exit 0
    ;;
esac

label="${1:+-$1}"
dir="$(backup_dir)"
keep_days="$(setting BACKUP_KEEP_DAYS 14)"
rsync_target="$(setting BACKUP_RSYNC_TARGET "")"

db_running || fail "контейнер базы не запущен, снимать дамп не с чего"

umask 077
name="roadmap-$(date -u '+%Y%m%d-%H%M%S')${label}.dump"
partial="$dir/.$name.partial"
trap 'rm -f "$partial"' EXIT

log "Снимаю дамп базы"
compose exec -T db sh -c 'pg_dump --username="$POSTGRES_USER" --dbname="$POSTGRES_DB" --format=custom' >"$partial"

run_script verify-backup "$partial"

mv "$partial" "$dir/$name"
log "Дамп сохранён: $dir/$name ($(du -h "$dir/$name" | cut -f1))"

find "$dir" -maxdepth 1 -name 'roadmap-*.dump' -mtime +"$keep_days" -print -delete | sed 's/^/Удалён старый дамп: /'

if [ -n "$rsync_target" ]; then
  log "Копирую дамп в $rsync_target"
  rsync --archive "$dir/$name" "$rsync_target"
fi
