using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using RoadmapPlusPlus.BuildingBlocks.Persistence;

namespace RoadmapPlusPlus.IntegrationTests
{
    /// <summary>
    /// Общие настройки API в тестах. Лимит частоты входа поднят: все тесты ходят с одного адреса и иначе упирались бы в него друг из-за друга.
    /// </summary>
    internal static class TestApi
    {
        public const string AuthRateLimitSetting = "Identity:AuthRateLimit:PermitLimit";

        public static void Configure( IWebHostBuilder builder, string connectionString )
        {
            builder.UseEnvironment( "Testing" );
            builder.UseSetting( $"ConnectionStrings:{PersistenceServiceCollectionExtensions.ConnectionStringName}", connectionString );
            builder.UseSetting( AuthRateLimitSetting, "1000" );
        }
    }

    /// <summary>
    /// API поверх отдельной базы, когда тесту нужна своя, например чтобы её остановить.
    /// </summary>
    public sealed class ApiFactoryForDatabase( string connectionString ) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost( IWebHostBuilder builder )
        {
            TestApi.Configure( builder, connectionString );
        }
    }
}
