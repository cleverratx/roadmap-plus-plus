using Microsoft.EntityFrameworkCore;
using RoadmapPlusPlus.BuildingBlocks;
using RoadmapPlusPlus.Modules.Identity.Contracts;
using RoadmapPlusPlus.Modules.Planning.Application.Items;
using RoadmapPlusPlus.Modules.Planning.Domain.Access;
using RoadmapPlusPlus.Modules.Planning.Domain.Items;
using RoadmapPlusPlus.Modules.Planning.Infrastructure.Persistence;

namespace RoadmapPlusPlus.Modules.Planning.Application.Access
{
    /// <summary>Какие роли открывают узел: проверка, замена набора и чтение для панели.</summary>
    internal sealed class ItemGrants( PlanningDbContext db, IWorkspaceRoles roles )
    {
        /// <summary>Открывать узлы ролям может только глава, и только ролям своей команды: роли других команд Planning не принимает.</summary>
        public async Task<Result> CheckAsync( ItemScope scope, Guid workspaceId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken )
        {
            if ( !scope.IsHead( workspaceId ) )
            {
                return PlanningErrors.NotHead;
            }

            IReadOnlyList<Guid> known = await roles.ListIdsAsync( workspaceId, cancellationToken );
            return roleIds.All( known.Contains ) ? Result.Success() : PlanningErrors.UnknownRole;
        }

        /// <summary>Заменяет набор ролей узла целиком. Сохраняет вызывающий, вместе с остальными изменениями.</summary>
        public async Task ReplaceAsync( Guid itemId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken )
        {
            List<ItemRole> current = await db.ItemRoles.Where( r => r.ItemId == itemId ).ToListAsync( cancellationToken );
            db.ItemRoles.RemoveRange( current.Where( r => !roleIds.Contains( r.RoleId ) ) );
            db.ItemRoles.AddRange( roleIds.Distinct().Where( id => current.All( r => r.RoleId != id ) ).Select( id => new ItemRole( itemId, id ) ) );
        }

        public Task<List<Guid>> OfAsync( IEnumerable<Guid> itemIds, CancellationToken cancellationToken )
        {
            Guid[] ids = [ .. itemIds ];
            return db.ItemRoles
                .AsNoTracking()
                .Where( r => ids.Contains( r.ItemId ) )
                .Select( r => r.RoleId )
                .Distinct()
                .ToListAsync( cancellationToken );
        }
    }
}
