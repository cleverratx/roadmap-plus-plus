using RoadmapPlusPlus.BuildingBlocks;

namespace RoadmapPlusPlus.Modules.Planning.Domain.Items
{
    internal static class PlanningErrors
    {
        /// <summary>Узла нет или он не виден пользователю. Один ответ на оба случая, как и у команд.</summary>
        public static readonly Error ItemNotFound = Error.NotFound(
            "planning.item_not_found",
            "Узел не найден." );

        public static readonly Error WorkspaceNotFound = Error.NotFound(
            "planning.workspace_not_found",
            "Команда не найдена." );

        public static readonly Error LinkNotFound = Error.NotFound(
            "planning.link_not_found",
            "Ссылка не найдена." );

        public static Error TooManyLinks( int max )
        {
            return Error.Validation( "planning.too_many_links", $"У одного узла может быть не больше {max} ссылок." );
        }

        public static Error InvalidRequest( IReadOnlyDictionary<string, string[]> fields )
        {
            return Error.Validation( "planning.invalid_request", "Проверьте поля формы.", fields );
        }

        public static Error CannotBeRoot( ItemKind kind )
        {
            return Nesting( $"{Accusative( kind )} нельзя положить на верхний уровень: она живёт внутри события." );
        }

        /// <summary>Перенос внутрь самого себя или своего потомка замкнул бы дерево в кольцо и оторвал поддерево от корня.</summary>
        public static Error MoveIntoItself()
        {
            const string message = "Нельзя переместить узел внутрь него самого или того, что лежит в нём.";
            return Error.Validation( "planning.move_cycle", message, new Dictionary<string, string[]> { [ "parentId" ] = [ message ] } );
        }

        /// <summary>
        /// Поддерево целиком принадлежит одной команде. Перенос в другую команду сменил бы команду у всех потомков,
        /// а с ролями из ADR-0003 и их доступ, поэтому пока его нет.
        /// </summary>
        public static Error MoveAcrossWorkspaces()
        {
            const string message = "Переносить узлы между командами пока нельзя.";
            return Error.Validation( "planning.move_across_workspaces", message, new Dictionary<string, string[]> { [ "parentId" ] = [ message ] } );
        }

        public static Error CannotContain( ItemKind parent, ItemKind child )
        {
            string allowed = parent == ItemKind.Macro ? "макрособытия и события" : "группы и задачи";
            string inside = parent switch
            {
                ItemKind.Macro => "Внутри макрособытия",
                ItemKind.Event => "Внутри события",
                ItemKind.Group => "Внутри группы",
                _ => "Внутри задачи",
            };

            return parent == ItemKind.Task
                ? Nesting( "Внутри задачи ничего не бывает." )
                : Nesting( $"{inside} бывают только {allowed}, а {Nominative( child )} сюда не подходит." );
        }

        private static Error Nesting( string message )
        {
            return Error.Validation( "planning.invalid_nesting", message, new Dictionary<string, string[]> { [ "kind" ] = [ message ] } );
        }

        private static string Accusative( ItemKind kind )
        {
            return kind == ItemKind.Group ? "Группу" : "Задачу";
        }

        private static string Nominative( ItemKind kind )
        {
            return kind switch
            {
                ItemKind.Macro => "макрособытие",
                ItemKind.Event => "событие",
                ItemKind.Group => "группа",
                _ => "задача",
            };
        }
    }
}
