# Структура репозитория

Здесь собрано, что где лежит и зачем. Часть папок пока пустая: они заведены заранее, чтобы было понятно, куда класть код, когда до него дойдёт очередь.

```
roadmap-plus-plus/
├── README.md
├── .editorconfig, .gitattributes, .gitignore
├── compose.yaml                             # стенд: db + api + nginx
├── compose.override.yaml                    # для локальной работы: Development, порт БД наружу
├── .env.example                             # переменные для compose, копируется в .env
├── .github/workflows/                       # CI: backend.yml (формат, сборка, OpenAPI-документ, тесты)
│                                            #     frontend.yml (клиент API, линтер, тесты, сборка)
│
├── backend/
│   ├── Dockerfile                           # образ API: сборка на SDK, запуск на aspnet
│   ├── RoadmapPlusPlus.slnx                 # решение (.NET 10 создаёт .slnx вместо .sln)
│   ├── global.json                          # SDK 10.0.x, dotnet test в режиме Microsoft.Testing.Platform
│   ├── Directory.Build.props                # общие настройки: net10.0, nullable, предупреждения = ошибки
│   ├── Directory.Packages.props             # версии всех NuGet-пакетов в одном месте
│   ├── .config/dotnet-tools.json            # dotnet-ef как локальный инструмент (dotnet tool restore)
│   ├── openapi/openapi.json                 # контракт API: генерируется при сборке и лежит в git
│   ├── src/
│   │   ├── Bootstrapper/
│   │   │   └── RoadmapPlusPlus.Api/         # Program.cs: подключает модули, БД и миграции, auth, OpenAPI, health
│   │   │       ├── Middleware/              # ошибки в ProblemDetails, необработанные исключения, X-Request-Id
│   │   │       ├── OpenApi/                 # режим генерации OpenAPI-документа при сборке: без миграций и логов
│   │   │       └── Persistence/             # настройки dotnet ef: миграции генерируются internal
│   │   ├── BuildingBlocks/
│   │   │   └── RoadmapPlusPlus.BuildingBlocks/   # IModule, ModuleDbContext, Result, Entity, шина событий
│   │   └── Modules/
│   │       ├── Identity/
│   │       │   ├── RoadmapPlusPlus.Modules.Identity/            # пользователи, вход, сессия в cookie, ключи Data Protection
│   │       │   └── RoadmapPlusPlus.Modules.Identity.Contracts/  # текущий пользователь, проверка прав
│   │       ├── Planning/                                         # ядро
│   │       │   ├── RoadmapPlusPlus.Modules.Planning/
│   │       │   │   ├── Domain/                                   # Items, Fields, ChangeSets
│   │       │   │   ├── Application/                              # Items, ChangeSets, Agenda, Summary
│   │       │   │   ├── Infrastructure/Persistence/               # PlanningDbContext (схема planning), миграции
│   │       │   │   └── Endpoints/
│   │       │   └── RoadmapPlusPlus.Modules.Planning.Contracts/  # чтение дерева, Propose/ApplyChangeSet, события
│   │       ├── Knowledge/
│   │       │   ├── RoadmapPlusPlus.Modules.Knowledge/           # MD-документы, вложения, поиск
│   │       │   └── RoadmapPlusPlus.Modules.Knowledge.Contracts/
│   │       ├── Interchange/
│   │       │   ├── RoadmapPlusPlus.Modules.Interchange/
│   │       │   │   ├── Application/                              # TaskTable, ITaskTableImporter/Exporter
│   │       │   │   ├── Adapters/
│   │       │   │   │   ├── Markdown/                             # task-table@0, делаем в любом случае
│   │       │   │   │   ├── AiContext/                            # база знаний и задачи одним файлом
│   │       │   │   │   ├── Xlsx/                                 # вариант D из ADR-0001
│   │       │   │   │   └── GoogleSheets/                         # вариант C из ADR-0001, пока пусто
│   │       │   │   └── Endpoints/
│   │       │   └── RoadmapPlusPlus.Modules.Interchange.Contracts/
│   │       ├── Assistant/
│   │       │   └── RoadmapPlusPlus.Modules.Assistant/           # без Contracts, его никто не вызывает
│   │       │       ├── Application/                              # сценарии: текст в ChangeSet, расписание по часам
│   │       │       ├── Providers/                                # Mock, OpenAI-совместимый, Anthropic
│   │       │       ├── Prompts/                                  # промпты в .md
│   │       │       ├── Infrastructure/                           # сессии, учёт токенов
│   │       │       └── Endpoints/
│   │       └── Notifications/                                    # позже: Telegram-бот
│   └── tests/
│       ├── RoadmapPlusPlus.ArchitectureTests/       # границы модулей: только *.Contracts, без циклов
│       ├── RoadmapPlusPlus.BuildingBlocks.Tests/    # Result, Error и их перевод в HTTP-ответ
│       ├── RoadmapPlusPlus.Modules.Planning.Tests/
│       ├── RoadmapPlusPlus.Modules.Knowledge.Tests/
│       ├── RoadmapPlusPlus.Modules.Interchange.Tests/
│       │   └── Fixtures/                            # bpmn-hackathon-roadmap.md, реальная таблица в MD
│       ├── RoadmapPlusPlus.Modules.Assistant.Tests/
│       └── RoadmapPlusPlus.IntegrationTests/        # API целиком поверх Postgres в Testcontainers
│
├── frontend/                                # Feature-Sliced Design, облегчённый
│   ├── Dockerfile                           # образ nginx со сборкой фронта
│   ├── public/
│   └── src/
│       ├── app/                             # вход (main.tsx, App.tsx), роутер, провайдеры, каркас страницы, стили
│       ├── pages/                           # roadmap, event, agenda, knowledge, settings, login, register, not-found
│       ├── widgets/                         # app-header, timeline, event-panel, task-table, agenda-list,
│       │                                    # kb-viewer, assistant-panel
│       ├── features/                        # auth, timeline-zoom, item-edit, task-status,
│       │                                    # md-export, md-import, changeset-review
│       ├── entities/                        # session, item, member, field, kb-doc
│       └── shared/                          # api (типы из OpenAPI, клиент, ошибки), ui (кит),
│                                            # lib (утилиты, настройка тестов), config (пути)
│
├── deploy/
│   ├── nginx/default.conf                   # статика фронта и прокси /api на api:8080
│   └── scripts/                             # деплой, бэкап БД
│
└── docs/
    ├── product.md                           # что умеет сервис и каким он задуман
    ├── domain-model.md                      # дерево, команды и роли, сущности
    ├── architecture.md                      # модули, границы, контракт API, ошибки, стек
    ├── repository-structure.md              # этот файл
    ├── development.md                       # запуск, локальная разработка, миграции, тесты
    ├── contributing.md                      # как называть ветки, коммиты и PR и как их вливать
    ├── plan.md                              # этапы и ветки текущего этапа
    ├── decisions/                           # ADR, архитектурные решения
    │   ├── 0001-task-table-source.md        # открыт: Google Таблицы или своя таблица
    │   ├── 0002-modular-monolith.md         # принят: модульный монолит
    │   ├── 0003-teams-and-roles.md          # принят: только командные роадмапы, роли и видимость
    │   └── 0004-auth.md                     # принят: вход по email и паролю, сессия в cookie
    ├── formats/
    │   ├── task-table-md.md                 # черновик формата MD для задач
    │   └── examples/                        # наша таблица BPMN-хакатона в MD
    └── design/                              # макеты, скриншоты, референсы интерфейса
```

Во фронтенде слои импортируют друг друга только сверху вниз: `app`, потом `pages`, `widgets`, `features`, `entities` и в самом низу `shared`. Поэтому, например, `shared` ничего не знает о страницах. У каждого среза есть `index.ts`, через него и импортируем: `import { Button } from '@/shared/ui'`, а не из внутреннего файла.
