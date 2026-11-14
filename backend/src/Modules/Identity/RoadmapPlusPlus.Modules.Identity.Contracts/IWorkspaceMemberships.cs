namespace RoadmapPlusPlus.Modules.Identity.Contracts
{
    /// <summary>
    /// В каких командах состоит пользователь, где он глава, включены ли там роли и какие роли у него есть.
    /// Через это другие модули решают, что пользователю видно и можно, не заглядывая в таблицы Identity (ADR-0002, ADR-0003).
    /// </summary>
    public interface IWorkspaceMemberships
    {
        Task<IReadOnlyList<WorkspaceMembership>> ListAsync( Guid userId, CancellationToken cancellationToken = default );

        Task<WorkspaceMembership?> FindAsync( Guid userId, Guid workspaceId, CancellationToken cancellationToken = default );
    }

    /// <summary>
    /// Участие пользователя в одной команде. <see cref="RoleIds"/> заполнен, даже когда роли выключены:
    /// решать, учитывать ли их, должен тот, кто проверяет доступ, по <see cref="RolesEnabled"/>.
    /// </summary>
    public sealed record WorkspaceMembership( Guid WorkspaceId, bool IsHead, bool RolesEnabled, IReadOnlyList<Guid> RoleIds );
}
