using System.Net;
using Testcontainers.PostgreSql;

namespace RoadmapPlusPlus.IntegrationTests
{
    public sealed class HealthTests( PostgresApiFactory api )
    {
        [Fact]
        public async Task HealthyWithDatabase()
        {
            using HttpClient client = api.CreateClient();

            using HttpResponseMessage response = await client.GetAsync( "/api/health", TestContext.Current.CancellationToken );

            Assert.Equal( HttpStatusCode.OK, response.StatusCode );
            Assert.Equal( "Healthy", await response.Content.ReadAsStringAsync( TestContext.Current.CancellationToken ) );
        }

        [Fact]
        public async Task UnhealthyWhenDatabaseGoesDown()
        {
            CancellationToken cancellation = TestContext.Current.CancellationToken;
            await using PostgreSqlContainer database = new PostgreSqlBuilder( PostgresApiFactory.PostgresImage ).Build();
            await database.StartAsync( cancellation );
            await using ApiFactoryForDatabase app = new( database.GetConnectionString() );
            using HttpClient client = app.CreateClient();

            using HttpResponseMessage before = await client.GetAsync( "/api/health", cancellation );
            await database.StopAsync( cancellation );
            using HttpResponseMessage after = await client.GetAsync( "/api/health", cancellation );

            Assert.Equal( HttpStatusCode.OK, before.StatusCode );
            Assert.Equal( HttpStatusCode.ServiceUnavailable, after.StatusCode );
            Assert.Equal( "Unhealthy", await after.Content.ReadAsStringAsync( cancellation ) );
        }
    }
}
