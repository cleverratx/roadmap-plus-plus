using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadmapPlusPlus.Modules.Identity.Domain.Workspaces;

namespace RoadmapPlusPlus.Modules.Identity.Infrastructure.Persistence.Configurations
{
    internal sealed class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
    {
        public void Configure( EntityTypeBuilder<Workspace> builder )
        {
            builder.ToTable( "workspaces" );
            builder.HasKey( w => w.Id );

            builder.Property( w => w.Name ).HasMaxLength( Workspace.NameMaxLength );
            builder.Property( w => w.Color ).HasMaxLength( 7 );
            builder.Property( w => w.Version ).IsConcurrencyToken();

            builder.HasMany( w => w.Members ).WithOne().HasForeignKey( m => m.WorkspaceId ).OnDelete( DeleteBehavior.Cascade );
            builder.Navigation( w => w.Members ).UsePropertyAccessMode( PropertyAccessMode.Field );
        }
    }
}
