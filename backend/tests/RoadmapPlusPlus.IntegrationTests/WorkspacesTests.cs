using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RoadmapPlusPlus.Modules.Identity.Contracts;

namespace RoadmapPlusPlus.IntegrationTests
{
    public sealed class WorkspacesTests( PostgresApiFactory api )
    {
        private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

        [Fact]
        public async Task CreatorSeesTeamAsHead()
        {
            using SignedInUser head = await TestUsers.SignUpAsync( api, "Ярослав" );

            Details created = await CreateAsync( head, "СКБ" );
            Summary[] mine = ( await head.Client.GetFromJsonAsync<Summary[]>( "/api/workspaces", Cancellation ) )!;

            Assert.True( created.IsHead );
            Member creator = Assert.Single( created.Members );
            Assert.Equal( head.Id, creator.UserId );
            Assert.Equal( "Ярослав", creator.DisplayName );
            Summary summary = Assert.Single( mine );
            Assert.Equal( (created.Id, "СКБ", true, 1), (summary.Id, summary.Name, summary.IsHead, summary.MemberCount) );
        }

        [Fact]
        public async Task OutsiderCannotSeeTheTeam()
        {
            using SignedInUser head = await TestUsers.SignUpAsync( api );
            using SignedInUser outsider = await TestUsers.SignUpAsync( api );
            Details team = await CreateAsync( head, "СКБ" );

            using HttpResponseMessage response = await outsider.Client.GetAsync( $"/api/workspaces/{team.Id}", Cancellation );

            Assert.Equal( HttpStatusCode.NotFound, response.StatusCode );
            Assert.Equal( "identity.workspace_not_found", await CodeAsync( response ) );
        }

        [Fact]
        public async Task InvitedUserJoinsAsMember()
        {
            using SignedInUser head = await TestUsers.SignUpAsync( api );
            using SignedInUser newcomer = await TestUsers.SignUpAsync( api, "Анастасия" );
            Details team = await CreateAsync( head, "Хакатон" );
            Invitation invitation = await InviteAsync( head, team.Id );

            Preview preview = ( await newcomer.Client.GetFromJsonAsync<Preview>( $"/api/invitations/{invitation.Token}", Cancellation ) )!;
            using HttpResponseMessage accepted = await newcomer.Client.PostAsync( $"/api/invitations/{invitation.Token}/accept", null, Cancellation );
            Details details = ( await newcomer.Client.GetFromJsonAsync<Details>( $"/api/workspaces/{team.Id}", Cancellation ) )!;

            Assert.Equal( ("Хакатон", 1, false), (preview.WorkspaceName, preview.MemberCount, preview.AlreadyMember) );
            Assert.Equal( HttpStatusCode.OK, accepted.StatusCode );
            Assert.False( details.IsHead );
            Assert.Equal( 2, details.Members.Length );
            Assert.Contains( details.Members, m => m.UserId == newcomer.Id && !m.IsHead && m.DisplayName == "Анастасия" );
        }

        [Fact]
        public async Task NewInvitationRevokesTheOldOne()
        {
            using SignedInUser head = await TestUsers.SignUpAsync( api );
            using SignedInUser newcomer = await TestUsers.SignUpAsync( api );
            Details team = await CreateAsync( head, "СКБ" );
            Invitation old = await InviteAsync( head, team.Id );

            Invitation fresh = await InviteAsync( head, team.Id );
            using HttpResponseMessage withOld = await newcomer.Client.PostAsync( $"/api/invitations/{old.Token}/accept", null, Cancellation );
            InvitationState? current = await head.Client.GetFromJsonAsync<InvitationState>( $"/api/workspaces/{team.Id}/invitation", Cancellation );

            Assert.Equal( HttpStatusCode.NotFound, withOld.StatusCode );
            Assert.Equal( "identity.invitation_not_found", await CodeAsync( withOld ) );
            Assert.Equal( fresh.Token, current?.Active?.Token );
        }

        [Fact]
        public async Task OnlyHeadManagesTheTeam()
        {
            using SignedInUser head = await TestUsers.SignUpAsync( api );
            using SignedInUser member = await TestUsers.SignUpAsync( api );
            Details team = await JoinedTeamAsync( head, member );

            using HttpResponseMessage rename = await member.Client.PatchAsJsonAsync( $"/api/workspaces/{team.Id}", new { name = "Моя", color = "#000000" }, Cancellation );
            using HttpResponseMessage invite = await member.Client.PostAsync( $"/api/workspaces/{team.Id}/invitation", null, Cancellation );
            using HttpResponseMessage kick = await member.Client.DeleteAsync( $"/api/workspaces/{team.Id}/members/{head.Id}", Cancellation );

            Assert.Equal( HttpStatusCode.Forbidden, rename.StatusCode );
            Assert.Equal( HttpStatusCode.Forbidden, invite.StatusCode );
            Assert.Equal( HttpStatusCode.Forbidden, kick.StatusCode );
            Assert.Equal( "identity.not_head", await CodeAsync( rename ) );
        }

