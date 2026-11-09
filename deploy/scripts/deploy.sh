#!/usr/bin/env bash
set -euo pipefail
. "$(dirname "${BASH_SOURCE[0]}")/common.sh"

usage() {
  cat <<'TEXT'
Выкладывает приложение на этот сервер.

  bash deploy/scripts/deploy.sh            последний main из origin
  bash deploy/scripts/deploy.sh <ref>      нужный коммит, тег или ветка, например откат на прошлую версию
  bash deploy/scripts/deploy.sh --current  то, что уже лежит в каталоге, без git

Перед выкладкой снимается дамп базы с меткой before-deploy: миграции применяются при старте API,
и если новая версия что-то испортит, к этому дампу можно вернуться через restore.sh.
TEXT
}

ref="origin/main"
case "${1:-}" in
  -h | --help)
    usage
    exit 0
    ;;
  --current) ref="" ;;
  "") ;;
  *) ref="$1" ;;
esac

cd "$ROOT"
[ -f .env ] || fail "нет файла .env, начните с cp .env.example .env и заполните его"

if [ -n "$ref" ]; then
  if ! git diff --quiet || ! git diff --cached --quiet; then
    fail "в каталоге есть незакоммиченные правки, выкладка их бы затёрла"
  fi
  log "Забираю $ref"
  git fetch --prune --tags origin
  git -c advice.detachedHead=false checkout --detach "$ref"
fi

if db_running; then
  run_script backup before-deploy
else
  log "База ещё не запущена, похоже, это первая выкладка: дамп пропускаю"
fi

log "Собираю образы"
compose build --pull

log "Перезапускаю контейнеры"
compose up -d --remove-orphans

wait_for_api
docker image prune --force >/dev/null
log "Выложена версия $(git rev-parse --short HEAD 2>/dev/null || printf 'без git')"
