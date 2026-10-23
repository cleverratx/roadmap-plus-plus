# Разработка

## Запуск всего стенда

Нужен только Docker с Compose v2.

```bash
cp .env.example .env
docker compose up -d --build
# UI:      http://localhost:8080
# API:     http://localhost:8080/api
# OpenAPI: http://localhost:8080/api/openapi/v1.json
```

`docker compose` сам подхватывает `compose.override.yaml` с настройками для локальной работы: API запускается в режиме Development, а БД открыта на хосте как `127.0.0.1:5433`. Порт можно поменять через `DB_PORT` в `.env`, а 5432 мы не берём, потому что его часто занимает локальный PostgreSQL. На сервере override не нужен, там запускаем `docker compose -f compose.yaml up -d --build` и задаём в `.env` свой пароль БД.

При старте API сам применяет миграции всех модулей, а схему каждого модуля создаёт его первая миграция. Если базу нужно сбросить начисто, хватит `docker compose down -v`.

## Локальная разработка

Обычно удобнее держать в Docker только базу, а бэкенд и фронт запускать напрямую, тогда изменения подхватываются на лету:

```bash
docker compose up -d db

cd backend/src/Bootstrapper/RoadmapPlusPlus.Api
dotnet watch

cd frontend
npm ci
npm run dev
```

Node нужен 24-й, версия записана в `frontend/.nvmrc`. Vite работает на `:5173` и проксирует `/api` на бэкенд `:5080`. Строка подключения для `dotnet watch` лежит в `appsettings.Development.json` и рассчитана на значения из `.env.example`, так что если вы меняли пароль или порт в `.env`, поправьте и её.

## Миграции

`DbContext` модуля наследует `ModuleDbContext` и регистрируется в `AddServices` модуля вызовом `services.AddModuleDbContext<T>()`. Дальше всё происходит само: таблицы попадают в схему модуля (это имя модуля в нижнем регистре, например `planning`), имена таблиц и колонок пишутся в snake_case, а история миграций лежит в `<схема>.__ef_migrations_history`. Архитектурные тесты следят, чтобы `DbContext` в модуле был один и наследовал `ModuleDbContext`.

Новая миграция создаётся так:

```bash
cd backend
dotnet tool restore
dotnet ef migrations add <Имя> \
  --project src/Modules/Planning/RoadmapPlusPlus.Modules.Planning \
  --startup-project src/Bootstrapper/RoadmapPlusPlus.Api \
  --output-dir Infrastructure/Persistence/Migrations
```

Класс миграции генерируется `internal`, иначе модуль показывал бы наружу не только свой `IModule` и архитектурный тест бы упал. Папки `Migrations/` в `.editorconfig` помечены как сгенерированный код, поэтому `dotnet format` к ним не придирается.

## Изменения API

