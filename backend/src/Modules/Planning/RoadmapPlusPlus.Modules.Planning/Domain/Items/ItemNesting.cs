namespace RoadmapPlusPlus.Modules.Planning.Domain.Items
{
    /// <summary>
    /// Что во что вкладывается. На верхнем уровне живут макрособытия и события; в макрособытие кладутся макрособытия
    /// и события; в событие и группу кладутся группы и задачи. Та же таблица есть на фронте в <c>entities/item</c>.
    /// </summary>
    internal static class ItemNesting
    {
        public static bool CanBeRoot( ItemKind kind )
        {
            return kind is ItemKind.Macro or ItemKind.Event;
        }

        public static bool CanContain( ItemKind parent, ItemKind child )
        {
            return parent switch
            {
                ItemKind.Macro => child is ItemKind.Macro or ItemKind.Event,
                ItemKind.Event or ItemKind.Group => child is ItemKind.Group or ItemKind.Task,
                _ => false,
            };
        }
    }
}
