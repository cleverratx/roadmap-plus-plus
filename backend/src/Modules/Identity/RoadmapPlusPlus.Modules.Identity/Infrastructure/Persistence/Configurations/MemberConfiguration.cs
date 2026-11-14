using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadmapPlusPlus.Modules.Identity.Domain;
using RoadmapPlusPlus.Modules.Identity.Domain.Workspaces;

namespace RoadmapPlusPlus.Modules.Identity.Infrastructure.Persistence.Configurations
{
    internal sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
    {
        public void Configure( EntityTypeBuilder<Member> builder )
        {
            builder.ToTable( "members" );
            builder.HasKey( m => new { m.WorkspaceId, m.UserId } );
            builder.HasIndex( m => m.UserId );

            builder.Property( m => m.Color ).HasMaxLength( 7 );

            builder.HasOne<User>().WithMany().HasForeignKey( m => m.UserId ).OnDelete( DeleteBehavior.Cascade );

            builder.HasMany( m => m.Roles ).WithOne().HasForeignKey( r => new { r.WorkspaceId, r.UserId } ).OnDelete( DeleteBehavior.Cascade );
            builder.Navigation( m => m.Roles ).UsePropertyAccessMode( PropertyAccessMode.Field );
        }
    }
}
