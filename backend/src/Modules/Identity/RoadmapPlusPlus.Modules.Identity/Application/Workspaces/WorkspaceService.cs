using Microsoft.EntityFrameworkCore;
using RoadmapPlusPlus.BuildingBlocks;
using RoadmapPlusPlus.Modules.Identity.Contracts;
using RoadmapPlusPlus.Modules.Identity.Domain.Workspaces;
using RoadmapPlusPlus.Modules.Identity.Infrastructure.Persistence;

namespace RoadmapPlusPlus.Modules.Identity.Application.Workspaces
{
    /// <summary>
    /// Сценарии команды. Каждый изменяющий сценарий устроен одинаково: загрузить агрегат <see cref="Workspace"/>,
    /// вызвать его метод (правила проверяет он), сохранить. Если состав успели поменять параллельно, база откажет
    /// по версии, и сценарий вернёт <see cref="WorkspaceErrors.ConcurrentChange"/>.
    /// </summary>
    internal sealed class WorkspaceService( IdentityModuleDbContext db, ICurrentUser currentUser, TimeProvider time )
    {
        public async Task<IReadOnlyList<WorkspaceSummary>> ListMineAsync( CancellationToken cancellationToken )
        {
            Guid me = currentUser.Id;

            return await (
                    from member in db.Members
                    join workspace in db.Workspaces on member.WorkspaceId equals workspace.Id
                    where member.UserId == me
                    orderby workspace.Name
                    select new WorkspaceSummary(
                        workspace.Id,
                        workspace.Name,
                        workspace.Color,
                        member.IsHead,
                        db.Members.Count( m => m.WorkspaceId == workspace.Id ) ) )
                .AsNoTracking()
                .ToListAsync( cancellationToken );
        }

        public async Task<Result<WorkspaceDetails>> CreateAsync( CreateWorkspaceRequest request, CancellationToken cancellationToken )
        {
            Guid me = currentUser.Id;
            string name = request.Name?.Trim() ?? string.Empty;
            Dictionary<string, string[]> fields = ValidateName( name );
            if ( request.Color is not null && !WorkspacePalette.IsValid( request.Color ) )
            {
                fields[ "color" ] = [ "Цвет должен быть в формате #RRGGBB." ];
            }

            if ( fields.Count > 0 )
            {
                return WorkspaceErrors.InvalidRequest( fields );
            }

            int myWorkspaces = await db.Members.CountAsync( m => m.UserId == me, cancellationToken );
            string color = request.Color ?? WorkspacePalette.ForWorkspace( myWorkspaces );

            Workspace workspace = Workspace.Create( name, color, me, time.GetUtcNow() );
            db.Workspaces.Add( workspace );
            await db.SaveChangesAsync( cancellationToken );

            return await DetailsAsync( workspace, cancellationToken );
        }

        public async Task<Result<WorkspaceDetails>> GetAsync( Guid workspaceId, CancellationToken cancellationToken )
        {
            Workspace? workspace = await LoadMineAsync( workspaceId, cancellationToken );
            return workspace is null ? WorkspaceErrors.NotFound : await DetailsAsync( workspace, cancellationToken );
        }

        public async Task<Result<WorkspaceDetails>> UpdateAsync( Guid workspaceId, UpdateWorkspaceRequest request, CancellationToken cancellationToken )
        {
            string name = request.Name?.Trim() ?? string.Empty;
            Dictionary<string, string[]> fields = ValidateName( name );
            if ( !WorkspacePalette.IsValid( request.Color ) )
            {
                fields[ "color" ] = [ "Цвет должен быть в формате #RRGGBB." ];
            }

            if ( fields.Count > 0 )
            {
                return WorkspaceErrors.InvalidRequest( fields );
            }

            return await ChangeAsync( workspaceId, w => w.Update( currentUser.Id, name, request.Color ), cancellationToken );
        }

        public Task<Result<WorkspaceDetails>> SetHeadAsync( Guid workspaceId, Guid userId, SetHeadRequest request, CancellationToken cancellationToken )
        {
            return ChangeAsync( workspaceId, w => w.SetHead( currentUser.Id, userId, request.IsHead ), cancellationToken );
        }

