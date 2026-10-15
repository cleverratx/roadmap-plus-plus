using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RoadmapPlusPlus.Modules.Identity.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Ключи, которыми шифруется cookie сессии. Лежат в базе, чтобы перезапуск контейнера не разлогинивал всех (ADR-0004).
    /// </summary>
    internal sealed class DataProtectionKeyConfiguration : IEntityTypeConfiguration<DataProtectionKey>
    {
        public void Configure( EntityTypeBuilder<DataProtectionKey> builder )
        {
            builder.ToTable( "data_protection_keys" );
        }
    }
}
