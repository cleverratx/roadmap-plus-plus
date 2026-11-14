using Microsoft.EntityFrameworkCore;
using RoadmapPlusPlus.Modules.Identity.Contracts;
using RoadmapPlusPlus.Modules.Identity.Domain.Workspaces;
using RoadmapPlusPlus.Modules.Identity.Infrastructure.Persistence;

namespace RoadmapPlusPlus.Modules.Identity.Infrastructure
{
    internal sealed class WorkspaceMemberships( IdentityModuleDbContext db ) : IWorkspaceMemberships
    {
        public async Task<IReadOnlyList<WorkspaceMembership>> ListAsync( Guid userId, CancellationToken cancellationToken = default )
        {
            return await Memberships( m => m.UserId == userId ).ToListAsync( cancellationToken );
        }

        public Task<WorkspaceMembership?> FindAsync( Guid userId, Guid workspaceId, CancellationToken cancellationToken = default )
        {
            return Memberships( m => m.UserId == userId && m.WorkspaceId == workspaceId ).FirstOrDefaultAsync( cancellationToken );
        }

        private IQueryable<WorkspaceMembership> Memberships( System.Linq.Expressions.Expression<Func<Member, bool>> filter )
        {
            return
                from member in db.Members.AsNoTracking().Where( filter )
                join workspace in db.Workspaces on member.WorkspaceId equals workspace.Id
                select new WorkspaceMembership(
                    member.WorkspaceId,
                    member.IsHead,
                    workspace.RolesEnabled,
                    member.Roles.Select( r => r.RoleId ).ToList() );
        }
    }
}