        public async Task<Result> RemoveMemberAsync( Guid workspaceId, Guid userId, CancellationToken cancellationToken )
        {
            Result<WorkspaceDetails> result = await ChangeAsync( workspaceId, w => w.RemoveMember( currentUser.Id, userId ), cancellationToken );
            return result.IsSuccess ? Result.Success() : result.Error;
        }

        public async Task<Result<InvitationState>> GetInvitationAsync( Guid workspaceId, CancellationToken cancellationToken )
        {
            Result<Workspace> workspace = await LoadAsHeadAsync( workspaceId, cancellationToken );
            if ( !workspace.IsSuccess )
            {
                return workspace.Error;
            }

            Invitation? active = await FindActiveInvitationAsync( workspaceId, cancellationToken );
            return new InvitationState( active is null ? null : ToResponse( active ) );
        }

        /// <summary>Создаёт новую ссылку и отзывает прежнюю: так глава может "сменить замок", если ссылка утекла.</summary>
        public async Task<Result<InvitationResponse>> CreateInvitationAsync( Guid workspaceId, CancellationToken cancellationToken )
        {
            Result<Workspace> workspace = await LoadAsHeadAsync( workspaceId, cancellationToken );
            if ( !workspace.IsSuccess )
            {
                return workspace.Error;
            }

            DateTimeOffset now = time.GetUtcNow();
            await RevokeActiveInvitationsAsync( workspaceId, now, cancellationToken );

            Invitation invitation = Invitation.Create( workspaceId, currentUser.Id, now );
            db.Invitations.Add( invitation );
            await db.SaveChangesAsync( cancellationToken );

            return ToResponse( invitation );
        }

        public async Task<Result> RevokeInvitationAsync( Guid workspaceId, CancellationToken cancellationToken )
        {
            Result<Workspace> workspace = await LoadAsHeadAsync( workspaceId, cancellationToken );
            if ( !workspace.IsSuccess )
            {
                return workspace.Error;
            }

            await RevokeActiveInvitationsAsync( workspaceId, time.GetUtcNow(), cancellationToken );
            await db.SaveChangesAsync( cancellationToken );
            return Result.Success();
        }

        public async Task<Result<InvitationPreview>> PreviewInvitationAsync( string token, CancellationToken cancellationToken )
        {
            Invitation? invitation = await FindInvitationByTokenAsync( token, cancellationToken );
            if ( invitation is null )
            {
                return WorkspaceErrors.InvitationNotFound;
            }

            Workspace workspace = await db.Workspaces.Include( w => w.Members ).AsNoTracking()
                .FirstAsync( w => w.Id == invitation.WorkspaceId, cancellationToken );

            return new InvitationPreview(
                workspace.Id,
                workspace.Name,
                workspace.Color,
                workspace.Members.Count,
                workspace.FindMember( currentUser.Id ) is not null );
        }

        /// <summary>Вступить по ссылке. Повторное вступление ничего не ломает: человек просто уже в команде.</summary>
        public async Task<Result<WorkspaceSummary>> AcceptInvitationAsync( string token, CancellationToken cancellationToken )
        {
            Invitation? invitation = await FindInvitationByTokenAsync( token, cancellationToken );
            if ( invitation is null )
            {
                return WorkspaceErrors.InvitationNotFound;
            }

            Workspace workspace = await db.Workspaces.Include( w => w.Members )
                .FirstAsync( w => w.Id == invitation.WorkspaceId, cancellationToken );

            if ( workspace.AddMember( currentUser.Id, time.GetUtcNow() ) && !await TrySaveAsync( cancellationToken ) )
            {
                return WorkspaceErrors.ConcurrentChange;
            }

            Member me = workspace.FindMember( currentUser.Id )!;
            return new WorkspaceSummary( workspace.Id, workspace.Name, workspace.Color, me.IsHead, workspace.Members.Count );
        }

        private async Task<Result<WorkspaceDetails>> ChangeAsync( Guid workspaceId, Func<Workspace, Result> change, CancellationToken cancellationToken )
        {
            Workspace? workspace = await LoadMineAsync( workspaceId, cancellationToken );
            if ( workspace is null )
            {
                return WorkspaceErrors.NotFound;
            }

            Result changed = change( workspace );
            if ( !changed.IsSuccess )
            {
                return changed.Error;
            }

            if ( !await TrySaveAsync( cancellationToken ) )
            {
                return WorkspaceErrors.ConcurrentChange;
            }

            return await DetailsAsync( workspace, cancellationToken );
        }

