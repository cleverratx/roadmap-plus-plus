using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RoadmapPlusPlus.BuildingBlocks;
using RoadmapPlusPlus.Modules.Planning.Application.Links;
using RoadmapPlusPlus.Modules.Planning.Domain.Items;
using RoadmapPlusPlus.Modules.Planning.Infrastructure.Persistence;

namespace RoadmapPlusPlus.Modules.Planning.Application.Items
{
    internal sealed class ItemService( PlanningDbContext db, ItemVisibility visibility, TimeProvider time )
    {
        /// <summary>Сколько дней можно запросить за раз: таймлайн просит видимый диапазон, а год с запасом покрывает самый дальний зум.</summary>
        public const int MaxRangeDays = 3 * 366;

        /// <summary>
        /// Узлы с датами, которые пересекаются с диапазоном <paramref name="from"/>..<paramref name="to"/>, из всех команд пользователя.
        /// Узлы без дат на таймлайн не попадают: их показывает панель родителя.
        /// </summary>
        public async Task<Result<ItemResponse[]>> ListAsync( DateOnly from, DateOnly to, Guid? workspaceId, CancellationToken cancellationToken )
        {
            if ( to < from || to.DayNumber - from.DayNumber > MaxRangeDays )
            {
                return PlanningErrors.InvalidRequest( new Dictionary<string, string[]>
                {
                    [ "to" ] = [ $"Диапазон должен идти вперёд и быть не длиннее {MaxRangeDays} дней." ],
                } );
            }

            IQueryable<Item> items = await visibility.VisibleItemsAsync( cancellationToken );
            if ( workspaceId is { } only )
            {
                items = items.Where( i => i.WorkspaceId == only );
            }

            ItemResponse[] found = await items
                .AsNoTracking()
                .Where( i => i.StartDate <= to && i.EndDate >= from )
                .OrderBy( i => i.StartDate ).ThenBy( i => i.Position )
                .Select( i => new ItemResponse( i.Id, i.WorkspaceId, i.ParentId, i.Kind, i.Title, i.StartDate, i.EndDate, i.Position ) )
                .ToArrayAsync( cancellationToken );

            return found;
        }

        public async Task<Result<ItemDetails>> GetAsync( Guid itemId, CancellationToken cancellationToken )
        {
            Item? item = await FindVisibleAsync( itemId, cancellationToken );
            return item is null ? PlanningErrors.ItemNotFound : await DetailsAsync( item, cancellationToken );
        }

        public async Task<Result<ItemDetails>> CreateAsync( CreateItemRequest request, CancellationToken cancellationToken )
        {
            Result<ItemContent> content = ItemContent.Create( request.Title, request.StartDate, request.EndDate, request.DescriptionMd );
            if ( !content.IsSuccess )
            {
                return content.Error;
            }

            DateTimeOffset now = time.GetUtcNow();
            Result<Item> created;

            if ( request.ParentId is { } parentId )
            {
                Item? parent = await FindVisibleAsync( parentId, cancellationToken );
                if ( parent is null )
                {
                    return PlanningErrors.ItemNotFound;
                }

                created = Item.CreateChild( parent, request.Kind, content.Value, await NextPositionAsync( parent.WorkspaceId, parent.Id, cancellationToken ), now );
            }
            else
            {
                if ( request.WorkspaceId is not { } workspaceId )
                {
                    return PlanningErrors.InvalidRequest( new Dictionary<string, string[]>
                    {
                        [ "workspaceId" ] = [ "Укажите команду для узла верхнего уровня." ],
                    } );
                }

                if ( !await visibility.CanCreateInWorkspaceAsync( workspaceId, cancellationToken ) )
                {
                    return PlanningErrors.WorkspaceNotFound;
                }

                created = Item.CreateRoot( workspaceId, request.Kind, content.Value, await NextPositionAsync( workspaceId, null, cancellationToken ), now );
            }

            if ( !created.IsSuccess )
            {
                return created.Error;
            }

            db.Items.Add( created.Value );
            await db.SaveChangesAsync( cancellationToken );
            return await DetailsAsync( created.Value, cancellationToken );
        }