Если вы добавили или поменяли эндпоинт, соберите бэкенд (документ `backend/openapi/openapi.json` обновится сам), потом выполните `npm run api:generate` во `frontend/` и закоммитьте оба файла вместе с кодом. Как устроена эта цепочка и почему CI на ней падает, описано в [архитектуре](architecture.md#контракт-api).

## Вход и сессия

Как и почему устроен вход, решено в [ADR-0004](decisions/0004-auth.md), а здесь то, что нужно помнить, когда пишешь код.

Всё под `/api` по умолчанию требует входа: группа эндпоинтов в `Program.cs` помечена `RequireAuthorization()`. Без входа сервер отвечает 401 в формате ProblemDetails, а не редиректом. Открыты только health, OpenAPI-документ и эндпоинты `/api/auth/*`, и если новому эндпоинту правда нужен анонимный доступ, это надо явно написать через `.AllowAnonymous()`. Так случайно забыть закрыть эндпоинт не получится.

Кто сейчас вошёл, проще всего узнать через `ICurrentUser` из `Identity.Contracts`, его можно получить в любом модуле. Регистрация и вход ограничены по частоте запросов с одного IP: 10 в минуту, это настраивается секцией `Identity:AuthRateLimit` (`PermitLimit`, `Window`).

На фронте "кто я" отдаёт `useMe()` из `entities/session`: там либо пользователь, либо `null`, если сессии нет. Все страницы приложения обёрнуты в `RequireAuth`, который отправляет гостя на `/login?returnTo=...`, а после входа `GuestOnly` возвращает человека туда, откуда он пришёл. Если любой запрос к API вернул 401, например потому что сессия истекла, клиент TanStack Query сам сбрасывает сессию, и человек оказывается на странице входа.

## Команды

Правила из [ADR-0003](decisions/0003-teams-and-roles.md) живут в агрегате `Workspace` модуля Identity: менять состав команды можно только через его методы, и проверки "это может только глава" и "последнего главу снять нельзя" стоят там, а не в эндпоинтах. Если правило меняется, править нужно `Workspace.cs`, а юнит-тесты на него лежат в `RoadmapPlusPlus.Modules.Identity.Tests`.

Каждое изменение состава увеличивает `Workspace.Version`, а база сохраняет команду, только если версия не изменилась с момента чтения. Поэтому одновременные правки не затирают друг друга: второй запрос получает 409 `identity.concurrent_change`, и человек просто повторяет действие.

Другим модулям про команды нужно знать немного: в каких командах состоит пользователь и где он глава. Для этого есть `IWorkspaceMemberships` в `Identity.Contracts`, в таблицы Identity напрямую никто не ходит. Если пользователь не состоит в команде, отвечаем 404, а не 403, чтобы не выдавать, что такая команда вообще есть.

Ссылка-приглашение многоразовая и живёт 7 дней, у команды одна активная ссылка, а новая сразу отзывает старую. На фронте ссылка ведёт на `/invite/<token>`; гость сначала попадает на вход, а потом возвращается на страницу приглашения.

## Тесты

`dotnet test` в `backend/` запускает все тестовые проекты бэкенда. Интеграционным тестам (`RoadmapPlusPlus.IntegrationTests`) нужен запущенный Docker: они поднимают API в памяти поверх настоящего Postgres, который Testcontainers сам запускает в контейнере (`postgres:18-alpine`, как в compose) и удаляет после прогона. API при старте применяет к этой базе миграции модулей, так что схема в тестах ровно та же, что в проде. Новый интеграционный тест получает API через конструктор (`PostgresApiFactory api`) и ходит в него через `api.CreateClient()`. Каждый такой клиент хранит свои cookie, поэтому, чтобы ходить от имени пользователя, достаточно зарегистрироваться этим же клиентом через `/api/auth/register`; примеры есть в `AuthTests`. База у всех тестов общая, поэтому email делаем уникальным, например с `Guid`.

На фронте `npm test` делает один прогон, а `npm run test:watch` перезапускает тесты при каждом изменении.

## Как появились проекты

Это история этапа 0, повторять её не нужно, но иногда полезно понимать, откуда что взялось.

<details>
<summary>Команды, которыми создавались проекты</summary>

```bash
cd backend
P=RoadmapPlusPlus
dotnet new sln -n $P

dotnet new webapi   -o src/Bootstrapper/$P.Api
dotnet new classlib -o src/BuildingBlocks/$P.BuildingBlocks

for m in Identity Planning Knowledge Interchange Assistant; do
  dotnet new classlib -o src/Modules/$m/$P.Modules.$m
  dotnet add src/Modules/$m/$P.Modules.$m reference src/BuildingBlocks/$P.BuildingBlocks
  dotnet add src/Bootstrapper/$P.Api reference src/Modules/$m/$P.Modules.$m
done

for m in Identity Planning Knowledge Interchange; do
  dotnet new classlib -o src/Modules/$m/$P.Modules.$m.Contracts
  dotnet add src/Modules/$m/$P.Modules.$m.Contracts reference src/BuildingBlocks/$P.BuildingBlocks
  dotnet add src/Modules/$m/$P.Modules.$m reference src/Modules/$m/$P.Modules.$m.Contracts
done

c() { echo src/Modules/$1/$P.Modules.$1.Contracts; }
dotnet add src/Modules/Planning/$P.Modules.Planning       reference $(c Identity)
dotnet add src/Modules/Knowledge/$P.Modules.Knowledge     reference $(c Identity) $(c Planning)
dotnet add src/Modules/Interchange/$P.Modules.Interchange reference $(c Identity) $(c Planning) $(c Knowledge)
dotnet add src/Modules/Assistant/$P.Modules.Assistant     reference $(c Identity) $(c Planning) $(c Interchange)

dotnet sln add src/*/*/*.csproj src/Modules/*/*/*.csproj
```

Сначала каждый модуль получает ссылку на `BuildingBlocks`, а Api подключает все модули. Потом проекты `Contracts` заводятся только у тех модулей, которые вызывают другие, и в конце модули связываются между собой строго через `Contracts`, по графу из ADR-0002.

Фронтенд создан шаблоном `create-vite` (`react-ts`) во временной папке и перенесён в структуру FSD: точка входа `src/app/main.tsx`, алиас `@/` указывает на `src/`.

Тестовые проекты заводим по мере появления тестов и делаем по образцу `RoadmapPlusPlus.ArchitectureTests` (xUnit v3 на Microsoft.Testing.Platform). Шаблон `dotnet new xunit` не подходит, потому что создаёт проект на xUnit v2 и VSTest. Модуль Notifications создадим, когда до него дойдём.

</details>
