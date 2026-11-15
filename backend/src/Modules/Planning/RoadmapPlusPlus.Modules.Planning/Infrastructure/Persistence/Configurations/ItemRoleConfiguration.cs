using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadmapPlusPlus.Modules.Planning.Domain.Access;
using RoadmapPlusPlus.Modules.Planning.Domain.Items;

namespace RoadmapPlusPlus.Modules.Planning.Infrastructure.Persistence.Configurations
{
    internal sealed class ItemRoleConfiguration : IEntityTypeConfiguration<ItemRole>
    {
        public void Configure( EntityTypeBuilder<ItemRole> builder )
        {
            builder.ToTable( "item_roles" );
            builder.HasKey( r => new { r.ItemId, r.RoleId } );
            builder.HasIndex( r => r.RoleId );

            builder.HasOne<Item>().WithMany().HasForeignKey( r => r.ItemId ).OnDelete( DeleteBehavior.Cascade );
        }
    }
}
