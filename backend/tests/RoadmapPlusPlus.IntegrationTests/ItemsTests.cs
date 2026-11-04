using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RoadmapPlusPlus.IntegrationTests
{
    public sealed class ItemsTests( PostgresApiFactory api )
    {
        private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

        [Fact]
        public async Task CreatedEventIsReadBackAndShownInRange()
        {
            using SignedInUser user = await TestUsers.SignUpAsync( api );
            Guid team = await CreateTeamAsync( user );

            Details created = await CreateAsync( user, new { workspaceId = team, kind = "event", title = "Хакатон", startDate = "2026-09-28", endDate = "2026-09-29", descriptionMd = "ТЗ" } );
            Details read = ( await user.Client.GetFromJsonAsync<Details>( $"/api/items/{created.Item.Id}", Cancellation ) )!;
            Item[] inSeptember = await RangeAsync( user, "2026-09-01", "2026-09-30" );
            Item[] inOctober = await RangeAsync( user, "2026-10-01", "2026-10-31" );

            Assert.Equal( ("event", "Хакатон", team), (read.Item.Kind, read.Item.Title, read.Item.WorkspaceId) );
            Assert.Equal( "ТЗ", read.DescriptionMd );
            Assert.Empty( read.Path );
            Assert.Contains( inSeptember, i => i.Id == created.Item.Id );
            Assert.DoesNotContain( inOctober, i => i.Id == created.Item.Id );
        }

        [Fact]
        public async Task ChildrenInheritTeamAndKnowTheirPath()
        {
            using SignedInUser user = await TestUsers.SignUpAsync( api );
            Guid team = await CreateTeamAsync( user );

            Details macro = await CreateAsync( user, new { workspaceId = team, kind = "macro", title = "Проектная деятельность" } );
            Details hackathon = await CreateAsync( user, new { parentId = macro.Item.Id, kind = "event", title = "Хакатон" } );
            Details day = await CreateAsync( user, new { parentId = hackathon.Item.Id, kind = "group", title = "День 1" } );
            Details task = await CreateAsync( user, new { parentId = day.Item.Id, kind = "task", title = "Репозиторий" } );
            Details secondDay = await CreateAsync( user, new { parentId = hackathon.Item.Id, kind = "group", title = "День 2" } );
            Details hackathonAgain = ( await user.Client.GetFromJsonAsync<Details>( $"/api/items/{hackathon.Item.Id}", Cancellation ) )!;

            Assert.Equal( team, task.Item.WorkspaceId );
            Assert.Equal( [ "Проектная деятельность", "Хакатон", "День 1" ], task.Path.Select( p => p.Title ) );
            Assert.Equal( [ "День 1", "День 2" ], hackathonAgain.Children.Select( c => c.Title ) );
            Assert.Equal( [ 0, 1 ], hackathonAgain.Children.Select( c => c.Position ) );
            Assert.Equal( secondDay.Item.Id, hackathonAgain.Children[ 1 ].Id );
        }

        [Fact]
        public async Task WrongNestingIsExplained()
        {
            using SignedInUser user = await TestUsers.SignUpAsync( api );
            Guid team = await CreateTeamAsync( user );
            Details macro = await CreateAsync( user, new { workspaceId = team, kind = "macro", title = "Учёба" } );

            using HttpResponseMessage taskAtTop = await user.Client.PostAsJsonAsync( "/api/items", new { workspaceId = team, kind = "task", title = "Задача" }, Cancellation );
            using HttpResponseMessage taskInMacro = await user.Client.PostAsJsonAsync( "/api/items", new { parentId = macro.Item.Id, kind = "task", title = "Задача" }, Cancellation );

            Assert.Equal( HttpStatusCode.BadRequest, taskAtTop.StatusCode );
            Assert.Equal( "planning.invalid_nesting", await CodeAsync( taskAtTop ) );
            Assert.Equal( "planning.invalid_nesting", await CodeAsync( taskInMacro ) );
        }

        [Fact]
        public async Task DatesAreValidated()
        {
            using SignedInUser user = await TestUsers.SignUpAsync( api );
            Guid team = await CreateTeamAsync( user );

            using HttpResponseMessage reversed = await user.Client.PostAsJsonAsync(
                "/api/items",
                new { workspaceId = team, kind = "event", title = "Наоборот", startDate = "2026-09-29", endDate = "2026-09-28" },
                Cancellation );

            JsonElement problem = await ProblemAsync( reversed );
            Assert.Equal( HttpStatusCode.BadRequest, reversed.StatusCode );
            Assert.True( problem.GetProperty( "errors" ).TryGetProperty( "endDate", out _ ) );
        }

        [Fact]
        public async Task OutsiderSeesNothingAndCannotWrite()
        {
            using SignedInUser owner = await TestUsers.SignUpAsync( api );
            using SignedInUser outsider = await TestUsers.SignUpAsync( api );
            Guid team = await CreateTeamAsync( owner );
            Details secret = await CreateAsync( owner, new { workspaceId = team, kind = "event", title = "Секрет", startDate = "2026-09-28", endDate = "2026-09-29" } );

            using HttpResponseMessage read = await outsider.Client.GetAsync( $"/api/items/{secret.Item.Id}", Cancellation );
            using HttpResponseMessage child = await outsider.Client.PostAsJsonAsync( "/api/items", new { parentId = secret.Item.Id, kind = "task", title = "Чужое" }, Cancellation );
            using HttpResponseMessage root = await outsider.Client.PostAsJsonAsync( "/api/items", new { workspaceId = team, kind = "event", title = "Чужое" }, Cancellation );
            using HttpResponseMessage delete = await outsider.Client.DeleteAsync( $"/api/items/{secret.Item.Id}", Cancellation );
            Item[] visible = await RangeAsync( outsider, "2026-09-01", "2026-09-30" );

            Assert.Equal( HttpStatusCode.NotFound, read.StatusCode );
            Assert.Equal( "planning.item_not_found", await CodeAsync( child ) );
            Assert.Equal( "planning.workspace_not_found", await CodeAsync( root ) );
            Assert.Equal( HttpStatusCode.NotFound, delete.StatusCode );
            Assert.DoesNotContain( visible, i => i.Id == secret.Item.Id );
        }

        [Fact]
        public async Task RangeCoversAllMyTeams()
        {
            using SignedInUser user = await TestUsers.SignUpAsync( api );
            Guid study = await CreateTeamAsync( user );
            Guid hackathons = await CreateTeamAsync( user );
            Details lecture = await CreateAsync( user, new { workspaceId = study, kind = "event", title = "Лекция", startDate = "2026-11-10", endDate = "2026-11-10" } );
            Details hackathon = await CreateAsync( user, new { workspaceId = hackathons, kind = "event", title = "Хакатон", startDate = "2026-11-14", endDate = "2026-11-16" } );

            Item[] all = await RangeAsync( user, "2026-11-01", "2026-11-30" );
            Item[] onlyStudy = ( await user.Client.GetFromJsonAsync<Item[]>( $"/api/items?from=2026-11-01&to=2026-11-30&workspaceId={study}", Cancellation ) )!;

            Assert.Equal( [ lecture.Item.Id, hackathon.Item.Id ], all.Select( i => i.Id ) );
            Assert.Equal( [ lecture.Item.Id ], onlyStudy.Select( i => i.Id ) );
        }

        [Fact]
        public async Task TooWideRangeIsRejected()
        {
            using SignedInUser user = await TestUsers.SignUpAsync( api );

            using HttpResponseMessage response = await user.Client.GetAsync( "/api/items?from=2020-01-01&to=2030-01-01", Cancellation );

            Assert.Equal( HttpStatusCode.BadRequest, response.StatusCode );
        }

        [Fact]
        public async Task UpdateChangesFields()
        {
            using SignedInUser user = await TestUsers.SignUpAsync( api );
            Guid team = await CreateTeamAsync( user );
            Details created = await CreateAsync( user, new { workspaceId = team, kind = "event", title = "Черновик" } );

            using HttpResponseMessage response = await user.Client.PutAsJsonAsync(
                $"/api/items/{created.Item.Id}",
                new { title = "Хакатон", startDate = "2026-12-01", endDate = "2026-12-03", descriptionMd = "Положение" },
                Cancellation );
            Details updated = ( await response.Content.ReadFromJsonAsync<Details>( Cancellation ) )!;

            Assert.Equal( HttpStatusCode.OK, response.StatusCode );
            Assert.Equal( ("Хакатон", "2026-12-01", "2026-12-03", "Положение"), (updated.Item.Title, updated.Item.StartDate, updated.Item.EndDate, updated.DescriptionMd) );
        }

        [Fact]
        public async Task DeletingRemovesTheWholeSubtree()
        {
            using SignedInUser user = await TestUsers.SignUpAsync( api );
            Guid team = await CreateTeamAsync( user );
            Details hackathon = await CreateAsync( user, new { workspaceId = team, kind = "event", title = "Хакатон" } );
            Details day = await CreateAsync( user, new { parentId = hackathon.Item.Id, kind = "group", title = "День 1" } );
            Details task = await CreateAsync( user, new { parentId = day.Item.Id, kind = "task", title = "Репозиторий" } );

            using HttpResponseMessage deleted = await user.Client.DeleteAsync( $"/api/items/{hackathon.Item.Id}", Cancellation );
            using HttpResponseMessage dayAfter = await user.Client.GetAsync( $"/api/items/{day.Item.Id}", Cancellation );
            using HttpResponseMessage taskAfter = await user.Client.GetAsync( $"/api/items/{task.Item.Id}", Cancellation );

            Assert.Equal( HttpStatusCode.NoContent, deleted.StatusCode );
            Assert.Equal( HttpStatusCode.NotFound, dayAfter.StatusCode );
            Assert.Equal( HttpStatusCode.NotFound, taskAfter.StatusCode );
        }

        private static async Task<Guid> CreateTeamAsync( SignedInUser user )
        {
            using HttpResponseMessage response = await user.Client.PostAsJsonAsync( "/api/workspaces", new { name = "Команда" }, Cancellation );
            response.EnsureSuccessStatusCode();
            return ( await response.Content.ReadFromJsonAsync<JsonElement>( Cancellation ) ).GetProperty( "id" ).GetGuid();
        }

        private static async Task<Details> CreateAsync( SignedInUser user, object body )
        {
            using HttpResponseMessage response = await user.Client.PostAsJsonAsync( "/api/items", body, Cancellation );
            Assert.Equal( HttpStatusCode.Created, response.StatusCode );
            return ( await response.Content.ReadFromJsonAsync<Details>( Cancellation ) )!;
        }

        private static async Task<Item[]> RangeAsync( SignedInUser user, string from, string to )
        {
            return ( await user.Client.GetFromJsonAsync<Item[]>( $"/api/items?from={from}&to={to}", Cancellation ) )!;
        }

        private static async Task<string?> CodeAsync( HttpResponseMessage response )
        {
            return ( await ProblemAsync( response ) ).GetProperty( "code" ).GetString();
        }

        private static async Task<JsonElement> ProblemAsync( HttpResponseMessage response )
        {
            using JsonDocument document = JsonDocument.Parse( await response.Content.ReadAsStringAsync( Cancellation ) );
            return document.RootElement.Clone();
        }

        private sealed record Item( Guid Id, Guid WorkspaceId, Guid? ParentId, string Kind, string Title, string? StartDate, string? EndDate, int Position );

        private sealed record Details( Item Item, string DescriptionMd, Item[] Path, Item[] Children );
    }
}