        private async Task<bool> TrySaveAsync( CancellationToken cancellationToken )
        {
            try
            {
                await db.SaveChangesAsync( cancellationToken );
                return true;
            }
            catch ( DbUpdateConcurrencyException )
            {
                return false;
            }
        }

        private async Task<Workspace?> LoadMineAsync( Guid workspaceId, CancellationToken cancellationToken )
        {
            Workspace? workspace = await db.Workspaces.Include( w => w.Members )
                .FirstOrDefaultAsync( w => w.Id == workspaceId, cancellationToken );

            return workspace?.FindMember( currentUser.Id ) is null ? null : workspace;
        }

        private async Task<Result<Workspace>> LoadAsHeadAsync( Guid workspaceId, CancellationToken cancellationToken )
        {
            Workspace? workspace = await LoadMineAsync( workspaceId, cancellationToken );
            if ( workspace is null )
            {
                return WorkspaceErrors.NotFound;
            }

            return workspace.IsHead( currentUser.Id ) ? workspace : WorkspaceErrors.NotHead;
        }

        private Task<Invitation?> FindActiveInvitationAsync( Guid workspaceId, CancellationToken cancellationToken )
        {
            DateTimeOffset now = time.GetUtcNow();
            return db.Invitations
                .Where( i => i.WorkspaceId == workspaceId && i.RevokedAt == null && i.ExpiresAt > now )
                .OrderByDescending( i => i.CreatedAt )
                .FirstOrDefaultAsync( cancellationToken );
        }

        private async Task<Invitation?> FindInvitationByTokenAsync( string token, CancellationToken cancellationToken )
        {
            Invitation? invitation = await db.Invitations.FirstOrDefaultAsync( i => i.Token == token, cancellationToken );
            return invitation is not null && invitation.IsActive( time.GetUtcNow() ) ? invitation : null;
        }

        private async Task RevokeActiveInvitationsAsync( Guid workspaceId, DateTimeOffset now, CancellationToken cancellationToken )
        {
            List<Invitation> active = await db.Invitations
                .Where( i => i.WorkspaceId == workspaceId && i.RevokedAt == null && i.ExpiresAt > now )
                .ToListAsync( cancellationToken );

            foreach ( Invitation invitation in active )
            {
                invitation.Revoke( now );
            }
        }

        private async Task<WorkspaceDetails> DetailsAsync( Workspace workspace, CancellationToken cancellationToken )
        {
            List<Guid> userIds = workspace.Members.Select( m => m.UserId ).ToList();
            Dictionary<Guid, (string DisplayName, string Email)> users = await db.Users
                .Where( u => userIds.Contains( u.Id ) )
                .Select( u => new { u.Id, u.DisplayName, u.Email } )
                .ToDictionaryAsync( u => u.Id, u => (u.DisplayName, u.Email ?? string.Empty), cancellationToken );

            List<MemberResponse> members = workspace.Members
                .OrderByDescending( m => m.IsHead )
                .ThenBy( m => m.JoinedAt )
                .Select( m => new MemberResponse(
                    m.UserId,
                    users.GetValueOrDefault( m.UserId ).DisplayName ?? string.Empty,
                    users.GetValueOrDefault( m.UserId ).Email ?? string.Empty,
                    m.Color,
                    m.IsHead,
                    m.JoinedAt ) )
                .ToList();

            return new WorkspaceDetails( workspace.Id, workspace.Name, workspace.Color, workspace.IsHead( currentUser.Id ), members );
        }

        private static InvitationResponse ToResponse( Invitation invitation )
        {
            return new InvitationResponse( invitation.Token, invitation.ExpiresAt );
        }

        private static Dictionary<string, string[]> ValidateName( string name )
        {
            Dictionary<string, string[]> fields = [];
            if ( name.Length == 0 )
            {
                fields[ "name" ] = [ "Назовите команду." ];
            }
            else if ( name.Length > Workspace.NameMaxLength )
            {
                fields[ "name" ] = [ $"Название должно быть не длиннее {Workspace.NameMaxLength} символов." ];
            }

            return fields;
        }
    }
}
