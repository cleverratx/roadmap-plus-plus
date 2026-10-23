using RoadmapPlusPlus.BuildingBlocks;
using RoadmapPlusPlus.Modules.Identity.Domain.Workspaces;

namespace RoadmapPlusPlus.Modules.Identity.Tests
{
    public sealed class WorkspaceTests
    {
        private static readonly DateTimeOffset Now = new( 2026, 10, 20, 12, 0, 0, TimeSpan.Zero );

        private readonly Guid _head = Guid.NewGuid();

        private readonly Guid _member = Guid.NewGuid();

        [Fact]
        public void CreatorBecomesTheOnlyHead()
        {
            Workspace workspace = Workspace.Create( "СКБ", "#3b5bdb", _head, Now );

            Member creator = Assert.Single( workspace.Members );
            Assert.Equal( _head, creator.UserId );
            Assert.True( creator.IsHead );
        }

        [Fact]
        public void JoiningTwiceKeepsOneMembership()
        {
            Workspace workspace = WithMember();

            Assert.False( workspace.AddMember( _member, Now ) );
            Assert.Equal( 2, workspace.Members.Count );
            Assert.False( workspace.FindMember( _member )!.IsHead );
        }

        [Fact]
        public void OnlyHeadCanRename()
        {
            Workspace workspace = WithMember();

            Assert.Same( WorkspaceErrors.NotHead, workspace.Update( _member, "Другое", "#000000" ).Error );
            Assert.True( workspace.Update( _head, "Другое", "#000000" ).IsSuccess );
            Assert.Equal( "Другое", workspace.Name );
        }

        [Fact]
        public void LastHeadCannotLeaveOrStepDown()
        {
            Workspace workspace = WithMember();

            Assert.Same( WorkspaceErrors.LastHead, workspace.RemoveMember( _head, _head ).Error );
            Assert.Same( WorkspaceErrors.LastHead, workspace.SetHead( _head, _head, false ).Error );
        }

        [Fact]
        public void HeadCanLeaveAfterAppointingAnotherHead()
        {
            Workspace workspace = WithMember();

            Assert.True( workspace.SetHead( _head, _member, true ).IsSuccess );
            Assert.True( workspace.RemoveMember( _head, _head ).IsSuccess );

            Member left = Assert.Single( workspace.Members );
            Assert.Equal( _member, left.UserId );
            Assert.True( left.IsHead );
        }

        [Fact]
        public void MemberCanLeaveButCannotRemoveOthers()
        {
            Workspace workspace = WithMember();

            Assert.Same( WorkspaceErrors.NotHead, workspace.RemoveMember( _member, _head ).Error );
            Assert.True( workspace.RemoveMember( _member, _member ).IsSuccess );
            Assert.Single( workspace.Members );
        }

        [Fact]
        public void MemberCannotAppointHeads()
        {
            Workspace workspace = WithMember();

            Assert.Same( WorkspaceErrors.NotHead, workspace.SetHead( _member, _member, true ).Error );
        }

        [Fact]
        public void UnknownMemberIsReported()
        {
            Workspace workspace = WithMember();

            Assert.Same( WorkspaceErrors.MemberNotFound, workspace.RemoveMember( _head, Guid.NewGuid() ).Error );
        }

        [Fact]
        public void EveryChangeOfMembershipBumpsVersion()
        {
            Workspace workspace = Workspace.Create( "СКБ", "#3b5bdb", _head, Now );
            int created = workspace.Version;

            workspace.AddMember( _member, Now );
            int joined = workspace.Version;
            workspace.SetHead( _head, _member, true );
            int appointed = workspace.Version;
            Result failed = workspace.RemoveMember( Guid.NewGuid(), _member );

            Assert.True( created < joined && joined < appointed );
            Assert.False( failed.IsSuccess );
            Assert.Equal( appointed, workspace.Version );
        }

        [Fact]
        public void InvitationWorksUntilItExpiresOrIsRevoked()
        {
            Invitation invitation = Invitation.Create( Guid.NewGuid(), _head, Now );

            Assert.True( invitation.IsActive( Now.AddDays( 6 ) ) );
            Assert.False( invitation.IsActive( Now + Invitation.Lifetime ) );

            invitation.Revoke( Now.AddHours( 1 ) );
            Assert.False( invitation.IsActive( Now.AddHours( 2 ) ) );
        }

        [Fact]
        public void InvitationTokensAreUnguessable()
        {
            Invitation first = Invitation.Create( Guid.NewGuid(), _head, Now );
            Invitation second = Invitation.Create( Guid.NewGuid(), _head, Now );

            Assert.Equal( 32, first.Token.Length );
            Assert.NotEqual( first.Token, second.Token );
        }

        private Workspace WithMember()
        {
            Workspace workspace = Workspace.Create( "СКБ", "#3b5bdb", _head, Now );
            workspace.AddMember( _member, Now );
            return workspace;
        }
    }
}
