using RoadmapPlusPlus.BuildingBlocks;
using RoadmapPlusPlus.Modules.Planning.Domain.Items;

namespace RoadmapPlusPlus.Modules.Planning.Tests
{
    public sealed class ItemTests
    {
        private static readonly DateTimeOffset Now = new( 2026, 11, 4, 12, 0, 0, TimeSpan.Zero );

        private static readonly Guid Workspace = Guid.NewGuid();

        public static TheoryData<string, string, bool> Nesting { get; } = new()
        {
            { nameof( ItemKind.Macro ), nameof( ItemKind.Macro ), true },
            { nameof( ItemKind.Macro ), nameof( ItemKind.Event ), true },
            { nameof( ItemKind.Macro ), nameof( ItemKind.Group ), false },
            { nameof( ItemKind.Macro ), nameof( ItemKind.Task ), false },
            { nameof( ItemKind.Event ), nameof( ItemKind.Macro ), false },
            { nameof( ItemKind.Event ), nameof( ItemKind.Event ), false },
            { nameof( ItemKind.Event ), nameof( ItemKind.Group ), true },
            { nameof( ItemKind.Event ), nameof( ItemKind.Task ), true },
            { nameof( ItemKind.Group ), nameof( ItemKind.Group ), true },
            { nameof( ItemKind.Group ), nameof( ItemKind.Task ), true },
            { nameof( ItemKind.Group ), nameof( ItemKind.Event ), false },
            { nameof( ItemKind.Task ), nameof( ItemKind.Task ), false },
        };

        [Theory]
        [MemberData( nameof( Nesting ) )]
        public void NestingFollowsTheTable( string parentName, string childName, bool allowed )
        {
            ItemKind parent = Enum.Parse<ItemKind>( parentName );
            ItemKind child = Enum.Parse<ItemKind>( childName );
            Item parentItem = Root( parent == ItemKind.Macro || parent == ItemKind.Event ? parent : ItemKind.Event );
            if ( parent is ItemKind.Group or ItemKind.Task )
            {
                parentItem = Item.CreateChild( parentItem, parent, Content(), 0, Now ).Value;
            }

            Result<Item> created = Item.CreateChild( parentItem, child, Content(), 0, Now );

            Assert.Equal( allowed, created.IsSuccess );
            if ( !allowed )
            {
                Assert.Equal( "planning.invalid_nesting", created.Error?.Code );
            }
        }

        [Theory]
        [InlineData( nameof( ItemKind.Macro ), true )]
        [InlineData( nameof( ItemKind.Event ), true )]
        [InlineData( nameof( ItemKind.Group ), false )]
        [InlineData( nameof( ItemKind.Task ), false )]
        public void OnlyMacroAndEventLiveAtTheTop( string kindName, bool allowed )
        {
            Assert.Equal( allowed, Item.CreateRoot( Workspace, Enum.Parse<ItemKind>( kindName ), Content(), 0, Now ).IsSuccess );
        }

        [Fact]
        public void ChildTakesWorkspaceFromParent()
        {
            Item parent = Root( ItemKind.Event );

            Item child = Item.CreateChild( parent, ItemKind.Task, Content(), 3, Now ).Value;

            Assert.Equal( Workspace, child.WorkspaceId );
            Assert.Equal( parent.Id, child.ParentId );
            Assert.Equal( 3, child.Position );
        }

        [Fact]
        public void TitleIsTrimmedAndRequired()
        {
            Assert.Equal( "Хакатон", ItemContent.Create( "  Хакатон ", null, null, null ).Value.Title );
            Assert.True( ItemContent.Create( "   ", null, null, null ).Error!.Fields.ContainsKey( "title" ) );
        }

        [Fact]
        public void DatesComeTogetherAndInOrder()
        {
            DateOnly first = new( 2026, 9, 28 );
            DateOnly second = new( 2026, 9, 29 );

            Assert.True( ItemContent.Create( "Хакатон", first, second, null ).IsSuccess );
            Assert.True( ItemContent.Create( "Один день", first, first, null ).IsSuccess );
            Assert.True( ItemContent.Create( "Без дат", null, null, null ).IsSuccess );
            Assert.True( ItemContent.Create( "Только начало", first, null, null ).Error!.Fields.ContainsKey( "endDate" ) );
            Assert.True( ItemContent.Create( "Наоборот", second, first, null ).Error!.Fields.ContainsKey( "endDate" ) );
        }

        [Fact]
        public void UpdateChangesContentAndTime()
        {
            Item item = Root( ItemKind.Event );
            DateTimeOffset later = Now.AddHours( 1 );

            item.Update( ItemContent.Create( "Новое", new DateOnly( 2026, 12, 1 ), new DateOnly( 2026, 12, 3 ), "ТЗ" ).Value, later );

            Assert.Equal( ("Новое", "ТЗ", later), (item.Title, item.DescriptionMd, item.UpdatedAt) );
            Assert.Equal( new DateOnly( 2026, 12, 3 ), item.EndDate );
        }

        private static Item Root( ItemKind kind )
        {
            return Item.CreateRoot( Workspace, kind, Content(), 0, Now ).Value;
        }

        private static ItemContent Content()
        {
            return ItemContent.Create( "Узел", null, null, null ).Value;
        }
    }
}
