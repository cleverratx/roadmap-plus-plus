using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RoadmapPlusPlus.BuildingBlocks.Http;
using RoadmapPlusPlus.Modules.Identity.Application.Workspaces;

namespace RoadmapPlusPlus.Modules.Identity.Endpoints
{
    internal static class WorkspaceEndpoints
    {
        public static void MapWorkspaceEndpoints( this IEndpointRouteBuilder endpoints )
        {
            RouteGroupBuilder workspaces = endpoints.MapGroup( "/workspaces" ).WithTags( "Workspaces" );

            workspaces.MapGet( "/", async ( WorkspaceService service, CancellationToken cancellationToken ) =>
                TypedResults.Ok( await service.ListMineAsync( cancellationToken ) ) );

            workspaces.MapPost( "/", async ( CreateWorkspaceRequest request, WorkspaceService service, CancellationToken cancellationToken ) =>
                    ( await service.CreateAsync( request, cancellationToken ) )
                        .ToHttpResult( details => TypedResults.Created( $"/api/workspaces/{details.Id}", details ) ) )
                .ProducesProblem( StatusCodes.Status400BadRequest );

            workspaces.MapGet( "/{workspaceId:guid}", async ( Guid workspaceId, WorkspaceService service, CancellationToken cancellationToken ) =>
                    ( await service.GetAsync( workspaceId, cancellationToken ) ).ToHttpResult() )
                .ProducesProblem( StatusCodes.Status404NotFound );

            workspaces.MapPatch( "/{workspaceId:guid}", async ( Guid workspaceId, UpdateWorkspaceRequest request, WorkspaceService service, CancellationToken cancellationToken ) =>
                    ( await service.UpdateAsync( workspaceId, request, cancellationToken ) ).ToHttpResult() )
                .ProducesProblem( StatusCodes.Status400BadRequest )
                .ProducesProblem( StatusCodes.Status403Forbidden )
                .ProducesProblem( StatusCodes.Status404NotFound )
                .ProducesProblem( StatusCodes.Status409Conflict );

            workspaces.MapPatch( "/{workspaceId:guid}/members/{userId:guid}", async ( Guid workspaceId, Guid userId, SetHeadRequest request, WorkspaceService service, CancellationToken cancellationToken ) =>
                    ( await service.SetHeadAsync( workspaceId, userId, request, cancellationToken ) ).ToHttpResult() )
                .ProducesProblem( StatusCodes.Status403Forbidden )
                .ProducesProblem( StatusCodes.Status404NotFound )
                .ProducesProblem( StatusCodes.Status409Conflict );

            workspaces.MapDelete( "/{workspaceId:guid}/members/{userId:guid}", async ( Guid workspaceId, Guid userId, WorkspaceService service, CancellationToken cancellationToken ) =>
                    ( await service.RemoveMemberAsync( workspaceId, userId, cancellationToken ) ).ToHttpResult() )
                .ProducesProblem( StatusCodes.Status403Forbidden )
                .ProducesProblem( StatusCodes.Status404NotFound )
                .ProducesProblem( StatusCodes.Status409Conflict );

            workspaces.MapGet( "/{workspaceId:guid}/invitation", async ( Guid workspaceId, WorkspaceService service, CancellationToken cancellationToken ) =>
                    ( await service.GetInvitationAsync( workspaceId, cancellationToken ) ).ToHttpResult() )
                .ProducesProblem( StatusCodes.Status403Forbidden )
                .ProducesProblem( StatusCodes.Status404NotFound );

            workspaces.MapPost( "/{workspaceId:guid}/invitation", async ( Guid workspaceId, WorkspaceService service, CancellationToken cancellationToken ) =>
                    ( await service.CreateInvitationAsync( workspaceId, cancellationToken ) ).ToHttpResult() )
                .ProducesProblem( StatusCodes.Status403Forbidden )
                .ProducesProblem( StatusCodes.Status404NotFound );

            workspaces.MapDelete( "/{workspaceId:guid}/invitation", async ( Guid workspaceId, WorkspaceService service, CancellationToken cancellationToken ) =>
                    ( await service.RevokeInvitationAsync( workspaceId, cancellationToken ) ).ToHttpResult() )
                .ProducesProblem( StatusCodes.Status403Forbidden )
                .ProducesProblem( StatusCodes.Status404NotFound );

            RouteGroupBuilder invitations = endpoints.MapGroup( "/invitations" ).WithTags( "Workspaces" );

            invitations.MapGet( "/{token}", async ( string token, WorkspaceService service, CancellationToken cancellationToken ) =>
                    ( await service.PreviewInvitationAsync( token, cancellationToken ) ).ToHttpResult() )
                .ProducesProblem( StatusCodes.Status404NotFound );

            invitations.MapPost( "/{token}/accept", async ( string token, WorkspaceService service, CancellationToken cancellationToken ) =>
                    ( await service.AcceptInvitationAsync( token, cancellationToken ) ).ToHttpResult() )
                .ProducesProblem( StatusCodes.Status404NotFound )
                .ProducesProblem( StatusCodes.Status409Conflict );
        }
    }
}
