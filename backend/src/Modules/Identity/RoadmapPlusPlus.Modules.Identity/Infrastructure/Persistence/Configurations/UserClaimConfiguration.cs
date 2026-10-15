using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RoadmapPlusPlus.Modules.Identity.Infrastructure.Persistence.Configurations
{
    internal sealed class UserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
    {
        public void Configure( EntityTypeBuilder<IdentityUserClaim<Guid>> builder )
        {
            builder.ToTable( "user_claims" );
            builder.HasKey( c => c.Id );
        }
    }
}
