using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using RoadmapPlusPlus.BuildingBlocks;
using RoadmapPlusPlus.Modules.Identity.Domain;

namespace RoadmapPlusPlus.Modules.Identity.Application.Auth
{
    internal sealed class AuthService( UserManager<User> users, SignInManager<User> signIn )
    {
        public async Task<Result<MeResponse>> RegisterAsync( RegisterRequest request )
        {
            Dictionary<string, string[]> fields = AuthRules.Validate( request );
            if ( fields.Count > 0 )
            {
                return AuthErrors.InvalidRequest( fields );
            }

            string email = request.Email.Trim();
            if ( await users.FindByEmailAsync( email ) is not null )
            {
                return AuthErrors.EmailTaken;
            }

            User user = new( email, request.DisplayName.Trim() );
            IdentityResult created = await users.CreateAsync( user, request.Password );
            if ( !created.Succeeded )
            {
                return ToError( created );
            }

            await signIn.SignInAsync( user, isPersistent: true );
            return MeResponse.From( user );
        }

        public async Task<Result<MeResponse>> LoginAsync( LoginRequest request )
        {
            User? user = await users.FindByEmailAsync( request.Email?.Trim() ?? string.Empty );
            if ( user is null || string.IsNullOrEmpty( request.Password ) )
            {
                return AuthErrors.InvalidCredentials;
            }

            SignInResult result = await signIn.PasswordSignInAsync( user, request.Password, isPersistent: true, lockoutOnFailure: true );
            if ( result.Succeeded )
            {
                return MeResponse.From( user );
            }

            return result.IsLockedOut ? AuthErrors.LockedOut : AuthErrors.InvalidCredentials;
        }

        public Task LogoutAsync()
        {
            return signIn.SignOutAsync();
        }

        public async Task<MeResponse?> FindMeAsync( ClaimsPrincipal principal )
        {
            User? user = await users.GetUserAsync( principal );
            return user is null ? null : MeResponse.From( user );
        }

        private static Error ToError( IdentityResult result )
        {
            if ( result.Errors.Any( e => e.Code is "DuplicateEmail" or "DuplicateUserName" ) )
            {
                return AuthErrors.EmailTaken;
            }

            Dictionary<string, string[]> fields = result.Errors
                .GroupBy( e => e.Code.StartsWith( "Password", StringComparison.Ordinal ) ? "password" : "email" )
                .ToDictionary( g => g.Key, g => g.Select( e => e.Description ).ToArray() );

            return AuthErrors.InvalidRequest( fields );
        }
    }
}
