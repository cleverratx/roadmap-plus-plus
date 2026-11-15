using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RoadmapPlusPlus.IntegrationTests
{
    public sealed class AccessTests( PostgresApiFactory api )
    {
        private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

        [Fact]
        public async Task WithRolesOffEveryoneSeesEverything()
        {
            await using Team team = await Team.CreateAsync( api );
            Guid hackathon = await team.CreateAsync( team.Head, new { workspaceId = team.Id, kind = "event", title = "Хакатон", startDate = "2026-11-14", endDate = "2026-11-16" } );

            Assert.Contains( await team.RangeAsync( team.Member ), i => i.Id == hackathon );
        }

        [Fact]
        public async Task MemberSeesOnlyWhatTheirRolesOpenWithTheWholeSubtree()
        {
            await using Team team = await Team.CreateAsync( api );
            Guid jury = await team.RoleForMemberAsync( "Жюри" );
            Guid hackathon = await team.CreateAsync( team.Head, new { workspaceId = team.Id, kind = "event", title = "Хакатон", startDate = "2026-11-14", endDate = "2026-11-16", roleIds = new[] { jury } } );
            Guid day = await team.CreateAsync( team.Head, new { parentId = hackathon, kind = "group", title = "День 1", startDate = "2026-11-14", endDate = "2026-11-14" } );
            Guid secret = await team.CreateAsync( team.Head, new { workspaceId = team.Id, kind = "event", title = "Секрет", startDate = "2026-11-20", endDate = "2026-11-21" } );
            await team.EnableRolesAsync();

            Item[] seen = await team.RangeAsync( team.Member );
            using HttpResponseMessage secretDetails = await team.Member.Client.GetAsync( $"/api/items/{secret}", Cancellation );

            Assert.Equal( new[] { hackathon, day }.Order(), seen.Select( i => i.Id ).Order() );
            Assert.All( seen, i => Assert.False( i.IsFrame ) );
            Assert.Equal( HttpStatusCode.NotFound, secretDetails.StatusCode );
            Assert.Equal( 3, ( await team.RangeAsync( team.Head ) ).Length );
        }

        [Fact]
        public async Task AncestorsOfOpenItemsAreFramesWithoutContent()
        {
            await using Team team = await Team.CreateAsync( api );
            Guid jury = await team.RoleForMemberAsync( "Жюри" );
            Guid projects = await team.CreateAsync( team.Head, new { workspaceId = team.Id, kind = "macro", title = "Проектная деятельность", startDate = "2026-09-01", endDate = "2026-12-31", descriptionMd = "Внутреннее" } );
            Guid hackathon = await team.CreateAsync( team.Head, new { parentId = projects, kind = "event", title = "Хакатон", startDate = "2026-11-14", endDate = "2026-11-16" } );
            Guid other = await team.CreateAsync( team.Head, new { parentId = projects, kind = "event", title = "Другое", startDate = "2026-11-20", endDate = "2026-11-21" } );
            await team.SetItemRolesAsync( hackathon, jury );
            await team.EnableRolesAsync();

            Item[] seen = await team.RangeAsync( team.Member );
            Details frame = await team.DetailsAsync( team.Member, projects );
            Details open = await team.DetailsAsync( team.Member, hackathon );
            using HttpResponseMessage edit = await team.Member.Client.PutAsJsonAsync( $"/api/items/{projects}", new { title = "Моё" }, Cancellation );
            using HttpResponseMessage child = await team.Member.Client.PostAsJsonAsync( "/api/items", new { parentId = projects, kind = "event", title = "Внутрь рамки" }, Cancellation );

            Assert.True( seen.Single( i => i.Id == projects ).IsFrame );
            Assert.DoesNotContain( seen, i => i.Id == other );
            Assert.True( frame.Item.IsFrame );
            Assert.Equal( string.Empty, frame.DescriptionMd );
            Assert.Equal( [ hackathon ], frame.Children.Select( c => c.Id ) );
            Assert.True( open.Path.Single().IsFrame );
            Assert.Equal( [ jury ], open.RoleIds );
            Assert.Equal( HttpStatusCode.NotFound, edit.StatusCode );
            Assert.Equal( HttpStatusCode.NotFound, child.StatusCode );
        }

        [Fact]
        public async Task AccessFromSeveralRolesAddsUp()
        {
            await using Team team = await Team.CreateAsync( api );
            Guid jury = await team.RoleForMemberAsync( "Жюри" );
            Guid mentors = await team.RoleForMemberAsync( "Менторы", alsoKeep: [ jury ] );
            Guid judging = await team.CreateAsync( team.Head, new { workspaceId = team.Id, kind = "event", title = "Судейство", startDate = "2026-11-14", endDate = "2026-11-14", roleIds = new[] { jury } } );
            Guid mentoring = await team.CreateAsync( team.Head, new { workspaceId = team.Id, kind = "event", title = "Менторство", startDate = "2026-11-15", endDate = "2026-11-15", roleIds = new[] { mentors } } );
            await team.EnableRolesAsync();

            Assert.Equal( [ judging, mentoring ], ( await team.RangeAsync( team.Member ) ).Select( i => i.Id ) );
        }

        [Fact]
        public async Task TurningRolesOffOrLosingTheRoleChangesWhatIsSeen()
        {
            await using Team team = await Team.CreateAsync( api );
            Guid jury = await team.RoleForMemberAsync( "Жюри" );
            await team.CreateAsync( team.Head, new { workspaceId = team.Id, kind = "event", title = "Судейство", startDate = "2026-11-14", endDate = "2026-11-14", roleIds = new[] { jury } } );
            await team.CreateAsync( team.Head, new { workspaceId = team.Id, kind = "event", title = "Секрет", startDate = "2026-11-15", endDate = "2026-11-15" } );
            await team.EnableRolesAsync();
            int withRole = ( await team.RangeAsync( team.Member ) ).Length;

            await team.SendAsync( team.Head, HttpMethod.Put, $"/api/workspaces/{team.Id}/members/{team.Member.Id}/roles", new { roleIds = Array.Empty<Guid>() } );
            int withoutRole = ( await team.RangeAsync( team.Member ) ).Length;
            await team.SendAsync( team.Head, HttpMethod.Put, $"/api/workspaces/{team.Id}/roles-enabled", new { enabled = false } );
            int rolesOff = ( await team.RangeAsync( team.Member ) ).Length;

            Assert.Equal( (1, 0, 2), (withRole, withoutRole, rolesOff) );
        }

        [Fact]
        public async Task WithRolesOnOnlyHeadsCreateTopLevelItems()
        {
            await using Team team = await Team.CreateAsync( api );
            Guid jury = await team.RoleForMemberAsync( "Жюри" );
            Guid hackathon = await team.CreateAsync( team.Head, new { workspaceId = team.Id, kind = "event", title = "Хакатон", roleIds = new[] { jury } } );
            await team.EnableRolesAsync();

            using HttpResponseMessage root = await team.Member.Client.PostAsJsonAsync( "/api/items", new { workspaceId = team.Id, kind = "event", title = "Своё" }, Cancellation );
            using HttpResponseMessage inside = await team.Member.Client.PostAsJsonAsync( "/api/items", new { parentId = hackathon, kind = "task", title = "Репозиторий" }, Cancellation );
            Guid task = ( await inside.Content.ReadFromJsonAsync<Details>( Cancellation ) )!.Item.Id;
            using HttpResponseMessage toTop = await team.Member.Client.PostAsJsonAsync( $"/api/items/{task}/move", new { parentId = ( Guid? )null }, Cancellation );

            Assert.Equal( "planning.root_needs_head", await CodeAsync( root ) );
            Assert.Equal( HttpStatusCode.Created, inside.StatusCode );
            Assert.Equal( HttpStatusCode.BadRequest, toTop.StatusCode );
        }

        [Fact]
        public async Task OnlyHeadOpensItemsAndOnlyToRolesOfTheirTeam()
        {
            await using Team team = await Team.CreateAsync( api );
            Guid jury = await team.RoleForMemberAsync( "Жюри" );
            Guid hackathon = await team.CreateAsync( team.Head, new { workspaceId = team.Id, kind = "event", title = "Хакатон" } );
            using HttpResponseMessage otherTeam = await team.Head.Client.PostAsJsonAsync( "/api/workspaces", new { name = "Другая" }, Cancellation );
            Guid otherId = ( await otherTeam.Content.ReadFromJsonAsync<JsonElement>( Cancellation ) ).GetProperty( "id" ).GetGuid();
            JsonElement withForeign = await team.SendAsync( team.Head, HttpMethod.Post, $"/api/workspaces/{otherId}/roles", new { name = "Чужая" } );
            Guid foreign = withForeign.GetProperty( "roles" )[ 0 ].GetProperty( "id" ).GetGuid();

            using HttpResponseMessage byMember = await team.Member.Client.PutAsJsonAsync( $"/api/items/{hackathon}/roles", new { roleIds = new[] { jury } }, Cancellation );
            using HttpResponseMessage foreignRole = await team.Head.Client.PutAsJsonAsync( $"/api/items/{hackathon}/roles", new { roleIds = new[] { foreign } }, Cancellation );
            Details opened = await team.SetItemRolesAsync( hackathon, jury );

            Assert.Equal( "planning.not_head", await CodeAsync( byMember ) );
            Assert.Equal( "planning.unknown_role", await CodeAsync( foreignRole ) );
            Assert.Equal( [ jury ], opened.RoleIds );
        }

        [Fact]
        public async Task ChildrenShowRolesInheritedFromAncestors()
        {
            await using Team team = await Team.CreateAsync( api );
            Guid jury = await team.RoleForMemberAsync( "Жюри" );
            Guid hackathon = await team.CreateAsync( team.Head, new { workspaceId = team.Id, kind = "event", title = "Хакатон", roleIds = new[] { jury } } );
            Guid day = await team.CreateAsync( team.Head, new { parentId = hackathon, kind = "group", title = "День 1" } );

            Details details = await team.DetailsAsync( team.Head, day );

            Assert.Empty( details.RoleIds );
            Assert.Equal( [ jury ], details.InheritedRoleIds );
        }

        [Fact]
        public async Task HeadSeesHowManyEventsNoRoleCanSee()
        {
            await using Team team = await Team.CreateAsync( api );
            Guid jury = await team.RoleForMemberAsync( "Жюри" );
            Guid projects = await team.CreateAsync( team.Head, new { workspaceId = team.Id, kind = "macro", title = "Проекты", roleIds = new[] { jury } } );
            await team.CreateAsync( team.Head, new { parentId = projects, kind = "event", title = "Открытое через предка" } );
            await team.CreateAsync( team.Head, new { workspaceId = team.Id, kind = "event", title = "Забытое", startDate = "2026-11-20", endDate = "2026-11-20" } );
            await team.CreateAsync( team.Head, new { workspaceId = team.Id, kind = "event", title = "Ещё одно" } );

            JsonElement summary = await team.Head.Client.GetFromJsonAsync<JsonElement>( $"/api/items/access-summary?workspaceId={team.Id}", Cancellation );
            using HttpResponseMessage byMember = await team.Member.Client.GetAsync( $"/api/items/access-summary?workspaceId={team.Id}", Cancellation );

            Assert.Equal( 2, summary.GetProperty( "unsharedEvents" ).GetInt32() );
            Assert.Equal( [ "Забытое", "Ещё одно" ], summary.GetProperty( "examples" ).EnumerateArray().Select( e => e.GetProperty( "title" ).GetString() ) );
            Assert.Equal( HttpStatusCode.Forbidden, byMember.StatusCode );
        }

        [Fact]
        public async Task LinksOfAFrameStayHidden()
        {
            await using Team team = await Team.CreateAsync( api );
            Guid jury = await team.RoleForMemberAsync( "Жюри" );
            Guid projects = await team.CreateAsync( team.Head, new { workspaceId = team.Id, kind = "macro", title = "Проекты" } );
            await team.CreateAsync( team.Head, new { parentId = projects, kind = "event", title = "Хакатон", roleIds = new[] { jury } } );
            using HttpResponseMessage added = await team.Head.Client.PostAsJsonAsync( $"/api/items/{projects}/links", new { url = "https://docs.google.com/spreadsheets/d/secret" }, Cancellation );
            await team.EnableRolesAsync();

            Details frame = await team.DetailsAsync( team.Member, projects );
            using HttpResponseMessage addToFrame = await team.Member.Client.PostAsJsonAsync( $"/api/items/{projects}/links", new { url = "https://example.com" }, Cancellation );

            Assert.Equal( HttpStatusCode.Created, added.StatusCode );
            Assert.Empty( frame.Links );
            Assert.Equal( HttpStatusCode.NotFound, addToFrame.StatusCode );
        }

        private static async Task<string?> CodeAsync( HttpResponseMessage response )
        {
            return ( await response.Content.ReadFromJsonAsync<JsonElement>( Cancellation ) ).GetProperty( "code" ).GetString();
        }

        private sealed record Item( Guid Id, string Title, bool IsFrame );

        private sealed record Link( Guid Id );

        private sealed record Details( Item Item, string DescriptionMd, Item[] Path, Item[] Children, Link[] Links, Guid[] RoleIds, Guid[] InheritedRoleIds );

        /// <summary>Команда из главы и одного участника, вступившего по приглашению.</summary>
        private sealed class Team : IAsyncDisposable
        {
            private Team( SignedInUser head, SignedInUser member, Guid id )
            {
                Head = head;
                Member = member;
                Id = id;
            }

            public SignedInUser Head { get; }

            public SignedInUser Member { get; }

            public Guid Id { get; }

            public static async Task<Team> CreateAsync( PostgresApiFactory api )
            {
                SignedInUser head = await TestUsers.SignUpAsync( api, "Глава" );
                SignedInUser member = await TestUsers.SignUpAsync( api, "Участник" );
                using HttpResponseMessage created = await head.Client.PostAsJsonAsync( "/api/workspaces", new { name = "СКБ" }, Cancellation );
                Guid id = ( await created.Content.ReadFromJsonAsync<JsonElement>( Cancellation ) ).GetProperty( "id" ).GetGuid();
                using HttpResponseMessage invited = await head.Client.PostAsync( $"/api/workspaces/{id}/invitation", null, Cancellation );
                string token = ( await invited.Content.ReadFromJsonAsync<JsonElement>( Cancellation ) ).GetProperty( "token" ).GetString()!;
                using HttpResponseMessage accepted = await member.Client.PostAsync( $"/api/invitations/{token}/accept", null, Cancellation );
                accepted.EnsureSuccessStatusCode();
                return new Team( head, member, id );
            }

            public async Task<Guid> RoleForMemberAsync( string name, Guid[]? alsoKeep = null )
            {
                JsonElement details = await SendAsync( Head, HttpMethod.Post, $"/api/workspaces/{Id}/roles", new { name } );
                Guid roleId = details.GetProperty( "roles" ).EnumerateArray().Single( r => r.GetProperty( "name" ).GetString() == name ).GetProperty( "id" ).GetGuid();
                await SendAsync( Head, HttpMethod.Put, $"/api/workspaces/{Id}/members/{Member.Id}/roles", new { roleIds = ( alsoKeep ?? [] ).Append( roleId ).ToArray() } );
                return roleId;
            }

            public Task EnableRolesAsync()
            {
                return SendAsync( Head, HttpMethod.Put, $"/api/workspaces/{Id}/roles-enabled", new { enabled = true } );
            }

            public async Task<Guid> CreateAsync( SignedInUser user, object body )
            {
                using HttpResponseMessage response = await user.Client.PostAsJsonAsync( "/api/items", body, Cancellation );
                Assert.Equal( HttpStatusCode.Created, response.StatusCode );
                return ( await response.Content.ReadFromJsonAsync<Details>( Cancellation ) )!.Item.Id;
            }

            public async Task<Details> SetItemRolesAsync( Guid itemId, params Guid[] roleIds )
            {
                using HttpResponseMessage response = await Head.Client.PutAsJsonAsync( $"/api/items/{itemId}/roles", new { roleIds }, Cancellation );
                Assert.Equal( HttpStatusCode.OK, response.StatusCode );
                return ( await response.Content.ReadFromJsonAsync<Details>( Cancellation ) )!;
            }

            public async Task<Item[]> RangeAsync( SignedInUser user )
            {
                return ( await user.Client.GetFromJsonAsync<Item[]>( $"/api/items?from=2026-09-01&to=2026-12-31&workspaceId={Id}", Cancellation ) )!;
            }

            public async Task<Details> DetailsAsync( SignedInUser user, Guid itemId )
            {
                return ( await user.Client.GetFromJsonAsync<Details>( $"/api/items/{itemId}", Cancellation ) )!;
            }

            public async Task<JsonElement> SendAsync( SignedInUser user, HttpMethod method, string url, object body )
            {
                using HttpRequestMessage request = new( method, url ) { Content = JsonContent.Create( body ) };
                using HttpResponseMessage response = await user.Client.SendAsync( request, Cancellation );
                Assert.Equal( HttpStatusCode.OK, response.StatusCode );
                return await response.Content.ReadFromJsonAsync<JsonElement>( Cancellation );
            }

            public ValueTask DisposeAsync()
            {
                Head.Dispose();
                Member.Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
