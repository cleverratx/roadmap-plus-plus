using System.Net.Http.Json;
using System.Text.Json;

namespace RoadmapPlusPlus.IntegrationTests
{
    /// <summary>
    /// Зарегистрированный пользователь и его HTTP-клиент: клиент хранит cookie сессии, поэтому все запросы идут от его имени.
    /// </summary>
    internal sealed record SignedInUser( HttpClient Client, Guid Id, string Email ) : IDisposable
    {
        public void Dispose()
        {
            Client.Dispose();
        }
    }

    internal static class TestUsers
    {
        public static async Task<SignedInUser> SignUpAsync( PostgresApiFactory api, string displayName = "Участник" )
        {
            HttpClient client = api.CreateClient();
            string email = $"user-{Guid.NewGuid():N}@example.com";

            using HttpResponseMessage response = await client.PostAsJsonAsync(
                "/api/auth/register",
                new { email, password = "correct horse", displayName },
                TestContext.Current.CancellationToken );
            response.EnsureSuccessStatusCode();

            JsonElement me = await response.Content.ReadFromJsonAsync<JsonElement>( TestContext.Current.CancellationToken );
            return new SignedInUser( client, me.GetProperty( "id" ).GetGuid(), email );
        }
    }
}
