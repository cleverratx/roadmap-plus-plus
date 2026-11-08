using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RoadmapPlusPlus.IntegrationTests
{
    public sealed class ItemMoveApiTests( PostgresApiFactory api )
    {
        private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

        [Fact]
        public async Task EventMovesToAnotherMacroWithItsSubtree()
        {
            using SignedInUser user = await TestUsers.SignUpAsync( api );
            Guid team = await CreateTeamAsync( user );
            Guid study = await CreateAsync( user, new { workspaceId = team, kind = "macro", title = "Учёба" } );
            Guid projects = await CreateAsync( user, new { workspaceId = team, kind = "macro", title = "Проектная деятельность" } );
            Guid hackathon = await CreateAsync( user, new { parentId = study, kind = "event", title = "Хакатон" } );
            Guid day = await CreateAsync( user, new { parentId = hackathon, kind = "group", title = "День 1" } );

            using HttpResponseMessage response = await MoveAsync( user, hackathon, new { parentId = projects } );
            Details dayAfter = await DetailsAsync( user, day );
            Details studyAfter = await DetailsAsync( user, study );

            Assert.Equal( HttpStatusCode.OK, response.StatusCode );
            Assert.Equal( [ "Проектная деятельность", "Хакатон" ], dayAfter.Path.Select( p => p.Title ) );
            Assert.Empty( studyAfter.Children );
        }

        [Fact]
        public async Task SiblingsAreReordered()
        {
            using SignedInUser user = await TestUsers.SignUpAsync( api );
            Guid team = await CreateTeamAsync( user );
            Guid hackathon = await CreateAsync( user, new { workspaceId = team, kind = "event", title = "Хакатон" } );
            Guid first = await CreateAsync( user, new { parentId = hackathon, kind = "group", title = "День 1" } );
            await CreateAsync( user, new { parentId = hackathon, kind = "group", title = "День 2" } );
            await CreateAsync( user, new { parentId = hackathon, kind = "group", title = "День 3" } );

            using HttpResponseMessage down = await MoveAsync( user, first, new { parentId = hackathon, position = 1 } );
            Details afterDown = await DetailsAsync( user, hackathon );
            using HttpResponseMessage toStart = await MoveAsync( user, first, new { parentId = hackathon, position = 0 } );
            Details afterStart = await DetailsAsync( user, hackathon );

            Assert.Equal( [ "День 2", "День 1", "День 3" ], afterDown.Children.Select( c => c.Title ) );
            Assert.Equal( [ "День 1", "День 2", "День 3" ], afterStart.Children.Select( c => c.Title ) );
            Assert.Equal( [ 0, 1, 2 ], afterStart.Children.Select( c => c.Position ) );
        }

        [Fact]
        public async Task MovingIntoOwnDescendantIsRefused()
        {
            using SignedInUser user = await TestUsers.SignUpAsync( api );
            Guid team = await CreateTeamAsync( user );
            Guid hackathon = await CreateAsync( user, new { workspaceId = team, kind = "event", title = "Хакатон" } );
            Guid day = await CreateAsync( user, new { parentId = hackathon, kind = "group", title = "День 1" } );
            Guid morning = await CreateAsync( user, new { parentId = day, kind = "group", title = "Утро" } );

            using HttpResponseMessage response = await MoveAsync( user, day, new { parentId = morning } );

            Assert.Equal( HttpStatusCode.BadRequest, response.StatusCode );
            Assert.Equal( "planning.move_cycle", await CodeAsync( response ) );
            Assert.Equal( [ "Хакатон" ], ( await DetailsAsync( user, day ) ).Path.Select( p => p.Title ) );
        }

        [Fact]
        public async Task CrossingTeamsAndBadNestingAreRefused()
        {
            using SignedInUser user = await TestUsers.SignUpAsync( api );
            Guid study = await CreateTeamAsync( user );
            Guid skb = await CreateTeamAsync( user );
            Guid lecture = await CreateAsync( user, new { workspaceId = study, kind = "event", title = "Лекция" } );
            Guid grant = await CreateAsync( user, new { workspaceId = skb, kind = "macro", title = "Грант" } );
            Guid hackathon = await CreateAsync( user, new { workspaceId = skb, kind = "event", title = "Хакатон" } );
            Guid task = await CreateAsync( user, new { parentId = hackathon, kind = "task", title = "Репозиторий" } );

            using HttpResponseMessage acrossTeams = await MoveAsync( user, lecture, new { parentId = grant } );
            using HttpResponseMessage taskToTop = await MoveAsync( user, task, new { parentId = ( Guid? )null } );

            Assert.Equal( "planning.move_across_workspaces", await CodeAsync( acrossTeams ) );
            Assert.Equal( "planning.invalid_nesting", await CodeAsync( taskToTop ) );
        }

        [Fact]
        public async Task TargetsFollowTheRules()
        {
            using SignedInUser user = await TestUsers.SignUpAsync( api );
            Guid team = await CreateTeamAsync( user );
            Guid other = await CreateTeamAsync( user );
            Guid hackathon = await CreateAsync( user, new { workspaceId = team, kind = "event", title = "Хакатон" } );
            Guid day = await CreateAsync( user, new { parentId = hackathon, kind = "group", title = "День 1" } );
            await CreateAsync( user, new { parentId = day, kind = "group", title = "Утро" } );
            await CreateAsync( user, new { parentId = day, kind = "task", title = "Репозиторий" } );
            Guid conference = await CreateAsync( user, new { workspaceId = team, kind = "event", title = "Конференция" } );
            await CreateAsync( user, new { workspaceId = other, kind = "event", title = "Чужое событие" } );

            Targets targets = ( await user.Client.GetFromJsonAsync<Targets>( $"/api/items/{day}/move-targets", Cancellation ) )!;

            Assert.False( targets.CanBeRoot );
            Assert.Equal( hackathon, targets.CurrentParentId );
            Assert.Equal( [ "Хакатон", "Конференция" ], targets.Parents.Select( p => p.Item.Title ) );
            Assert.Contains( targets.Parents, p => p.Item.Id == conference && p.Path.Length == 0 );
        }

        [Fact]
        public async Task OutsiderCannotMove()
        {
            using SignedInUser owner = await TestUsers.SignUpAsync( api );
            using SignedInUser outsider = await TestUsers.SignUpAsync( api );
            Guid team = await CreateTeamAsync( owner );
            Guid hackathon = await CreateAsync( owner, new { workspaceId = team, kind = "event", title = "Хакатон" } );

            using HttpResponseMessage move = await MoveAsync( outsider, hackathon, new { parentId = ( Guid? )null } );
            using HttpResponseMessage targets = await outsider.Client.GetAsync( $"/api/items/{hackathon}/move-targets", Cancellation );

            Assert.Equal( HttpStatusCode.NotFound, move.StatusCode );
            Assert.Equal( HttpStatusCode.NotFound, targets.StatusCode );
        }

        private static async Task<Guid> CreateTeamAsync( SignedInUser user )
        {
            using HttpResponseMessage response = await user.Client.PostAsJsonAsync( "/api/workspaces", new { name = "Команда" }, Cancellation );
            response.EnsureSuccessStatusCode();
            return ( await response.Content.ReadFromJsonAsync<JsonElement>( Cancellation ) ).GetProperty( "id" ).GetGuid();
        }

        private static async Task<Guid> CreateAsync( SignedInUser user, object body )
        {
            using HttpResponseMessage response = await user.Client.PostAsJsonAsync( "/api/items", body, Cancellation );
            Assert.Equal( HttpStatusCode.Created, response.StatusCode );
            return ( await response.Content.ReadFromJsonAsync<Details>( Cancellation ) )!.Item.Id;
        }

        private static Task<HttpResponseMessage> MoveAsync( SignedInUser user, Guid itemId, object body )
        {
            return user.Client.PostAsJsonAsync( $"/api/items/{itemId}/move", body, Cancellation );
        }

        private static async Task<Details> DetailsAsync( SignedInUser user, Guid itemId )
        {
            return ( await user.Client.GetFromJsonAsync<Details>( $"/api/items/{itemId}", Cancellation ) )!;
        }

        private static async Task<string?> CodeAsync( HttpResponseMessage response )
        {
            return ( await response.Content.ReadFromJsonAsync<JsonElement>( Cancellation ) ).GetProperty( "code" ).GetString();
        }

        private sealed record Item( Guid Id, string Title, int Position );

        private sealed record Details( Item Item, Item[] Path, Item[] Children );

        private sealed record Target( Item Item, string[] Path );

        private sealed record Targets( bool CanBeRoot, Guid? CurrentParentId, Target[] Parents );
    }
}
