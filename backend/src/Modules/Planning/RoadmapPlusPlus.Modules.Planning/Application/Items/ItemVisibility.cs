using RoadmapPlusPlus.Modules.Identity.Contracts;
using RoadmapPlusPlus.Modules.Planning.Domain.Items;
using RoadmapPlusPlus.Modules.Planning.Infrastructure.Persistence;

namespace RoadmapPlusPlus.Modules.Planning.Application.Items
{
    /// <summary>
    /// Единственное место, где решается, какие узлы видны пользователю. Все выборки дерева начинаются отсюда,
    /// поэтому, когда ветка 17 добавит роли (ADR-0003), править придётся только этот класс.
    /// Сейчас правило простое: участник команды видит все её узлы.
    /// </summary>
    internal sealed class ItemVisibility( PlanningDbContext db, ICurrentUser currentUser, IWorkspaceMemberships memberships )
    {
        public async Task<IQueryable<Item>> VisibleItemsAsync( CancellationToken cancellationToken )
        {
            IReadOnlyList<WorkspaceMembership> mine = await memberships.ListAsync( currentUser.Id, cancellationToken );
            List<Guid> workspaceIds = mine.Select( m => m.WorkspaceId ).ToList();

            return db.Items.Where( i => workspaceIds.Contains( i.WorkspaceId ) );
        }

        public async Task<bool> CanCreateInWorkspaceAsync( Guid workspaceId, CancellationToken cancellationToken )
        {
            return await memberships.FindAsync( currentUser.Id, workspaceId, cancellationToken ) is not null;
        }
    }
}
