namespace RoadmapPlusPlus.Modules.Identity.Domain.Workspaces
{
    /// <summary>
    /// Роль команды, как в Discord: у неё есть название и цвет, а участнику можно выдать несколько ролей.
    /// Какие узлы открывает роль, хранит Planning (ADR-0003), здесь только сама роль и кто её носит.
    /// </summary>
    internal sealed class Role
    {
        public const int NameMaxLength = 50;

        public Role( Guid id, Guid workspaceId, string name, string color, DateTimeOffset createdAt )
        {
            Id = id;
            WorkspaceId = workspaceId;
            Name = name;
            Color = color;
            CreatedAt = createdAt;
        }

        public Guid Id { get; private set; }

        public Guid WorkspaceId { get; private set; }

        public string Name { get; private set; }

        public string Color { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        internal void Rename( string name, string color )
        {
            Name = name;
            Color = color;
        }
    }
}
