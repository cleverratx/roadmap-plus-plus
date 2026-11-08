using Microsoft.EntityFrameworkCore;
using RoadmapPlusPlus.BuildingBlocks;
using RoadmapPlusPlus.Modules.Planning.Application.Items;
using RoadmapPlusPlus.Modules.Planning.Domain.Items;
using RoadmapPlusPlus.Modules.Planning.Domain.Links;
using RoadmapPlusPlus.Modules.Planning.Infrastructure.Persistence;

namespace RoadmapPlusPlus.Modules.Planning.Application.Links
{
    /// <summary>
    /// Ссылки узла. Отдельной видимости у ссылок нет: кто видит узел, тот видит и правит его ссылки,
    /// поэтому каждая операция начинается с поиска узла через <see cref="ItemVisibility"/>.
    /// </summary>
    internal sealed class LinkService( PlanningDbContext db, ItemVisibility visibility, TimeProvider time )
    {
        public async Task<Result<LinkResponse>> AddAsync( Guid itemId, SaveLinkRequest request, CancellationToken cancellationToken )
        {
            if ( !await IsVisibleAsync( itemId, cancellationToken ) )
            {
                return PlanningErrors.ItemNotFound;
            }

            List<int> positions = await db.Links
                .Where( l => l.ItemId == itemId )
                .Select( l => l.Position )
                .ToListAsync( cancellationToken );

            if ( positions.Count >= Link.MaxPerItem )
            {
                return PlanningErrors.TooManyLinks( Link.MaxPerItem );
            }

            Result<Link> created = Link.Create( itemId, request.Url, request.Title, positions.Count == 0 ? 0 : positions.Max() + 1, time.GetUtcNow() );
            if ( !created.IsSuccess )
            {
                return created.Error;
            }

            db.Links.Add( created.Value );
            await db.SaveChangesAsync( cancellationToken );
            return LinkResponse.From( created.Value );
        }

        public async Task<Result<LinkResponse>> UpdateAsync( Guid itemId, Guid linkId, SaveLinkRequest request, CancellationToken cancellationToken )
        {
            Result<Link> found = await FindAsync( itemId, linkId, cancellationToken );
            if ( !found.IsSuccess )
            {
                return found.Error;
            }

            Result updated = found.Value.Update( request.Url, request.Title );
            if ( !updated.IsSuccess )
            {
                return updated.Error;
            }

            await db.SaveChangesAsync( cancellationToken );
            return LinkResponse.From( found.Value );
        }

        public async Task<Result> DeleteAsync( Guid itemId, Guid linkId, CancellationToken cancellationToken )
        {
            Result<Link> found = await FindAsync( itemId, linkId, cancellationToken );
            if ( !found.IsSuccess )
            {
                return found.Error;
            }

            db.Links.Remove( found.Value );
            await db.SaveChangesAsync( cancellationToken );
            return Result.Success();
        }

        private async Task<Result<Link>> FindAsync( Guid itemId, Guid linkId, CancellationToken cancellationToken )
        {
            if ( !await IsVisibleAsync( itemId, cancellationToken ) )
            {
                return PlanningErrors.ItemNotFound;
            }

            Link? link = await db.Links.FirstOrDefaultAsync( l => l.Id == linkId && l.ItemId == itemId, cancellationToken );
            return link is null ? PlanningErrors.LinkNotFound : link;
        }

        private async Task<bool> IsVisibleAsync( Guid itemId, CancellationToken cancellationToken )
        {
            IQueryable<Item> items = await visibility.VisibleItemsAsync( cancellationToken );
            return await items.AnyAsync( i => i.Id == itemId, cancellationToken );
        }
    }
}
