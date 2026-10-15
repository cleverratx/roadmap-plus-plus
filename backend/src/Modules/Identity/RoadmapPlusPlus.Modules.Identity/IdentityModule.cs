using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RoadmapPlusPlus.BuildingBlocks;
using RoadmapPlusPlus.BuildingBlocks.Persistence;
using RoadmapPlusPlus.Modules.Identity.Application.Auth;
using RoadmapPlusPlus.Modules.Identity.Domain;
using RoadmapPlusPlus.Modules.Identity.Endpoints;
using RoadmapPlusPlus.Modules.Identity.Infrastructure.Persistence;

namespace RoadmapPlusPlus.Modules.Identity
{
    public sealed class IdentityModule : IModule
    {
        public const string SessionCookieName = "roadmap_session";

        public void AddServices( IServiceCollection services, IConfiguration configuration )
        {
            services.AddModuleDbContext<IdentityModuleDbContext>();

            services.AddIdentityCore<User>( ConfigureIdentity )
                .AddUserStore<UserOnlyStore<User, IdentityModuleDbContext, Guid>>()
                .AddSignInManager();

            services.AddAuthentication( IdentityConstants.ApplicationScheme ).AddIdentityCookies();
            services.AddOptions<CookieAuthenticationOptions>( IdentityConstants.ApplicationScheme )
                .Configure<IHostEnvironment>( ConfigureSessionCookie );
            services.AddAuthorization();

            services.AddDataProtection()
                .SetApplicationName( "roadmap-plus-plus" )
                .PersistKeysToDbContext<IdentityModuleDbContext>();

            services.AddOptions<AuthRateLimit>().BindConfiguration( AuthRateLimit.ConfigurationSection );
            services.AddRateLimiter( options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy( AuthRateLimit.PolicyName, AuthRateLimit.Partition );
            } );

            services.AddScoped<AuthService>();
        }

        public void MapEndpoints( IEndpointRouteBuilder endpoints )
        {
            endpoints.MapAuthEndpoints();
        }

        private static void ConfigureIdentity( IdentityOptions options )
        {
            options.Password.RequiredLength = AuthRules.MinPasswordLength;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredUniqueChars = 1;

            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = AuthRules.MaxFailedAccessAttempts;
            options.Lockout.DefaultLockoutTimeSpan = AuthRules.LockoutDuration;

            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = string.Empty;
        }

        /// <summary>
        /// Cookie сессии из ADR-0004. API без входа отвечает 401 и 403, а не редиректом на страницу входа:
        /// куда вести человека, решает SPA.
        /// </summary>
        private static void ConfigureSessionCookie( CookieAuthenticationOptions options, IHostEnvironment environment )
        {
            options.Cookie.Name = SessionCookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = environment.IsProduction() ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = TimeSpan.FromDays( 14 );
            options.SlidingExpiration = true;

            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        }
    }
}
