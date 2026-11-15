namespace RoadmapPlusPlus.Modules.Identity.Contracts
{
    /// <summary>
    /// Какие роли есть в команде. Planning проверяет по нему, что узел открывают ролям той же команды,
    /// потому что привязки к ролям лежат в его схеме и ссылаются на роль только по Id, без внешнего ключа (ADR-0003).
    /// </summary>
    public interface IWorkspaceRoles
    {
        Task<IReadOnlyList<Guid>> ListIdsAsync( Guid workspaceId, CancellationToken cancellationToken = default );
    }
}
