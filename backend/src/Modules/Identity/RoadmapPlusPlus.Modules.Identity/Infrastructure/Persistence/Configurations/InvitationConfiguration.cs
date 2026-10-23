using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadmapPlusPlus.Modules.Identity.Domain.Workspaces;

namespace RoadmapPlusPlus.Modules.Identity.Infrastructure.Persistence.Configurations
{
    internal sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
    {
        public void Configure( EntityTypeBuilder<Invitation> builder )
        {
            builder.ToTable( "invitations" );
            builder.HasKey( i => i.Id );
            builder.HasIndex( i => i.Token ).IsUnique();
            builder.HasIndex( i => i.WorkspaceId );

            builder.Property( i => i.Token ).HasMaxLength( 64 );

            builder.HasOne<Workspace>().WithMany().HasForeignKey( i => i.WorkspaceId ).OnDelete( DeleteBehavior.Cascade );
        }
    }
}