        public async Task<Result<ItemDetails>> UpdateAsync( Guid itemId, UpdateItemRequest request, CancellationToken cancellationToken )
        {
            Item? item = await FindVisibleAsync( itemId, cancellationToken );
            if ( item is null )
            {
                return PlanningErrors.ItemNotFound;
            }

            Result<ItemContent> content = ItemContent.Create( request.Title, request.StartDate, request.EndDate, request.DescriptionMd );
            if ( !content.IsSuccess )
            {
                return content.Error;
            }

            item.Update( content.Value, time.GetUtcNow() );
            await db.SaveChangesAsync( cancellationToken );
            return await DetailsAsync( item, cancellationToken );
        }

        /// <summary>Удаляет узел вместе со всем поддеревом: каскад делает сама база по <c>parent_id</c>.</summary>
        public async Task<Result> DeleteAsync( Guid itemId, CancellationToken cancellationToken )
        {
            Item? item = await FindVisibleAsync( itemId, cancellationToken );
            if ( item is null )
            {
                return PlanningErrors.ItemNotFound;
            }

            db.Items.Remove( item );
            await db.SaveChangesAsync( cancellationToken );
            return Result.Success();
        }

        /// <summary>
        /// Переносит узел к другому родителю той же команды или меняет его место среди соседей. Переносы внутри команды идут
        /// по очереди под advisory-блокировкой: иначе два встречных переноса ("A в B" и "B в A") прошли бы проверку на кольцо
        /// каждый по отдельности и вместе замкнули бы дерево.
        /// </summary>
        public async Task<Result<ItemDetails>> MoveAsync( Guid itemId, MoveItemRequest request, CancellationToken cancellationToken )
        {
            Item? item = await FindVisibleAsync( itemId, cancellationToken );
            if ( item is null )
            {
                return PlanningErrors.ItemNotFound;
            }

            await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync( cancellationToken );
            await db.Database.ExecuteSqlAsync( $"SELECT pg_advisory_xact_lock(hashtextextended({item.WorkspaceId.ToString()}, 0))", cancellationToken );
            await db.Entry( item ).ReloadAsync( cancellationToken );
            if ( db.Entry( item ).State == EntityState.Detached )
            {
                return PlanningErrors.ItemNotFound;
            }

            Item? parent = null;
            List<Guid> parentPath = [];
            if ( request.ParentId is { } parentId )
            {
                parent = await FindVisibleAsync( parentId, cancellationToken );
                if ( parent is null )
                {
                    return PlanningErrors.ItemNotFound;
                }

                parentPath = [ .. ( await PathAsync( parent, cancellationToken ) ).Select( p => p.Id ), parent.Id ];
            }

            Guid? oldParentId = item.ParentId;
            Result moved = item.MoveTo( parent, parentPath, time.GetUtcNow() );
            if ( !moved.IsSuccess )
            {
                return moved.Error;
            }

            List<Item> siblings = await SiblingsAsync( item.WorkspaceId, item.ParentId, item.Id, cancellationToken );
            siblings.Insert( Math.Clamp( request.Position ?? siblings.Count, 0, siblings.Count ), item );
            Renumber( siblings );

            if ( oldParentId != item.ParentId )
            {
                Renumber( await SiblingsAsync( item.WorkspaceId, oldParentId, item.Id, cancellationToken ) );
            }

            await db.SaveChangesAsync( cancellationToken );
            await transaction.CommitAsync( cancellationToken );
            return await DetailsAsync( item, cancellationToken );
        }

        /// <summary>
        /// Куда можно перенести узел: узлы его команды, которые могут его вместить по правилам вложенности, кроме него самого
        /// и его потомков. Идут в порядке дерева, у каждого путь от корня, чтобы в списке было понятно, где он лежит.
        /// </summary>
        public async Task<Result<MoveTargets>> MoveTargetsAsync( Guid itemId, CancellationToken cancellationToken )
        {
            Item? item = await FindVisibleAsync( itemId, cancellationToken );
            if ( item is null )
            {
                return PlanningErrors.ItemNotFound;
            }

            IQueryable<Item> visible = await visibility.VisibleItemsAsync( cancellationToken );
            List<ItemResponse> team = await visible
                .AsNoTracking()
                .Where( i => i.WorkspaceId == item.WorkspaceId )
                .OrderBy( i => i.Position )
                .Select( i => new ItemResponse( i.Id, i.WorkspaceId, i.ParentId, i.Kind, i.Title, i.StartDate, i.EndDate, i.Position ) )
                .ToListAsync( cancellationToken );

            ILookup<Guid?, ItemResponse> childrenOf = team.ToLookup( i => i.ParentId );
            List<MoveTarget> parents = [];
            Stack<(ItemResponse Node, string[] Path)> pending = new( childrenOf[ null ].Reverse().Select( root => (root, Array.Empty<string>()) ) );

            while ( pending.TryPop( out (ItemResponse Node, string[] Path) next ) )
            {
                if ( next.Node.Id == item.Id )
                {
                    continue;
                }

                if ( ItemNesting.CanContain( next.Node.Kind, item.Kind ) )
                {
                    parents.Add( new MoveTarget( next.Node, next.Path ) );
                }

                string[] childPath = [ .. next.Path, next.Node.Title ];
                foreach ( ItemResponse child in childrenOf[ next.Node.Id ].Reverse() )
                {
                    pending.Push( (child, childPath) );
                }
            }

            return new MoveTargets( ItemNesting.CanBeRoot( item.Kind ), item.ParentId, parents );
        }

