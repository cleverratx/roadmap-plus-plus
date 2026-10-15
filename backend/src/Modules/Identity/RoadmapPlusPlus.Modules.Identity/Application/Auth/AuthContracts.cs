using RoadmapPlusPlus.Modules.Identity.Domain;

namespace RoadmapPlusPlus.Modules.Identity.Application.Auth
{
    internal sealed record RegisterRequest( string Email, string Password, string DisplayName );

    internal sealed record LoginRequest( string Email, string Password );

    internal sealed record MeResponse( Guid Id, string Email, string DisplayName )
    {
        public static MeResponse From( User user )
        {
            return new MeResponse( user.Id, user.Email!, user.DisplayName );
        }
    }
}
