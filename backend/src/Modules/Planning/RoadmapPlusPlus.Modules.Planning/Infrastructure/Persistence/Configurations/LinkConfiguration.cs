using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadmapPlusPlus.Modules.Planning.Domain.Items;
using RoadmapPlusPlus.Modules.Planning.Domain.Links;

namespace RoadmapPlusPlus.Modules.Planning.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Ссылки лежат отдельной таблицей с <c>item_id</c> и уходят каскадом вместе с узлом. Вид хранится строкой
    /// в snake_case (<c>google_sheet</c>), чтобы таблицу было удобно читать глазами.
    /// </summary>
    internal sealed class LinkConfiguration : IEntityTypeConfiguration<Link>
    {
        public void Configure( EntityTypeBuilder<Link> builder )
        {
            builder.ToTable( "links" );
            builder.HasKey( l => l.Id );

            builder.Property( l => l.Url ).HasMaxLength( LinkTarget.UrlMaxLength );
            builder.Property( l => l.Title ).HasMaxLength( Link.TitleMaxLength );
            builder.Property( l => l.Kind )
                .HasConversion(
                    kind => JsonNamingPolicy.SnakeCaseLower.ConvertName( kind.ToString() ),
                    value => Enum.Parse<LinkKind>( value.Replace( "_", string.Empty, StringComparison.Ordinal ), true ) )
                .HasMaxLength( 32 );

            builder.HasOne<Item>().WithMany().HasForeignKey( l => l.ItemId ).OnDelete( DeleteBehavior.Cascade );
            builder.HasIndex( l => new { l.ItemId, l.Position } );
        }
    }
}
