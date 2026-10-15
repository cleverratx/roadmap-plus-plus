using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using RoadmapPlusPlus.IntegrationTests;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture( typeof( PostgresApiFactory ) )]

namespace RoadmapPlusPlus.IntegrationTests
{
    /// <summary>
    /// API целиком, в памяти процесса тестов, поверх настоящего Postgres в Docker-контейнере.
    /// Один на все тесты сборки: контейнер поднимается один раз, миграции модулей API применяет при старте, как в проде.
    /// </summary>
    public sealed class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        public const string PostgresImage = "postgres:18-alpine";

        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder( PostgresImage ).Build();

        public string ConnectionString => _postgres.GetConnectionString();

        public async ValueTask InitializeAsync()
        {
            await _postgres.StartAsync();
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            await _postgres.DisposeAsync();
        }

        protected override void ConfigureWebHost( IWebHostBuilder builder )
        {
            TestApi.Configure( builder, ConnectionString );
        }
    }
}
