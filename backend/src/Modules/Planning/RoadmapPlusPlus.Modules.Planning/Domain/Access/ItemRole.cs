namespace RoadmapPlusPlus.Modules.Planning.Domain.Access
{
    /// <summary>
    /// Узел открыт роли, и это действует на всё его поддерево (ADR-0003). Роль живёт в Identity, поэтому здесь на неё
    /// только Id, без внешнего ключа между схемами. Если роль удалили, привязка просто перестаёт совпадать с ролями участников.
    /// </summary>
    internal sealed class ItemRole
    {
        public ItemRole( Guid itemId, Guid roleId )
        {
            ItemId = itemId;
            RoleId = roleId;
        }

        public Guid ItemId { get; private set; }

        public Guid RoleId { get; private set; }
    }
}