        private Task<List<Item>> SiblingsAsync( Guid workspaceId, Guid? parentId, Guid exceptId, CancellationToken cancellationToken )
        {
            return db.Items
                .Where( i => i.WorkspaceId == workspaceId && i.ParentId == parentId && i.Id != exceptId )
                .OrderBy( i => i.Position )
                .ToListAsync( cancellationToken );
        }

        private static void Renumber( List<Item> siblings )
        {
            for ( int position = 0; position < siblings.Count; position++ )
            {
                siblings[ position ].PlaceAt( position );
            }
        }

        private async Task<Item?> FindVisibleAsync( Guid itemId, CancellationToken cancellationToken )
        {
            IQueryable<Item> items = await visibility.VisibleItemsAsync( cancellationToken );
            return await items.FirstOrDefaultAsync( i => i.Id == itemId, cancellationToken );
        }

        private async Task<int> NextPositionAsync( Guid workspaceId, Guid? parentId, CancellationToken cancellationToken )
        {
            int? last = await db.Items
                .Where( i => i.WorkspaceId == workspaceId && i.ParentId == parentId )
                .MaxAsync( i => ( int? )i.Position, cancellationToken );

            return ( last ?? -1 ) + 1;
        }

        private async Task<ItemDetails> DetailsAsync( Item item, CancellationToken cancellationToken )
        {
            List<ItemResponse> children = await db.Items
                .AsNoTracking()
                .Where( i => i.ParentId == item.Id )
                .OrderBy( i => i.Position )
                .Select( i => new ItemResponse( i.Id, i.WorkspaceId, i.ParentId, i.Kind, i.Title, i.StartDate, i.EndDate, i.Position ) )
                .ToListAsync( cancellationToken );

            List<LinkResponse> links = await db.Links
                .AsNoTracking()
                .Where( l => l.ItemId == item.Id )
                .OrderBy( l => l.Position )
                .Select( l => new LinkResponse( l.Id, l.Url, l.Title, l.Kind, l.Position ) )
                .ToListAsync( cancellationToken );

            return new ItemDetails( ItemResponse.From( item ), item.DescriptionMd, await PathAsync( item, cancellationToken ), children, links );
        }

        /// <summary>
        /// Предки узла от корня до родителя одним рекурсивным запросом. Все предки в той же команде, что и узел,
        /// поэтому отдельно проверять их видимость не нужно.
        /// </summary>
        private async Task<IReadOnlyList<ItemResponse>> PathAsync( Item item, CancellationToken cancellationToken )
        {
            if ( item.ParentId is null )
            {
                return [];
            }

            List<Item> ancestors = await db.Items
                .FromSql( $"""
                    WITH RECURSIVE ancestors AS (
                        SELECT * FROM planning.items WHERE id = {item.ParentId}
                        UNION ALL
                        SELECT parent.* FROM planning.items parent JOIN ancestors child ON parent.id = child.parent_id
                    )
                    SELECT * FROM ancestors
                    """ )
                .AsNoTracking()
                .ToListAsync( cancellationToken );

            Dictionary<Guid, Item> byId = ancestors.ToDictionary( a => a.Id );
            List<ItemResponse> path = [];
            for ( Guid? id = item.ParentId; id is { } current && byId.TryGetValue( current, out Item? ancestor ); id = ancestor.ParentId )
            {
                path.Add( ItemResponse.From( ancestor ) );
            }

            path.Reverse();
            return path;
        }
    }
}
