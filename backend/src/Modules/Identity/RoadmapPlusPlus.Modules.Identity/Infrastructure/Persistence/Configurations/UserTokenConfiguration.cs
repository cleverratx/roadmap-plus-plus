using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RoadmapPlusPlus.Modules.Identity.Infrastructure.Persistence.Configurations
{
    internal sealed class UserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<Guid>>
    {
        public void Configure( EntityTypeBuilder<IdentityUserToken<Guid>> builder )
        {
            builder.ToTable( "user_tokens" );
            builder.HasKey( t => new { t.UserId, t.LoginProvider, t.Name } );
            builder.Property( t => t.LoginProvider ).HasMaxLength( 128 );
            builder.Property( t => t.Name ).HasMaxLength( 128 );
        }
    }
}
