using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RoadmapPlusPlus.Modules.Identity.Contracts;

namespace RoadmapPlusPlus.IntegrationTests
{
    public sealed class RolesTests( PostgresApiFactory api )
    {
        private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

        [Fact]
        public async Task HeadCreatesRolesAndGivesThemToMembers()
        {
            using SignedInUser head = await TestUsers.SignUpAsync( api );
            using SignedInUser member = await TestUsers.SignUpAsync( api );
            Guid team = await JoinedTeamAsync( head, member );

            Details withJury = await SendAsync( head, HttpMethod.Post, $"/api/workspaces/{team}/roles", new { name = "Жюри", color = "#7048e8" } );
            Details withMentors = await SendAsync( head, HttpMethod.Post, $"/api/workspaces/{team}/roles", new { name = "Менторы" } );
            Guid jury = withJury.Roles.Single().Id;
            Guid mentors = withMentors.Roles.Single( r => r.Name == "Менторы" ).Id;
            Details assigned = await SendAsync( head, HttpMethod.Put, $"/api/workspaces/{team}/members/{member.Id}/roles", new { roleIds = new[] { jury, mentors } } );
            Details seenByMember = ( await member.Client.GetFromJsonAsync<Details>( $"/api/workspaces/{team}", Cancellation ) )!;

            Assert.Equal( [ "Жюри", "Менторы" ], assigned.Roles.Select( r => r.Name ) );
            Assert.Equal( "#7048e8", assigned.Roles[ 0 ].Color );
            Assert.Matches( "^#[0-9a-f]{6}$", assigned.Roles[ 1 ].Color );
            Assert.Equal( [ 1, 1 ], assigned.Roles.Select( r => r.MemberCount ) );
            Assert.Equal( new[] { jury, mentors }.Order(), seenByMember.Members.Single( m => m.UserId == member.Id ).RoleIds.Order() );
            Assert.Equal( 2, seenByMember.Roles.Length );
        }

        [Fact]
        public async Task FlagIsStoredAndReachesOtherModules()
        {
            using SignedInUser head = await TestUsers.SignUpAsync( api );
            using SignedInUser member = await TestUsers.SignUpAsync( api );
            Guid team = await JoinedTeamAsync( head, member );
            Details withRole = await SendAsync( head, HttpMethod.Post, $"/api/workspaces/{team}/roles", new { name = "Жюри" } );
            Guid jury = withRole.Roles.Single().Id;
            await SendAsync( head, HttpMethod.Put, $"/api/workspaces/{team}/members/{member.Id}/roles", new { roleIds = new[] { jury } } );

            Details enabled = await SendAsync( head, HttpMethod.Put, $"/api/workspaces/{team}/roles-enabled", new { enabled = true } );

            await using AsyncServiceScope scope = api.Services.CreateAsyncScope();
            WorkspaceMembership membership = ( await scope.ServiceProvider.GetRequiredService<IWorkspaceMemberships>().FindAsync( member.Id, team, Cancellation ) )!;
            Assert.True( enabled.RolesEnabled );
            Assert.Equal( (true, false), (membership.RolesEnabled, membership.IsHead) );
            Assert.Equal( [ jury ], membership.RoleIds );
        }

        [Fact]
        public async Task MemberCannotManageRoles()
        {
            using SignedInUser head = await TestUsers.SignUpAsync( api );
            using SignedInUser member = await TestUsers.SignUpAsync( api );
            Guid team = await JoinedTeamAsync( head, member );

            using HttpResponseMessage flag = await member.Client.PutAsJsonAsync( $"/api/workspaces/{team}/roles-enabled", new { enabled = true }, Cancellation );
            using HttpResponseMessage role = await member.Client.PostAsJsonAsync( $"/api/workspaces/{team}/roles", new { name = "Я главный" }, Cancellation );

            Assert.Equal( HttpStatusCode.Forbidden, flag.StatusCode );
            Assert.Equal( "identity.not_head", await CodeAsync( role ) );
        }

        [Fact]
        public async Task RoleNamesAreValidated()
        {
            using SignedInUser head = await TestUsers.SignUpAsync( api );
            Guid team = await CreateTeamAsync( head );
            await SendAsync( head, HttpMethod.Post, $"/api/workspaces/{team}/roles", new { name = "Жюри" } );

            using HttpResponseMessage empty = await head.Client.PostAsJsonAsync( $"/api/workspaces/{team}/roles", new { name = "  " }, Cancellation );
            using HttpResponseMessage duplicate = await head.Client.PostAsJsonAsync( $"/api/workspaces/{team}/roles", new { name = "жюри" }, Cancellation );
            using HttpResponseMessage badColor = await head.Client.PostAsJsonAsync( $"/api/workspaces/{team}/roles", new { name = "Менторы", color = "red" }, Cancellation );

            Assert.True( ( await ProblemAsync( empty ) ).GetProperty( "errors" ).TryGetProperty( "name", out _ ) );
            Assert.Equal( "identity.role_name_taken", await CodeAsync( duplicate ) );
            Assert.True( ( await ProblemAsync( badColor ) ).GetProperty( "errors" ).TryGetProperty( "color", out _ ) );
        }

