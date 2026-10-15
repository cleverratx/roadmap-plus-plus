using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadmapPlusPlus.Modules.Identity.Domain;

namespace RoadmapPlusPlus.Modules.Identity.Infrastructure.Persistence.Configurations
{
    internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure( EntityTypeBuilder<User> builder )
        {
            builder.ToTable( "users" );
            builder.HasKey( u => u.Id );

            builder.HasIndex( u => u.NormalizedUserName ).IsUnique();
            builder.HasIndex( u => u.NormalizedEmail ).IsUnique();

            builder.Property( u => u.ConcurrencyStamp ).IsConcurrencyToken();
            builder.Property( u => u.UserName ).HasMaxLength( User.EmailMaxLength );
            builder.Property( u => u.NormalizedUserName ).HasMaxLength( User.EmailMaxLength );
            builder.Property( u => u.Email ).HasMaxLength( User.EmailMaxLength );
            builder.Property( u => u.NormalizedEmail ).HasMaxLength( User.EmailMaxLength );
            builder.Property( u => u.DisplayName ).HasMaxLength( User.DisplayNameMaxLength );

            builder.HasMany<IdentityUserClaim<Guid>>().WithOne().HasForeignKey( c => c.UserId ).IsRequired();
            builder.HasMany<IdentityUserLogin<Guid>>().WithOne().HasForeignKey( l => l.UserId ).IsRequired();
            builder.HasMany<IdentityUserToken<Guid>>().WithOne().HasForeignKey( t => t.UserId ).IsRequired();
        }
    }
}
