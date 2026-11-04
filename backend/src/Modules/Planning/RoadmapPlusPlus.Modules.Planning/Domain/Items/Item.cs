using RoadmapPlusPlus.BuildingBlocks;

namespace RoadmapPlusPlus.Modules.Planning.Domain.Items
{
    /// <summary>
    /// Узел дерева: макрособытие, событие, группа или задача. Узел и все его потомки принадлежат одной команде,
    /// поэтому у дочернего узла команда всегда берётся у родителя и не передаётся снаружи.
    /// </summary>
    internal sealed class Item
    {
        private Item()
        {
            Title = string.Empty;
            DescriptionMd = string.Empty;
        }

        public Guid Id { get; private set; }

        public Guid WorkspaceId { get; private set; }

        public Guid? ParentId { get; private set; }

        public ItemKind Kind { get; private set; }

        public string Title { get; private set; }

        public DateOnly? StartDate { get; private set; }

        public DateOnly? EndDate { get; private set; }

        public string DescriptionMd { get; private set; }

        /// <summary>Порядок среди соседей; новый узел встаёт в конец. Перестановку добавит ветка <c>planning-item-move</c>.</summary>
        public int Position { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public DateTimeOffset UpdatedAt { get; private set; }

        public static Result<Item> CreateRoot( Guid workspaceId, ItemKind kind, ItemContent content, int position, DateTimeOffset now )
        {
            if ( !ItemNesting.CanBeRoot( kind ) )
            {
                return PlanningErrors.CannotBeRoot( kind );
            }

            return New( workspaceId, null, kind, content, position, now );
        }

        public static Result<Item> CreateChild( Item parent, ItemKind kind, ItemContent content, int position, DateTimeOffset now )
        {
            if ( !ItemNesting.CanContain( parent.Kind, kind ) )
            {
                return PlanningErrors.CannotContain( parent.Kind, kind );
            }

            return New( parent.WorkspaceId, parent.Id, kind, content, position, now );
        }

        public void Update( ItemContent content, DateTimeOffset now )
        {
            Apply( content );
            UpdatedAt = now;
        }

        private static Item New( Guid workspaceId, Guid? parentId, ItemKind kind, ItemContent content, int position, DateTimeOffset now )
        {
            Item item = new()
            {
                Id = Guid.CreateVersion7(),
                WorkspaceId = workspaceId,
                ParentId = parentId,
                Kind = kind,
                Position = position,
                CreatedAt = now,
                UpdatedAt = now,
            };

            item.Apply( content );
            return item;
        }

        private void Apply( ItemContent content )
        {
            Title = content.Title;
            StartDate = content.StartDate;
            EndDate = content.EndDate;
            DescriptionMd = content.DescriptionMd;
        }
    }
}
