using Microsoft.EntityFrameworkCore;
using RoadmapPlusPlus.Modules.Identity.Contracts;
using RoadmapPlusPlus.Modules.Identity.Infrastructure.Persistence;

namespace RoadmapPlusPlus.Modules.Identity.Infrastructure
{
    internal sealed class WorkspaceMemberships( IdentityModuleDbContext db ) : IWorkspaceMemberships
    {
        public async Task<IReadOnlyList<WorkspaceMembership>> ListAsync( Guid userId, CancellationToken cancellationToken = default )
        {
            return await db.Members
                .AsNoTracking()
                .Where( m => m.UserId == userId )
                .Select( m => new WorkspaceMembership( m.WorkspaceId, m.IsHead ) )
                .ToListAsync( cancellationToken );
        }

        public Task<WorkspaceMembership?> FindAsync( Guid userId, Guid workspaceId, CancellationToken cancellationToken = default )
        {
            return db.Members
                .AsNoTracking()
                .Where( m => m.UserId == userId && m.WorkspaceId == workspaceId )
                .Select( m => new WorkspaceMembership( m.WorkspaceId, m.IsHead ) )
                .FirstOrDefaultAsync( cancellationToken );
        }
    }
}
