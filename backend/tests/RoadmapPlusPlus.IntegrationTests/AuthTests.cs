using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace RoadmapPlusPlus.IntegrationTests
{
    public sealed class AuthTests( PostgresApiFactory api )
    {
        private const string Password = "correct horse";

        private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

        [Fact]
        public async Task RegisterSignsInAndMeReturnsUser()
        {
            using HttpClient client = api.CreateClient();
            string email = NewEmail();

            using HttpResponseMessage registered = await RegisterAsync( client, email );
            Me? me = await client.GetFromJsonAsync<Me>( "/api/me", Cancellation );

            Assert.Equal( HttpStatusCode.OK, registered.StatusCode );
            Assert.NotNull( me );
            Assert.Equal( email, me.Email );
            Assert.Equal( "Ярослав", me.DisplayName );
            Assert.NotEqual( Guid.Empty, me.Id );
        }

        [Fact]
        public async Task MeWithoutSessionIsUnauthorizedProblem()
        {
            using HttpClient client = api.CreateClient();

            using HttpResponseMessage response = await client.GetAsync( "/api/me", Cancellation );

            Assert.Equal( HttpStatusCode.Unauthorized, response.StatusCode );
            Assert.Equal( "application/problem+json", response.Content.Headers.ContentType?.MediaType );
            Assert.Null( response.Headers.Location );
        }

        [Fact]
        public async Task SameEmailInAnotherCaseIsTaken()
        {
            using HttpClient client = api.CreateClient();
            string email = NewEmail();
            using HttpResponseMessage first = await RegisterAsync( client, email );

            using HttpResponseMessage second = await RegisterAsync( api.CreateClient(), email.ToUpperInvariant() );

            Assert.Equal( HttpStatusCode.Conflict, second.StatusCode );
            Assert.Equal( "identity.email_taken", ( await ProblemAsync( second ) ).GetProperty( "code" ).GetString() );
        }

        [Fact]
        public async Task InvalidRegistrationListsFields()
        {
            using HttpClient client = api.CreateClient();

            using HttpResponseMessage response = await client.PostAsJsonAsync(
                "/api/auth/register",
                new { email = "not-an-email", password = "short", displayName = " " },
                Cancellation );

            JsonElement problem = await ProblemAsync( response );
            JsonElement errors = problem.GetProperty( "errors" );
            Assert.Equal( HttpStatusCode.BadRequest, response.StatusCode );
            Assert.Equal( "identity.invalid_request", problem.GetProperty( "code" ).GetString() );
            Assert.True( errors.TryGetProperty( "email", out _ ) );
            Assert.True( errors.TryGetProperty( "password", out _ ) );
            Assert.True( errors.TryGetProperty( "displayName", out _ ) );
        }

        [Fact]
        public async Task WrongPasswordIsInvalidCredentials()
        {
            string email = NewEmail();
            using HttpResponseMessage registered = await RegisterAsync( api.CreateClient(), email );
            using HttpClient client = api.CreateClient();

            using HttpResponseMessage response = await LoginAsync( client, email, "wrong password" );

            Assert.Equal( HttpStatusCode.BadRequest, response.StatusCode );
            Assert.Equal( "identity.invalid_credentials", ( await ProblemAsync( response ) ).GetProperty( "code" ).GetString() );
        }

        [Fact]
        public async Task UnknownEmailLooksLikeWrongPassword()
        {
            using HttpClient client = api.CreateClient();

            using HttpResponseMessage response = await LoginAsync( client, NewEmail(), Password );

            Assert.Equal( HttpStatusCode.BadRequest, response.StatusCode );
            Assert.Equal( "identity.invalid_credentials", ( await ProblemAsync( response ) ).GetProperty( "code" ).GetString() );
        }

        [Fact]
        public async Task LoginThenLogoutEndsSession()
        {
            string email = NewEmail();
            using HttpResponseMessage registered = await RegisterAsync( api.CreateClient(), email );
            using HttpClient client = api.CreateClient();

            using HttpResponseMessage login = await LoginAsync( client, email, Password );
            using HttpResponseMessage meAfterLogin = await client.GetAsync( "/api/me", Cancellation );
            using HttpResponseMessage logout = await client.PostAsync( "/api/auth/logout", null, Cancellation );
            using HttpResponseMessage meAfterLogout = await client.GetAsync( "/api/me", Cancellation );

            Assert.Equal( HttpStatusCode.OK, login.StatusCode );
            Assert.Equal( HttpStatusCode.OK, meAfterLogin.StatusCode );
            Assert.Equal( HttpStatusCode.NoContent, logout.StatusCode );
            Assert.Equal( HttpStatusCode.Unauthorized, meAfterLogout.StatusCode );
        }

        [Fact]
        public async Task FiveFailedLoginsLockTheAccount()
        {
            string email = NewEmail();
            using HttpResponseMessage registered = await RegisterAsync( api.CreateClient(), email );
            using HttpClient client = api.CreateClient();

            for ( int attempt = 0; attempt < 5; attempt++ )
            {
                using HttpResponseMessage failed = await LoginAsync( client, email, "wrong password" );
            }

            using HttpResponseMessage response = await LoginAsync( client, email, Password );

            Assert.Equal( HttpStatusCode.Forbidden, response.StatusCode );
            Assert.Equal( "identity.locked_out", ( await ProblemAsync( response ) ).GetProperty( "code" ).GetString() );
        }

        [Fact]
        public async Task SessionCookieIsHttpOnlyAndLax()
        {
            using HttpClient client = api.CreateClient( new WebApplicationFactoryClientOptions { HandleCookies = false } );

            using HttpResponseMessage response = await RegisterAsync( client, NewEmail() );

            string cookie = Assert.Single( response.Headers.GetValues( "Set-Cookie" ), c => c.StartsWith( "roadmap_session=", StringComparison.Ordinal ) );
            Assert.Contains( "httponly", cookie, StringComparison.OrdinalIgnoreCase );
            Assert.Contains( "samesite=lax", cookie, StringComparison.OrdinalIgnoreCase );
            Assert.Contains( "expires=", cookie, StringComparison.OrdinalIgnoreCase );
        }

        [Fact]
        public async Task DataProtectionKeysAreStoredInDatabase()
        {
            using HttpResponseMessage registered = await RegisterAsync( api.CreateClient(), NewEmail() );

            await using NpgsqlConnection connection = new( api.ConnectionString );
            await connection.OpenAsync( Cancellation );
            await using NpgsqlCommand count = new( "SELECT count(*) FROM identity.data_protection_keys", connection );

            Assert.True( ( long )( await count.ExecuteScalarAsync( Cancellation ) )! > 0 );
        }

        [Fact]
        public async Task TooManyAuthRequestsAreRejected()
        {
            await using WebApplicationFactory<Program> limited = api.WithWebHostBuilder( builder => builder.UseSetting( TestApi.AuthRateLimitSetting, "2" ) );
            using HttpClient client = limited.CreateClient();

            using HttpResponseMessage first = await LoginAsync( client, NewEmail(), Password );
            using HttpResponseMessage second = await LoginAsync( client, NewEmail(), Password );
            using HttpResponseMessage third = await LoginAsync( client, NewEmail(), Password );

            Assert.Equal( HttpStatusCode.BadRequest, second.StatusCode );
            Assert.Equal( HttpStatusCode.TooManyRequests, third.StatusCode );
            Assert.Equal( "application/problem+json", third.Content.Headers.ContentType?.MediaType );
        }

        private static string NewEmail()
        {
            return $"user-{Guid.NewGuid():N}@example.com";
        }

        private static Task<HttpResponseMessage> RegisterAsync( HttpClient client, string email )
        {
            return client.PostAsJsonAsync( "/api/auth/register", new { email, password = Password, displayName = "Ярослав" }, Cancellation );
        }

        private static Task<HttpResponseMessage> LoginAsync( HttpClient client, string email, string password )
        {
            return client.PostAsJsonAsync( "/api/auth/login", new { email, password }, Cancellation );
        }

        private static async Task<JsonElement> ProblemAsync( HttpResponseMessage response )
        {
            using JsonDocument document = JsonDocument.Parse( await response.Content.ReadAsStringAsync( Cancellation ) );
            return document.RootElement.Clone();
        }

        private sealed record Me( Guid Id, string Email, string DisplayName );
    }
}
