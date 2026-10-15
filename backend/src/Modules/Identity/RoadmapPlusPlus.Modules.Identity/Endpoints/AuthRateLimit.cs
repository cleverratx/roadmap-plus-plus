using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace RoadmapPlusPlus.Modules.Identity.Endpoints
{
    /// <summary>
    /// Ограничение частоты регистрации и входа с одного IP: защита от перебора паролей по многим аккаунтам сразу,
    /// которую не ловит блокировка одного аккаунта. Настраивается секцией <c>Identity:AuthRateLimit</c>.
    /// </summary>
    internal sealed class AuthRateLimit
    {
        public const string PolicyName = "auth";

        public const string ConfigurationSection = "Identity:AuthRateLimit";

        public int PermitLimit { get; set; } = 10;

        public TimeSpan Window { get; set; } = TimeSpan.FromMinutes( 1 );

        public static RateLimitPartition<string> Partition( HttpContext context )
        {
            AuthRateLimit options = context.RequestServices.GetRequiredService<IOptions<AuthRateLimit>>().Value;
            string client = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter( client, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = options.PermitLimit,
                Window = options.Window,
                QueueLimit = 0,
            } );
        }
    }
}
