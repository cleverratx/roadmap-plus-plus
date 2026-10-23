namespace RoadmapPlusPlus.Modules.Identity.Domain.Workspaces
{
    /// <summary>
    /// Участник: пара из пользователя и команды. Глава это признак участника, а не одна из ролей (ADR-0003).
    /// </summary>
    internal sealed class Member
    {
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

        internal void SetHead( bool isHead )
        {
            IsHead = isHead;
        }
    }
}
