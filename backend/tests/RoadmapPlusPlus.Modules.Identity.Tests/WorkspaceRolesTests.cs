using RoadmapPlusPlus.BuildingBlocks;
using RoadmapPlusPlus.Modules.Identity.Domain.Workspaces;

namespace RoadmapPlusPlus.Modules.Identity.Tests
{
    public sealed class WorkspaceRolesTests
    {
        private static readonly DateTimeOffset Now = new( 2026, 11, 14, 12, 0, 0, TimeSpan.Zero );

        private readonly Guid _head = Guid.NewGuid();

        private readonly Guid _member = Guid.NewGuid();

        [Fact]
        public void RolesAreOffUntilHeadTurnsThemOn()
        {
            Workspace workspace = WithMember();

            Assert.False( workspace.RolesEnabled );
            Assert.Same( WorkspaceErrors.NotHead, workspace.SetRolesEnabled( _member, true ).Error );
            Assert.True( workspace.SetRolesEnabled( _head, true ).IsSuccess );
            Assert.True( workspace.RolesEnabled );
        }

        [Fact]
        public void TurningRolesOffKeepsRolesAndAssignments()
        {
            Workspace workspace = WithMember();
            Role jury = workspace.AddRole( _head, "Жюри", "#7048e8", Now ).Value;
            workspace.SetMemberRoles( _head, _member, [ jury.Id ] );
            workspace.SetRolesEnabled( _head, true );

            workspace.SetRolesEnabled( _head, false );

            Assert.Single( workspace.Roles );
            Assert.Equal( [ jury.Id ], workspace.FindMember( _member )!.Roles.Select( r => r.RoleId ) );
        }

        [Fact]
        public void OnlyHeadManagesRoles()
        {
            Workspace workspace = WithMember();
            Role jury = workspace.AddRole( _head, "Жюри", "#7048e8", Now ).Value;

            Assert.Same( WorkspaceErrors.NotHead, workspace.AddRole( _member, "Менторы", "#1098ad", Now ).Error );
            Assert.Same( WorkspaceErrors.NotHead, workspace.UpdateRole( _member, jury.Id, "Судьи", "#1098ad" ).Error );
            Assert.Same( WorkspaceErrors.NotHead, workspace.RemoveRole( _member, jury.Id ).Error );
            Assert.Same( WorkspaceErrors.NotHead, workspace.SetMemberRoles( _member, _member, [ jury.Id ] ).Error );
        }

        [Fact]
        public void RoleNamesAreUniqueIgnoringCase()
        {
            Workspace workspace = WithMember();
            workspace.AddRole( _head, "Жюри", "#7048e8", Now );
            Role mentors = workspace.AddRole( _head, "Менторы", "#1098ad", Now ).Value;

            Assert.Same( WorkspaceErrors.RoleNameTaken, workspace.AddRole( _head, "жюри", "#000000", Now ).Error );
            Assert.Same( WorkspaceErrors.RoleNameTaken, workspace.UpdateRole( _head, mentors.Id, "ЖЮРИ", "#1098ad" ).Error );
            Assert.True( workspace.UpdateRole( _head, mentors.Id, "Менторы", "#e8590c" ).IsSuccess );
        }

        [Fact]
        public void MemberGetsExactlyTheGivenRoles()
        {
            Workspace workspace = WithMember();
            Role jury = workspace.AddRole( _head, "Жюри", "#7048e8", Now ).Value;
            Role mentors = workspace.AddRole( _head, "Менторы", "#1098ad", Now ).Value;
            Role design = workspace.AddRole( _head, "Дизайн", "#e8590c", Now ).Value;

            workspace.SetMemberRoles( _head, _member, [ jury.Id, mentors.Id ] );
            workspace.SetMemberRoles( _head, _member, [ mentors.Id, design.Id, design.Id ] );

            Assert.Equal( [ mentors.Id, design.Id ], workspace.FindMember( _member )!.Roles.Select( r => r.RoleId ).Order() );
        }

        [Fact]
        public void ForeignRoleCannotBeGiven()
        {
            Workspace workspace = WithMember();
            Workspace other = Workspace.Create( "Другая", "#000000", _head, Now );
            Role foreign = other.AddRole( _head, "Жюри", "#7048e8", Now ).Value;

            Result given = workspace.SetMemberRoles( _head, _member, [ foreign.Id ] );

            Assert.Same( WorkspaceErrors.RoleNotFound, given.Error );
            Assert.Empty( workspace.FindMember( _member )!.Roles );
        }

        [Fact]
        public void RemovedRoleIsTakenFromEveryone()
        {
            Workspace workspace = WithMember();
            Role jury = workspace.AddRole( _head, "Жюри", "#7048e8", Now ).Value;
            workspace.SetMemberRoles( _head, _member, [ jury.Id ] );
            workspace.SetMemberRoles( _head, _head, [ jury.Id ] );

            Assert.True( workspace.RemoveRole( _head, jury.Id ).IsSuccess );

            Assert.Empty( workspace.Roles );
            Assert.All( workspace.Members, m => Assert.Empty( m.Roles ) );
        }

        [Fact]
        public void EveryRoleChangeBumpsTheVersion()
        {
            Workspace workspace = WithMember();
            int before = workspace.Version;

            Role jury = workspace.AddRole( _head, "Жюри", "#7048e8", Now ).Value;
            workspace.SetMemberRoles( _head, _member, [ jury.Id ] );
            workspace.SetRolesEnabled( _head, true );
            workspace.SetRolesEnabled( _head, true );

            Assert.Equal( before + 3, workspace.Version );
        }

        private Workspace WithMember()
        {
            Workspace workspace = Workspace.Create( "СКБ", "#3b5bdb", _head, Now );
            workspace.AddMember( _member, Now );
            return workspace;
        }
    }
}
