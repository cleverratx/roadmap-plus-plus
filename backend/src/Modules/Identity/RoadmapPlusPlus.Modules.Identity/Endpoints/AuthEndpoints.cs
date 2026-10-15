using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using RoadmapPlusPlus.BuildingBlocks.Http;
using RoadmapPlusPlus.Modules.Identity.Application.Auth;

namespace RoadmapPlusPlus.Modules.Identity.Endpoints
{
    internal static class AuthEndpoints
    {
        public static void MapAuthEndpoints( this IEndpointRouteBuilder endpoints )
        {
            RouteGroupBuilder auth = endpoints.MapGroup( "/auth" ).WithTags( "Auth" ).AllowAnonymous();

            auth.MapPost( "/register", async ( RegisterRequest request, AuthService service ) =>
                    ( await service.RegisterAsync( request ) ).ToHttpResult() )
                .RequireRateLimiting( AuthRateLimit.PolicyName )
                .ProducesProblem( StatusCodes.Status400BadRequest )
                .ProducesProblem( StatusCodes.Status409Conflict );

            auth.MapPost( "/login", async ( LoginRequest request, AuthService service ) =>
                    ( await service.LoginAsync( request ) ).ToHttpResult() )
                .RequireRateLimiting( AuthRateLimit.PolicyName )
                .ProducesProblem( StatusCodes.Status400BadRequest )
                .ProducesProblem( StatusCodes.Status403Forbidden );

            auth.MapPost( "/logout", async ( AuthService service ) =>
            {
                await service.LogoutAsync();
                return TypedResults.NoContent();
            } );

            endpoints.MapGet( "/me", async Task<Results<Ok<MeResponse>, UnauthorizedHttpResult>> ( ClaimsPrincipal principal, AuthService service ) =>
                    await service.FindMeAsync( principal ) is { } me ? TypedResults.Ok( me ) : TypedResults.Unauthorized() )
                .WithTags( "Auth" )
                .ProducesProblem( StatusCodes.Status401Unauthorized );
        }
    }
}
