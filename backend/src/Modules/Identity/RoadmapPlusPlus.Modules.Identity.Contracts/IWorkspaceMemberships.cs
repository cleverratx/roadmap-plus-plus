namespace RoadmapPlusPlus.Modules.Identity.Contracts
{
    /// <summary>
    /// В каких командах состоит пользователь и где он глава. Через это другие модули решают, что пользователю видно и можно,
    /// не заглядывая в таблицы Identity (ADR-0002, ADR-0003).
    /// </summary>
    public interface IWorkspaceMemberships
    {
        Task<IReadOnlyList<WorkspaceMembership>> ListAsync( Guid userId, CancellationToken cancellationToken = default );

        Task<WorkspaceMembership?> FindAsync( Guid userId, Guid workspaceId, CancellationToken cancellationToken = default );
    }

    public sealed record WorkspaceMembership( Guid WorkspaceId, bool IsHead );
}
