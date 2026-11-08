using RoadmapPlusPlus.BuildingBlocks;
using RoadmapPlusPlus.Modules.Planning.Domain.Items;

namespace RoadmapPlusPlus.Modules.Planning.Tests
{
    public sealed class ItemMoveTests
    {
        private static readonly DateTimeOffset Now = new( 2026, 11, 8, 12, 0, 0, TimeSpan.Zero );

        private static readonly Guid Workspace = Guid.NewGuid();

        [Fact]
        public void EventMovesIntoAnotherMacro()
        {
            Item study = Root( ItemKind.Macro );
            Item projects = Root( ItemKind.Macro );
            Item hackathon = Child( study, ItemKind.Event );
            DateTimeOffset later = Now.AddHours( 1 );

            Result moved = hackathon.MoveTo( projects, [ projects.Id ], later );

            Assert.True( moved.IsSuccess );
            Assert.Equal( (projects.Id, later), (hackathon.ParentId, hackathon.UpdatedAt) );
        }

        [Fact]
        public void EventMovesToTheTopAndGroupDoesNot()
        {
            Item macro = Root( ItemKind.Macro );
            Item hackathon = Child( macro, ItemKind.Event );
            Item day = Child( hackathon, ItemKind.Group );

            Result eventToTop = hackathon.MoveTo( null, [], Now );
            Result groupToTop = day.MoveTo( null, [], Now );

            Assert.True( eventToTop.IsSuccess );
            Assert.Null( hackathon.ParentId );
            Assert.Equal( "planning.invalid_nesting", groupToTop.Error?.Code );
            Assert.Equal( hackathon.Id, day.ParentId );
        }

        [Fact]
        public void ItemCannotGoInsideItselfOrItsDescendants()
        {
            Item hackathon = Root( ItemKind.Event );
            Item day = Child( hackathon, ItemKind.Group );
            Item morning = Child( day, ItemKind.Group );

            Result intoItself = day.MoveTo( day, [ hackathon.Id, day.Id ], Now );
            Result intoGrandchild = day.MoveTo( morning, [ hackathon.Id, day.Id, morning.Id ], Now );

            Assert.Equal( "planning.move_cycle", intoItself.Error?.Code );
            Assert.Equal( "planning.move_cycle", intoGrandchild.Error?.Code );
            Assert.Equal( hackathon.Id, day.ParentId );
        }

        [Fact]
        public void NestingRulesHoldForMoves()
        {
            Item macro = Root( ItemKind.Macro );
            Item hackathon = Root( ItemKind.Event );
            Item task = Child( hackathon, ItemKind.Task );

            Result taskIntoMacro = task.MoveTo( macro, [ macro.Id ], Now );

            Assert.Equal( "planning.invalid_nesting", taskIntoMacro.Error?.Code );
        }

        [Fact]
        public void ItemStaysInItsTeam()
        {
            Item hackathon = Root( ItemKind.Event );
            Item foreign = Item.CreateRoot( Guid.NewGuid(), ItemKind.Macro, Content(), 0, Now ).Value;

            Result moved = hackathon.MoveTo( foreign, [ foreign.Id ], Now );

            Assert.Equal( "planning.move_across_workspaces", moved.Error?.Code );
            Assert.Null( hackathon.ParentId );
        }

        [Fact]
        public void StayingUnderTheSameParentKeepsUpdatedAt()
        {
            Item hackathon = Root( ItemKind.Event );
            Item day = Child( hackathon, ItemKind.Group );

            day.MoveTo( hackathon, [ hackathon.Id ], Now.AddHours( 1 ) );

            Assert.Equal( Now, day.UpdatedAt );
        }

        private static Item Root( ItemKind kind )
        {
            return Item.CreateRoot( Workspace, kind, Content(), 0, Now ).Value;
        }

        private static Item Child( Item parent, ItemKind kind )
        {
            return Item.CreateChild( parent, kind, Content(), 0, Now ).Value;
        }

        private static ItemContent Content()
        {
            return ItemContent.Create( "Узел", null, null, null ).Value;
        }
    }
}
