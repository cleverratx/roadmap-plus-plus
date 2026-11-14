namespace RoadmapPlusPlus.Modules.Identity.Domain.Workspaces
{
    /// <summary>
    /// Участник: пара из пользователя и команды. Глава это признак участника, а не одна из ролей (ADR-0003).
    /// </summary>
    internal sealed class Member
    {
        private readonly List<MemberRole> _roles = [];

        public Member( Guid workspaceId, Guid userId, bool isHead, string color, DateTimeOffset joinedAt )
        {
            WorkspaceId = workspaceId;
            UserId = userId;
            IsHead = isHead;
            Color = color;
            JoinedAt = joinedAt;
        }

        public Guid WorkspaceId { get; private set; }

        public Guid UserId { get; private set; }

        public bool IsHead { get; private set; }

        public string Color { get; private set; }

        public DateTimeOffset JoinedAt { get; private set; }

        public IReadOnlyList<MemberRole> Roles => _roles;

        internal void SetHead( bool isHead )
        {
            IsHead = isHead;
        }

        /// <summary>Заменяет набор ролей целиком: снимает те, которых нет в новом наборе, и выдаёт недостающие.</summary>
        internal void SetRoles( IReadOnlySet<Guid> roleIds )
        {
            _roles.RemoveAll( r => !roleIds.Contains( r.RoleId ) );
            foreach ( Guid roleId in roleIds.Where( id => _roles.All( r => r.RoleId != id ) ) )
            {
                _roles.Add( new MemberRole( WorkspaceId, UserId, roleId ) );
            }
        }

        internal void TakeRole( Guid roleId )
        {
            _roles.RemoveAll( r => r.RoleId == roleId );
        }
    }
}
