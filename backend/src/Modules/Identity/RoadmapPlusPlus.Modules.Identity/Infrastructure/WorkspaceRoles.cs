using Microsoft.EntityFrameworkCore;
using RoadmapPlusPlus.Modules.Identity.Contracts;
using RoadmapPlusPlus.Modules.Identity.Infrastructure.Persistence;

namespace RoadmapPlusPlus.Modules.Identity.Infrastructure
{
    internal sealed class WorkspaceRoles( IdentityModuleDbContext db ) : IWorkspaceRoles
    {
        public async Task<IReadOnlyList<Guid>> ListIdsAsync( Guid workspaceId, CancellationToken cancellationToken = default )
        {
            return await db.Roles
                .AsNoTracking()
                .Where( r => r.WorkspaceId == workspaceId )
                .Select( r => r.Id )
                .ToListAsync( cancellationToken );
        }
    }
}
