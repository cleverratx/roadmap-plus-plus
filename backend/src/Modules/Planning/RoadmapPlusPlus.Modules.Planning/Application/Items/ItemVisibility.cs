using Microsoft.EntityFrameworkCore;
using RoadmapPlusPlus.Modules.Identity.Contracts;
using RoadmapPlusPlus.Modules.Planning.Domain.Items;
using RoadmapPlusPlus.Modules.Planning.Infrastructure.Persistence;

namespace RoadmapPlusPlus.Modules.Planning.Application.Items
{
    /// <summary>
    /// Единственное место, где решается, какие узлы видны пользователю (ADR-0003). Все выборки дерева начинаются отсюда.
    /// <list type="bullet">
    /// <item>Глава видит всю свою команду. Участник команды с выключенными ролями тоже видит всё.</item>
    /// <item>При включённых ролях обычный участник видит узлы, открытые хотя бы одной его ролью, вместе со всем их поддеревом.</item>
    /// <item>Предки таких узлов, которые сами ему не открыты, видны рамкой: только название и даты, без содержимого.</item>
    /// </list>
    /// Доступ считается один раз на запрос: сервис живёт в рамках запроса и запоминает результат.
    /// </summary>
    internal sealed class ItemVisibility( PlanningDbContext db, ICurrentUser currentUser, IWorkspaceMemberships memberships )
    {
        private ItemScope? _scope;

        public async Task<ItemScope> ScopeAsync( CancellationToken cancellationToken )
        {
            return _scope ??= await LoadAsync( cancellationToken );
        }

        /// <summary>Узлы, с которыми можно работать: смотреть целиком, править, создавать внутри них.</summary>
        public async Task<IQueryable<Item>> VisibleItemsAsync( CancellationToken cancellationToken )
        {
            ItemScope scope = await ScopeAsync( cancellationToken );
            return scope.Visible( db.Items );
        }

        /// <summary>Видимые узлы вместе с рамками: так их показывают таймлайн и путь в панели.</summary>
        public async Task<IQueryable<Item>> SeenItemsAsync( CancellationToken cancellationToken )
        {
            ItemScope scope = await ScopeAsync( cancellationToken );
            return scope.Seen( db.Items );
        }

        private async Task<ItemScope> LoadAsync( CancellationToken cancellationToken )
        {
            IReadOnlyList<WorkspaceMembership> mine = await memberships.ListAsync( currentUser.Id, cancellationToken );
            List<Guid> open = mine.Where( m => m.IsHead || !m.RolesEnabled ).Select( m => m.WorkspaceId ).ToList();
            List<WorkspaceMembership> restricted = mine.Where( m => !m.IsHead && m.RolesEnabled ).ToList();

            Guid[] roleIds = restricted.SelectMany( m => m.RoleIds ).Distinct().ToArray();
            if ( roleIds.Length == 0 )
            {
                return new ItemScope( mine, open, [], [] );
            }

            Guid[] restrictedWorkspaces = restricted.Select( m => m.WorkspaceId ).ToArray();
            List<Guid> granted = await db.Database
                .SqlQuery<Guid>( $"""
                    WITH RECURSIVE granted AS (
                        SELECT i.id FROM planning.items i
                        WHERE i.workspace_id = ANY({restrictedWorkspaces})
                          AND EXISTS (SELECT 1 FROM planning.item_roles r WHERE r.item_id = i.id AND r.role_id = ANY({roleIds}))
                        UNION
                        SELECT child.id FROM planning.items child JOIN granted parent ON child.parent_id = parent.id
                    )
                    SELECT id AS "Value" FROM granted
                    """ )
                .ToListAsync( cancellationToken );

            if ( granted.Count == 0 )
            {
                return new ItemScope( mine, open, [], [] );
            }

            Guid[] grantedIds = [ .. granted ];
            List<Guid> ancestors = await db.Database
                .SqlQuery<Guid>( $"""
                    WITH RECURSIVE up AS (
                        SELECT i.parent_id AS id FROM planning.items i WHERE i.id = ANY({grantedIds}) AND i.parent_id IS NOT NULL
                        UNION
                        SELECT i.parent_id FROM planning.items i JOIN up ON i.id = up.id WHERE i.parent_id IS NOT NULL
                    )
                    SELECT id AS "Value" FROM up
                    """ )
                .ToListAsync( cancellationToken );

            HashSet<Guid> grantedSet = [ .. granted ];
            return new ItemScope( mine, open, granted, ancestors.Where( id => !grantedSet.Contains( id ) ).ToList() );
        }
    }

    /// <summary>
    /// Что видно пользователю в одном запросе. <see cref="OpenWorkspaces"/> это команды, где ему видно всё,
    /// <see cref="GrantedIds"/> это узлы, открытые его ролям в остальных командах, а <see cref="FrameIds"/> их предки-рамки.
    /// </summary>
    internal sealed record ItemScope(
        IReadOnlyList<WorkspaceMembership> Memberships,
        List<Guid> OpenWorkspaces,
        List<Guid> GrantedIds,
        List<Guid> FrameIds )
    {
        public IQueryable<Item> Visible( IQueryable<Item> items )
        {
            List<Guid> open = OpenWorkspaces;
            List<Guid> granted = GrantedIds;
            return items.Where( i => open.Contains( i.WorkspaceId ) || granted.Contains( i.Id ) );
        }

        public IQueryable<Item> Seen( IQueryable<Item> items )
        {
            List<Guid> open = OpenWorkspaces;
            List<Guid> granted = GrantedIds;
            List<Guid> frames = FrameIds;
            return items.Where( i => open.Contains( i.WorkspaceId ) || granted.Contains( i.Id ) || frames.Contains( i.Id ) );
        }

        public bool IsFrame( Guid itemId )
        {
            return FrameIds.Contains( itemId );
        }

        public WorkspaceMembership? In( Guid workspaceId )
        {
            return Memberships.FirstOrDefault( m => m.WorkspaceId == workspaceId );
        }

        public bool IsHead( Guid workspaceId )
        {
            return In( workspaceId )?.IsHead == true;
        }

        /// <summary>Узлы верхнего уровня при включённых ролях создают только главы: иначе новый узел не был бы открыт никому.</summary>
        public bool CanCreateRoot( Guid workspaceId )
        {
            return In( workspaceId ) is { } membership && ( membership.IsHead || !membership.RolesEnabled );
        }
    }
}