        [Fact]
        public async Task RenamedRoleKeepsItsHolders()
        {
            using SignedInUser head = await TestUsers.SignUpAsync( api );
            Guid team = await CreateTeamAsync( head );
            Guid jury = ( await SendAsync( head, HttpMethod.Post, $"/api/workspaces/{team}/roles", new { name = "Жюри" } ) ).Roles.Single().Id;
            await SendAsync( head, HttpMethod.Put, $"/api/workspaces/{team}/members/{head.Id}/roles", new { roleIds = new[] { jury } } );

            Details renamed = await SendAsync( head, HttpMethod.Patch, $"/api/workspaces/{team}/roles/{jury}", new { name = "Судьи", color = "#c2255c" } );

            Assert.Equal( ("Судьи", "#c2255c", 1), (renamed.Roles[ 0 ].Name, renamed.Roles[ 0 ].Color, renamed.Roles[ 0 ].MemberCount) );
        }

        [Fact]
        public async Task DeletedRoleIsTakenFromMembers()
        {
            using SignedInUser head = await TestUsers.SignUpAsync( api );
            using SignedInUser member = await TestUsers.SignUpAsync( api );
            Guid team = await JoinedTeamAsync( head, member );
            Guid jury = ( await SendAsync( head, HttpMethod.Post, $"/api/workspaces/{team}/roles", new { name = "Жюри" } ) ).Roles.Single().Id;
            await SendAsync( head, HttpMethod.Put, $"/api/workspaces/{team}/members/{member.Id}/roles", new { roleIds = new[] { jury } } );

            Details afterDelete = await SendAsync( head, HttpMethod.Delete, $"/api/workspaces/{team}/roles/{jury}", null );
            Details reread = ( await head.Client.GetFromJsonAsync<Details>( $"/api/workspaces/{team}", Cancellation ) )!;

            Assert.Empty( afterDelete.Roles );
            Assert.All( reread.Members, m => Assert.Empty( m.RoleIds ) );
        }

        [Fact]
        public async Task RoleOfAnotherTeamCannotBeGiven()
        {
            using SignedInUser head = await TestUsers.SignUpAsync( api );
            Guid team = await CreateTeamAsync( head );
            Guid other = await CreateTeamAsync( head );
            Guid foreign = ( await SendAsync( head, HttpMethod.Post, $"/api/workspaces/{other}/roles", new { name = "Жюри" } ) ).Roles.Single().Id;

            using HttpResponseMessage response = await head.Client.PutAsJsonAsync( $"/api/workspaces/{team}/members/{head.Id}/roles", new { roleIds = new[] { foreign } }, Cancellation );

            Assert.Equal( "identity.role_not_found", await CodeAsync( response ) );
        }

        [Fact]
        public async Task LeavingTheTeamDropsTheRoles()
        {
            using SignedInUser head = await TestUsers.SignUpAsync( api );
            using SignedInUser member = await TestUsers.SignUpAsync( api );
            Guid team = await JoinedTeamAsync( head, member );
            Guid jury = ( await SendAsync( head, HttpMethod.Post, $"/api/workspaces/{team}/roles", new { name = "Жюри" } ) ).Roles.Single().Id;
            await SendAsync( head, HttpMethod.Put, $"/api/workspaces/{team}/members/{member.Id}/roles", new { roleIds = new[] { jury } } );

            using HttpResponseMessage left = await member.Client.DeleteAsync( $"/api/workspaces/{team}/members/{member.Id}", Cancellation );
            Details after = ( await head.Client.GetFromJsonAsync<Details>( $"/api/workspaces/{team}", Cancellation ) )!;

            Assert.Equal( HttpStatusCode.NoContent, left.StatusCode );
            Assert.Equal( 0, after.Roles.Single().MemberCount );
        }

        private async Task<Guid> JoinedTeamAsync( SignedInUser head, SignedInUser member )
        {
            Guid team = await CreateTeamAsync( head );
            using HttpResponseMessage invited = await head.Client.PostAsync( $"/api/workspaces/{team}/invitation", null, Cancellation );
            string token = ( await invited.Content.ReadFromJsonAsync<JsonElement>( Cancellation ) ).GetProperty( "token" ).GetString()!;
            using HttpResponseMessage accepted = await member.Client.PostAsync( $"/api/invitations/{token}/accept", null, Cancellation );
            accepted.EnsureSuccessStatusCode();
            return team;
        }

        private static async Task<Guid> CreateTeamAsync( SignedInUser user )
        {
            using HttpResponseMessage response = await user.Client.PostAsJsonAsync( "/api/workspaces", new { name = "СКБ" }, Cancellation );
            response.EnsureSuccessStatusCode();
            return ( await response.Content.ReadFromJsonAsync<JsonElement>( Cancellation ) ).GetProperty( "id" ).GetGuid();
        }

        private static async Task<Details> SendAsync( SignedInUser user, HttpMethod method, string url, object? body )
        {
            using HttpRequestMessage request = new( method, url ) { Content = body is null ? null : JsonContent.Create( body ) };
            using HttpResponseMessage response = await user.Client.SendAsync( request, Cancellation );
            Assert.Equal( HttpStatusCode.OK, response.StatusCode );
            return ( await response.Content.ReadFromJsonAsync<Details>( Cancellation ) )!;
        }

        private static async Task<string?> CodeAsync( HttpResponseMessage response )
        {
            return ( await ProblemAsync( response ) ).GetProperty( "code" ).GetString();
        }

        private static async Task<JsonElement> ProblemAsync( HttpResponseMessage response )
        {
            return await response.Content.ReadFromJsonAsync<JsonElement>( Cancellation );
        }

        private sealed record Role( Guid Id, string Name, string Color, int MemberCount );

        private sealed record Member( Guid UserId, Guid[] RoleIds );

        private sealed record Details( Guid Id, bool RolesEnabled, Role[] Roles, Member[] Members );
    }
}
