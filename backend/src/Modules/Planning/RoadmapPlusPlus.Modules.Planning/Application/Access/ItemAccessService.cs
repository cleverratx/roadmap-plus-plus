using Microsoft.EntityFrameworkCore;
using RoadmapPlusPlus.BuildingBlocks;
using RoadmapPlusPlus.Modules.Identity.Contracts;
using RoadmapPlusPlus.Modules.Planning.Application.Items;
using RoadmapPlusPlus.Modules.Planning.Domain.Items;
using RoadmapPlusPlus.Modules.Planning.Infrastructure.Persistence;

namespace RoadmapPlusPlus.Modules.Planning.Application.Access
{
    /// <summary>Кто что видит при включённых ролях: какие роли открывают узел и что пока не открыто никому.</summary>
    internal sealed class ItemAccessService(
        PlanningDbContext db,
        ItemVisibility visibility,
        ItemGrants grants,
        ItemService items,
        IWorkspaceRoles roles )
    {
        /// <summary>Сколько примеров неоткрытых событий показать главе рядом с предупреждением.</summary>
        public const int ExampleCount = 5;

        public async Task<Result<ItemDetails>> SetRolesAsync( Guid itemId, SetItemRolesRequest request, CancellationToken cancellationToken )
        {
            ItemScope scope = await visibility.ScopeAsync( cancellationToken );
            Item? item = await scope.Visible( db.Items ).AsNoTracking().FirstOrDefaultAsync( i => i.Id == itemId, cancellationToken );
            if ( item is null )
            {
                return PlanningErrors.ItemNotFound;
            }

            IReadOnlyList<Guid> roleIds = request.RoleIds ?? [];
            Result allowed = await grants.CheckAsync( scope, item.WorkspaceId, roleIds, cancellationToken );
            if ( !allowed.IsSuccess )
            {
                return allowed.Error;
            }

            await grants.ReplaceAsync( item.Id, roleIds, cancellationToken );
            await db.SaveChangesAsync( cancellationToken );
            return await items.GetAsync( item.Id, cancellationToken );
        }

        /// <summary>
        /// События команды, которые не открыты ни одной её роли ни сами, ни через предка. Когда роли включены, их видят только главы,
        /// поэтому глава получает предупреждение вида "N событий не открыты ни одной роли" (ADR-0003).
        /// </summary>
        public async Task<Result<AccessSummary>> SummaryAsync( Guid workspaceId, CancellationToken cancellationToken )
        {
            ItemScope scope = await visibility.ScopeAsync( cancellationToken );
            if ( scope.In( workspaceId ) is null )
            {
                return PlanningErrors.WorkspaceNotFound;
            }

            if ( !scope.IsHead( workspaceId ) )
            {
                return PlanningErrors.NotHead;
            }

            Guid[] roleIds = [ .. await roles.ListIdsAsync( workspaceId, cancellationToken ) ];
            List<Guid> covered = roleIds.Length == 0
                ? []
                : await db.Database
                    .SqlQuery<Guid>( $"""
                        WITH RECURSIVE covered AS (
                            SELECT i.id FROM planning.items i
                            WHERE i.workspace_id = {workspaceId}
                              AND EXISTS (SELECT 1 FROM planning.item_roles r WHERE r.item_id = i.id AND r.role_id = ANY({roleIds}))
                            UNION
                            SELECT child.id FROM planning.items child JOIN covered parent ON child.parent_id = parent.id
                        )
                        SELECT id AS "Value" FROM covered
                        """ )
                    .ToListAsync( cancellationToken );

            IQueryable<Item> unshared = db.Items
                .AsNoTracking()
                .Where( i => i.WorkspaceId == workspaceId && i.Kind == ItemKind.Event && !covered.Contains( i.Id ) );

            int count = await unshared.CountAsync( cancellationToken );
            List<Item> examples = await unshared
                .OrderBy( i => i.StartDate == null )
                .ThenBy( i => i.StartDate )
                .ThenBy( i => i.Title )
                .Take( ExampleCount )
                .ToListAsync( cancellationToken );

            return new AccessSummary( count, examples.Select( i => ItemResponse.From( i ) ).ToList() );
        }
    }
}
