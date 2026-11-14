using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadmapPlusPlus.Modules.Identity.Domain.Workspaces;

namespace RoadmapPlusPlus.Modules.Identity.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Роль добавляется через агрегат команды, а не через <c>db.Roles.Add</c>. Id ей выдаёт код, поэтому EF надо сказать,
    /// что база ключ не генерирует: иначе новая роль с заполненным Id выглядела бы для EF уже существующей строкой.
    /// </summary>
    internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure( EntityTypeBuilder<Role> builder )
        {
            builder.ToTable( "roles" );
            builder.HasKey( r => r.Id );
            builder.Property( r => r.Id ).ValueGeneratedNever();

            builder.HasAlternateKey( r => new { r.WorkspaceId, r.Id } );

            builder.Property( r => r.Name ).HasMaxLength( Role.NameMaxLength );
            builder.Property( r => r.Color ).HasMaxLength( 7 );
        }
    }
}
