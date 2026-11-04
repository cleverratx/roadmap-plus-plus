using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadmapPlusPlus.Modules.Planning.Domain.Items;

namespace RoadmapPlusPlus.Modules.Planning.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Дерево хранится списком смежности: у узла есть <c>parent_id</c>. Удаление узла каскадом удаляет всё поддерево,
    /// а правила дат продублированы ограничениями в самой базе, чтобы их не обошёл никакой код.
    /// </summary>
    internal sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
    {
        public void Configure( EntityTypeBuilder<Item> builder )
        {
            builder.ToTable( "items", table =>
            {
                table.HasCheckConstraint( "ck_items_dates_together", "(start_date IS NULL) = (end_date IS NULL)" );
                table.HasCheckConstraint( "ck_items_dates_order", "end_date >= start_date" );
            } );
            builder.HasKey( i => i.Id );

            builder.Property( i => i.Kind )
                .HasConversion( kind => kind.ToString().ToLowerInvariant(), value => Enum.Parse<ItemKind>( value, true ) )
                .HasMaxLength( 16 );
            builder.Property( i => i.Title ).HasMaxLength( ItemContent.TitleMaxLength );
            builder.Property( i => i.DescriptionMd ).HasMaxLength( ItemContent.DescriptionMaxLength );

            builder.HasOne<Item>().WithMany().HasForeignKey( i => i.ParentId ).OnDelete( DeleteBehavior.Cascade );

            builder.HasIndex( i => i.ParentId );
            builder.HasIndex( i => new { i.WorkspaceId, i.StartDate, i.EndDate } );
        }
    }
}
