namespace RoadmapPlusPlus.Modules.Identity.Domain.Workspaces
{
    /// <summary>Роль, выданная участнику. Пара участник и роль, у одного участника таких может быть несколько.</summary>
    internal sealed class MemberRole
    {
        public MemberRole( Guid workspaceId, Guid userId, Guid roleId )
        {
            WorkspaceId = workspaceId;
            UserId = userId;
            RoleId = roleId;
        }

        public Guid WorkspaceId { get; private set; }

        public Guid UserId { get; private set; }

        public Guid RoleId { get; private set; }
    }
}
