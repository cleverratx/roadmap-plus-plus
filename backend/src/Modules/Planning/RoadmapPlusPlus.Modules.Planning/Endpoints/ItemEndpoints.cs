using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RoadmapPlusPlus.BuildingBlocks.Http;
using RoadmapPlusPlus.Modules.Planning.Application.Items;

namespace RoadmapPlusPlus.Modules.Planning.Endpoints
{
    internal static class ItemEndpoints
    {
        public static void MapItemEndpoints( this IEndpointRouteBuilder endpoints )
        {
            RouteGroupBuilder items = endpoints.MapGroup( "/items" ).WithTags( "Items" );

            items.MapGet( "/", async ( DateOnly from, DateOnly to, Guid? workspaceId, ItemService service, CancellationToken cancellationToken ) =>
                    ( await service.ListAsync( from, to, workspaceId, cancellationToken ) ).ToHttpResult() )
                .ProducesProblem( StatusCodes.Status400BadRequest );

            items.MapGet( "/{itemId:guid}", async ( Guid itemId, ItemService service, CancellationToken cancellationToken ) =>
                    ( await service.GetAsync( itemId, cancellationToken ) ).ToHttpResult() )
                .ProducesProblem( StatusCodes.Status404NotFound );

            items.MapPost( "/", async ( CreateItemRequest request, ItemService service, CancellationToken cancellationToken ) =>
                    ( await service.CreateAsync( request, cancellationToken ) )
                        .ToHttpResult( details => TypedResults.Created( $"/api/items/{details.Item.Id}", details ) ) )
                .ProducesProblem( StatusCodes.Status400BadRequest )
                .ProducesProblem( StatusCodes.Status404NotFound );

            items.MapPut( "/{itemId:guid}", async ( Guid itemId, UpdateItemRequest request, ItemService service, CancellationToken cancellationToken ) =>
                    ( await service.UpdateAsync( itemId, request, cancellationToken ) ).ToHttpResult() )
                .ProducesProblem( StatusCodes.Status400BadRequest )
                .ProducesProblem( StatusCodes.Status404NotFound );

            items.MapPost( "/{itemId:guid}/move", async ( Guid itemId, MoveItemRequest request, ItemService service, CancellationToken cancellationToken ) =>
                    ( await service.MoveAsync( itemId, request, cancellationToken ) ).ToHttpResult() )
                .ProducesProblem( StatusCodes.Status400BadRequest )
                .ProducesProblem( StatusCodes.Status404NotFound );

            items.MapGet( "/{itemId:guid}/move-targets", async ( Guid itemId, ItemService service, CancellationToken cancellationToken ) =>
                    ( await service.MoveTargetsAsync( itemId, cancellationToken ) ).ToHttpResult() )
                .ProducesProblem( StatusCodes.Status404NotFound );

            items.MapDelete( "/{itemId:guid}", async ( Guid itemId, ItemService service, CancellationToken cancellationToken ) =>
                    ( await service.DeleteAsync( itemId, cancellationToken ) ).ToHttpResult() )
                .ProducesProblem( StatusCodes.Status404NotFound );
        }
    }
}
