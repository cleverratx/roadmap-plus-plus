using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RoadmapPlusPlus.IntegrationTests
{
    public sealed class LinksTests( PostgresApiFactory api )
    {
        private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

        [Fact]
        public async Task LinksAreAddedWithKindAndShownInThePanel()
        {
            using SignedInUser user = await TestUsers.SignUpAsync( api );
            Guid hackathon = await CreateEventAsync( user );

            Link sheet = await AddAsync( user, hackathon, new { url = "https://docs.google.com/spreadsheets/d/1AbC/edit", title = "Задачи" } );
            Link post = await AddAsync( user, hackathon, new { url = "t.me/skb_hack/42" } );
            Details details = ( await user.Client.GetFromJsonAsync<Details>( $"/api/items/{hackathon}", Cancellation ) )!;

            Assert.Equal( ("googleSheet", "Задачи", 0), (sheet.Kind, sheet.Title, sheet.Position) );
            Assert.Equal( ("tgPost", "https://t.me/skb_hack/42", string.Empty, 1), (post.Kind, post.Url, post.Title, post.Position) );
            Assert.Equal( [ sheet.Id, post.Id ], details.Links.Select( l => l.Id ) );
        }

        [Fact]
        public async Task EditingTheAddressRedetectsTheKind()
        {
            using SignedInUser user = await TestUsers.SignUpAsync( api );
            Guid hackathon = await CreateEventAsync( user );
            Link link = await AddAsync( user, hackathon, new { url = "https://example.com/rules" } );

            using HttpResponseMessage response = await user.Client.PutAsJsonAsync(
                $"/api/items/{hackathon}/links/{link.Id}",
                new { url = "https://github.com/skb/hackathon", title = "Репозиторий" },
                Cancellation );
            Link updated = ( await response.Content.ReadFromJsonAsync<Link>( Cancellation ) )!;

            Assert.Equal( HttpStatusCode.OK, response.StatusCode );
            Assert.Equal( ("repo", "Репозиторий"), (updated.Kind, updated.Title) );
        }

        [Fact]
        public async Task UnsafeAddressIsRejected()
        {
            using SignedInUser user = await TestUsers.SignUpAsync( api );
            Guid hackathon = await CreateEventAsync( user );

            using HttpResponseMessage response = await user.Client.PostAsJsonAsync( $"/api/items/{hackathon}/links", new { url = "javascript:alert(1)" }, Cancellation );

            JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>( Cancellation );
            Assert.Equal( HttpStatusCode.BadRequest, response.StatusCode );
            Assert.True( problem.GetProperty( "errors" ).TryGetProperty( "url", out _ ) );
        }

        [Fact]
        public async Task DeletedLinkDisappears()
        {
            using SignedInUser user = await TestUsers.SignUpAsync( api );
            Guid hackathon = await CreateEventAsync( user );
            Link link = await AddAsync( user, hackathon, new { url = "https://www.figma.com/design/AbC" } );

            using HttpResponseMessage deleted = await user.Client.DeleteAsync( $"/api/items/{hackathon}/links/{link.Id}", Cancellation );
            using HttpResponseMessage again = await user.Client.DeleteAsync( $"/api/items/{hackathon}/links/{link.Id}", Cancellation );
            Details details = ( await user.Client.GetFromJsonAsync<Details>( $"/api/items/{hackathon}", Cancellation ) )!;

            Assert.Equal( HttpStatusCode.NoContent, deleted.StatusCode );
            Assert.Equal( "planning.link_not_found", ( await again.Content.ReadFromJsonAsync<JsonElement>( Cancellation ) ).GetProperty( "code" ).GetString() );
            Assert.Empty( details.Links );
        }

        [Fact]
        public async Task LinkOfAnotherItemIsNotReachable()
        {
            using SignedInUser user = await TestUsers.SignUpAsync( api );
            Guid hackathon = await CreateEventAsync( user );
            Guid conference = await CreateEventAsync( user );
            Link link = await AddAsync( user, hackathon, new { url = "https://github.com/skb/hackathon" } );

            using HttpResponseMessage response = await user.Client.DeleteAsync( $"/api/items/{conference}/links/{link.Id}", Cancellation );

            Assert.Equal( HttpStatusCode.NotFound, response.StatusCode );
        }

        [Fact]
        public async Task OutsiderCannotTouchLinks()
        {
            using SignedInUser owner = await TestUsers.SignUpAsync( api );
            using SignedInUser outsider = await TestUsers.SignUpAsync( api );
            Guid hackathon = await CreateEventAsync( owner );
            Link link = await AddAsync( owner, hackathon, new { url = "https://github.com/skb/hackathon" } );

            using HttpResponseMessage add = await outsider.Client.PostAsJsonAsync( $"/api/items/{hackathon}/links", new { url = "https://example.com" }, Cancellation );
            using HttpResponseMessage edit = await outsider.Client.PutAsJsonAsync( $"/api/items/{hackathon}/links/{link.Id}", new { url = "https://example.com" }, Cancellation );
            using HttpResponseMessage delete = await outsider.Client.DeleteAsync( $"/api/items/{hackathon}/links/{link.Id}", Cancellation );

            Assert.Equal( HttpStatusCode.NotFound, add.StatusCode );
            Assert.Equal( HttpStatusCode.NotFound, edit.StatusCode );
            Assert.Equal( HttpStatusCode.NotFound, delete.StatusCode );
        }

        [Fact]
        public async Task LinksGoAwayWithTheirItem()
        {
            using SignedInUser user = await TestUsers.SignUpAsync( api );
            Guid hackathon = await CreateEventAsync( user );
            Link link = await AddAsync( user, hackathon, new { url = "https://github.com/skb/hackathon" } );

            using HttpResponseMessage deleted = await user.Client.DeleteAsync( $"/api/items/{hackathon}", Cancellation );
            using HttpResponseMessage linkAfter = await user.Client.PutAsJsonAsync( $"/api/items/{hackathon}/links/{link.Id}", new { url = "https://example.com" }, Cancellation );

            Assert.Equal( HttpStatusCode.NoContent, deleted.StatusCode );
            Assert.Equal( HttpStatusCode.NotFound, linkAfter.StatusCode );
        }

        private static async Task<Guid> CreateEventAsync( SignedInUser user )
        {
            using HttpResponseMessage team = await user.Client.PostAsJsonAsync( "/api/workspaces", new { name = "Команда" }, Cancellation );
            Guid teamId = ( await team.Content.ReadFromJsonAsync<JsonElement>( Cancellation ) ).GetProperty( "id" ).GetGuid();

            using HttpResponseMessage item = await user.Client.PostAsJsonAsync(
                "/api/items",
                new { workspaceId = teamId, kind = "event", title = "Хакатон", startDate = "2026-11-14", endDate = "2026-11-16" },
                Cancellation );
            return ( await item.Content.ReadFromJsonAsync<Details>( Cancellation ) )!.Item.Id;
        }

        private static async Task<Link> AddAsync( SignedInUser user, Guid itemId, object body )
        {
            using HttpResponseMessage response = await user.Client.PostAsJsonAsync( $"/api/items/{itemId}/links", body, Cancellation );
            Assert.Equal( HttpStatusCode.Created, response.StatusCode );
            return ( await response.Content.ReadFromJsonAsync<Link>( Cancellation ) )!;
        }

        private sealed record Link( Guid Id, string Url, string Title, string Kind, int Position );

        private sealed record ItemRef( Guid Id );

        private sealed record Details( ItemRef Item, Link[] Links );
    }
}
