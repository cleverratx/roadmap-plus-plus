using RoadmapPlusPlus.BuildingBlocks;
using RoadmapPlusPlus.Modules.Planning.Domain.Links;

namespace RoadmapPlusPlus.Modules.Planning.Tests
{
    public sealed class LinkTests
    {
        private static readonly DateTimeOffset Now = new( 2026, 11, 8, 12, 0, 0, TimeSpan.Zero );

        [Theory]
        [InlineData( "https://docs.google.com/spreadsheets/d/1AbC/edit#gid=0", nameof( LinkKind.GoogleSheet ) )]
        [InlineData( "https://docs.google.com/document/d/1AbC/edit", nameof( LinkKind.GoogleDoc ) )]
        [InlineData( "https://docs.google.com/presentation/d/1AbC/edit", nameof( LinkKind.GoogleSlides ) )]
        [InlineData( "https://docs.google.com/forms/d/1AbC/viewform", nameof( LinkKind.GoogleDrive ) )]
        [InlineData( "https://drive.google.com/drive/folders/1AbC", nameof( LinkKind.GoogleDrive ) )]
        [InlineData( "https://t.me/skb_hack/42", nameof( LinkKind.TgPost ) )]
        [InlineData( "https://t.me/s/skb_hack/42", nameof( LinkKind.TgPost ) )]
        [InlineData( "https://t.me/c/1234567890/42", nameof( LinkKind.TgPost ) )]
        [InlineData( "https://t.me/skb_hack", nameof( LinkKind.Telegram ) )]
        [InlineData( "https://github.com/skb/hackathon", nameof( LinkKind.Repo ) )]
        [InlineData( "https://gitlab.com/skb/hackathon", nameof( LinkKind.Repo ) )]
        [InlineData( "https://www.figma.com/design/AbC/Hackathon", nameof( LinkKind.Figma ) )]
        [InlineData( "https://skb.notion.site/Hackathon-123", nameof( LinkKind.Notion ) )]
        [InlineData( "https://miro.com/app/board/AbC=/", nameof( LinkKind.Miro ) )]
        [InlineData( "https://example.com/rules", nameof( LinkKind.Other ) )]
        [InlineData( "https://notgithub.com/skb", nameof( LinkKind.Other ) )]
        public void KindComesFromTheAddress( string url, string kindName )
        {
            Result<LinkTarget> target = LinkTarget.Parse( url );

            Assert.True( target.IsSuccess );
            Assert.Equal( Enum.Parse<LinkKind>( kindName ), target.Value.Kind );
        }

        [Fact]
        public void AddressWithoutSchemeBecomesHttps()
        {
            Result<LinkTarget> target = LinkTarget.Parse( "  t.me/skb_hack/42 " );

            Assert.Equal( "https://t.me/skb_hack/42", target.Value.Url );
            Assert.Equal( LinkKind.TgPost, target.Value.Kind );
        }

        [Theory]
        [InlineData( "" )]
        [InlineData( "javascript:alert(1)" )]
        [InlineData( "data:text/html,hello" )]
        [InlineData( "ftp://files.example.com/plan.xlsx" )]
        [InlineData( "просто текст" )]
        [InlineData( "localhost" )]
        public void OnlyWebAddressesAreAccepted( string url )
        {
            Result<LinkTarget> target = LinkTarget.Parse( url );

            Assert.False( target.IsSuccess );
            Assert.True( target.Error.Fields.ContainsKey( "url" ) );
        }

        [Fact]
        public void TitleIsOptionalAndTrimmed()
        {
            Link withoutTitle = Link.Create( Guid.NewGuid(), "https://github.com/skb/hackathon", null, 0, Now ).Value;
            Link withTitle = Link.Create( Guid.NewGuid(), "https://github.com/skb/hackathon", "  Репозиторий команды ", 0, Now ).Value;

            Assert.Equal( string.Empty, withoutTitle.Title );
            Assert.Equal( "Репозиторий команды", withTitle.Title );
        }

        [Fact]
        public void BadUpdateLeavesTheLinkAsItWas()
        {
            Link link = Link.Create( Guid.NewGuid(), "https://github.com/skb/hackathon", "Репозиторий", 0, Now ).Value;

            Result updated = link.Update( "javascript:alert(1)", new string( 'a', Link.TitleMaxLength + 1 ) );

            Assert.False( updated.IsSuccess );
            Assert.Equal( [ "title", "url" ], updated.Error.Fields.Keys.Order() );
            Assert.Equal( "https://github.com/skb/hackathon", link.Url );
            Assert.Equal( "Репозиторий", link.Title );
        }

        [Fact]
        public void NewAddressChangesTheKind()
        {
            Link link = Link.Create( Guid.NewGuid(), "https://github.com/skb/hackathon", null, 0, Now ).Value;

            link.Update( "https://docs.google.com/spreadsheets/d/1AbC", null );

            Assert.Equal( LinkKind.GoogleSheet, link.Kind );
        }
    }
}
