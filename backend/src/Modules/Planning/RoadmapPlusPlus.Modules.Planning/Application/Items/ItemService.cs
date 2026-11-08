using Microsoft.EntityFrameworkCore;
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
