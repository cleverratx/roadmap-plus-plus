using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RoadmapPlusPlus.Modules.Identity.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Внешние способы входа, привязанные к пользователю. Понадобятся для Telegram и других провайдеров из ADR-0004.
    /// </summary>
    internal sealed class UserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
    {
        public void Configure( EntityTypeBuilder<IdentityUserLogin<Guid>> builder )
        {
            builder.ToTable( "user_logins" );
            builder.HasKey( l => new { l.LoginProvider, l.ProviderKey } );
            builder.Property( l => l.LoginProvider ).HasMaxLength( 128 );
            builder.Property( l => l.ProviderKey ).HasMaxLength( 128 );
        }
    }
}
