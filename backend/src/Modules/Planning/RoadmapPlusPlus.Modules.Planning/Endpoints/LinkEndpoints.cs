using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RoadmapPlusPlus.BuildingBlocks.Http;
using RoadmapPlusPlus.Modules.Planning.Application.Links;

namespace RoadmapPlusPlus.Modules.Planning.Endpoints
{
    internal static class LinkEndpoints
    {
        public static void MapLinkEndpoints( this IEndpointRouteBuilder endpoints )
        {
            RouteGroupBuilder links = endpoints.MapGroup( "/items/{itemId:guid}/links" ).WithTags( "Links" );

            links.MapPost( "/", async ( Guid itemId, SaveLinkRequest request, LinkService service, CancellationToken cancellationToken ) =>
                    ( await service.AddAsync( itemId, request, cancellationToken ) )
                        .ToHttpResult( link => TypedResults.Created( $"/api/items/{itemId}/links/{link.Id}", link ) ) )
                .ProducesProblem( StatusCodes.Status400BadRequest )
                .ProducesProblem( StatusCodes.Status404NotFound );

            links.MapPut( "/{linkId:guid}", async ( Guid itemId, Guid linkId, SaveLinkRequest request, LinkService service, CancellationToken cancellationToken ) =>
                    ( await service.UpdateAsync( itemId, linkId, request, cancellationToken ) ).ToHttpResult() )
                .ProducesProblem( StatusCodes.Status400BadRequest )
                .ProducesProblem( StatusCodes.Status404NotFound );

            links.MapDelete( "/{linkId:guid}", async ( Guid itemId, Guid linkId, LinkService service, CancellationToken cancellationToken ) =>
                    ( await service.DeleteAsync( itemId, linkId, cancellationToken ) ).ToHttpResult() )
                .ProducesProblem( StatusCodes.Status404NotFound );
        }
    }
}