        [Fact]
        public async Task LastHeadLeavesOnlyAfterAppointingAnother()
        {
            using SignedInUser head = await TestUsers.SignUpAsync( api );
            using SignedInUser member = await TestUsers.SignUpAsync( api );
            Details team = await JoinedTeamAsync( head, member );

            using HttpResponseMessage blocked = await head.Client.DeleteAsync( $"/api/workspaces/{team.Id}/members/{head.Id}", Cancellation );
            using HttpResponseMessage appointed = await head.Client.PatchAsJsonAsync( $"/api/workspaces/{team.Id}/members/{member.Id}", new { isHead = true }, Cancellation );
            using HttpResponseMessage left = await head.Client.DeleteAsync( $"/api/workspaces/{team.Id}/members/{head.Id}", Cancellation );
            Summary[] memberTeams = ( await member.Client.GetFromJsonAsync<Summary[]>( "/api/workspaces", Cancellation ) )!;

            Assert.Equal( HttpStatusCode.Conflict, blocked.StatusCode );
            Assert.Equal( "identity.last_head", await CodeAsync( blocked ) );
            Assert.Equal( HttpStatusCode.OK, appointed.StatusCode );
            Assert.Equal( HttpStatusCode.NoContent, left.StatusCode );
            Summary remaining = Assert.Single( memberTeams );
            Assert.Equal( (true, 1), (remaining.IsHead, remaining.MemberCount) );
        }

        [Fact]
        public async Task RemovedMemberLosesAccess()
        {
            using SignedInUser head = await TestUsers.SignUpAsync( api );
            using SignedInUser member = await TestUsers.SignUpAsync( api );
            Details team = await JoinedTeamAsync( head, member );

            using HttpResponseMessage removed = await head.Client.DeleteAsync( $"/api/workspaces/{team.Id}/members/{member.Id}", Cancellation );
            using HttpResponseMessage afterwards = await member.Client.GetAsync( $"/api/workspaces/{team.Id}", Cancellation );

            Assert.Equal( HttpStatusCode.NoContent, removed.StatusCode );
            Assert.Equal( HttpStatusCode.NotFound, afterwards.StatusCode );
        }

        [Fact]
        public async Task EmptyNameIsRejected()
        {
            using SignedInUser user = await TestUsers.SignUpAsync( api );

            using HttpResponseMessage response = await user.Client.PostAsJsonAsync( "/api/workspaces", new { name = "  ", color = "red" }, Cancellation );
            JsonElement errors = ( await ProblemAsync( response ) ).GetProperty( "errors" );

            Assert.Equal( HttpStatusCode.BadRequest, response.StatusCode );
            Assert.True( errors.TryGetProperty( "name", out _ ) );
            Assert.True( errors.TryGetProperty( "color", out _ ) );
        }

        [Fact]
        public async Task TeamsRequireSignIn()
        {
            using HttpClient guest = api.CreateClient();

            using HttpResponseMessage response = await guest.GetAsync( "/api/workspaces", Cancellation );

            Assert.Equal( HttpStatusCode.Unauthorized, response.StatusCode );
        }

        [Fact]
        public async Task MembershipsContractReportsTeamsAndHeads()
        {
            using SignedInUser head = await TestUsers.SignUpAsync( api );
            using SignedInUser member = await TestUsers.SignUpAsync( api );
            Details team = await JoinedTeamAsync( head, member );

            await using AsyncServiceScope scope = api.Services.CreateAsyncScope();
            IWorkspaceMemberships memberships = scope.ServiceProvider.GetRequiredService<IWorkspaceMemberships>();

            WorkspaceMembership? headMembership = await memberships.FindAsync( head.Id, team.Id, Cancellation );
            IReadOnlyList<WorkspaceMembership> memberTeams = await memberships.ListAsync( member.Id, Cancellation );
            WorkspaceMembership? stranger = await memberships.FindAsync( Guid.NewGuid(), team.Id, Cancellation );

            Assert.Equal( new WorkspaceMembership( team.Id, true ), headMembership );
            Assert.Equal( [ new WorkspaceMembership( team.Id, false ) ], memberTeams );
            Assert.Null( stranger );
        }

        private async Task<Details> JoinedTeamAsync( SignedInUser head, SignedInUser member )
        {
            Details team = await CreateAsync( head, "СКБ" );
            Invitation invitation = await InviteAsync( head, team.Id );
            using HttpResponseMessage accepted = await member.Client.PostAsync( $"/api/invitations/{invitation.Token}/accept", null, Cancellation );
            accepted.EnsureSuccessStatusCode();
            return team;
        }

        private static async Task<Details> CreateAsync( SignedInUser user, string name )
        {
            using HttpResponseMessage response = await user.Client.PostAsJsonAsync( "/api/workspaces", new { name }, Cancellation );
            Assert.Equal( HttpStatusCode.Created, response.StatusCode );
            return ( await response.Content.ReadFromJsonAsync<Details>( Cancellation ) )!;
        }

        private static async Task<Invitation> InviteAsync( SignedInUser head, Guid workspaceId )
        {
            using HttpResponseMessage response = await head.Client.PostAsync( $"/api/workspaces/{workspaceId}/invitation", null, Cancellation );
            response.EnsureSuccessStatusCode();
            return ( await response.Content.ReadFromJsonAsync<Invitation>( Cancellation ) )!;
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

        private sealed record Summary( Guid Id, string Name, string Color, bool IsHead, int MemberCount );

        private sealed record Member( Guid UserId, string DisplayName, string Email, string Color, bool IsHead );

        private sealed record Details( Guid Id, string Name, string Color, bool IsHead, Member[] Members );

        private sealed record Invitation( string Token, DateTimeOffset ExpiresAt );

        private sealed record InvitationState( Invitation? Active );

        private sealed record Preview( Guid WorkspaceId, string WorkspaceName, string Color, int MemberCount, bool AlreadyMember );
    }
}
