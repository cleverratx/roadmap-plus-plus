using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadmapPlusPlus.Modules.Identity.Domain.Workspaces;

namespace RoadmapPlusPlus.Modules.Identity.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Выданная роль ссылается на роль по паре (команда, роль), а не только по id роли. Так база сама не даст выдать
    /// участнику роль чужой команды, даже если код где-то ошибётся. Удаление роли или участника снимает выдачу каскадом.
    /// </summary>
    internal sealed class MemberRoleConfiguration : IEntityTypeConfiguration<MemberRole>
    {
        public void Configure( EntityTypeBuilder<MemberRole> builder )
        {
            builder.ToTable( "member_roles" );
            builder.HasKey( r => new { r.WorkspaceId, r.UserId, r.RoleId } );

            builder.HasOne<Role>()
                .WithMany()
                .HasForeignKey( r => new { r.WorkspaceId, r.RoleId } )
                .HasPrincipalKey( r => new { r.WorkspaceId, r.Id } )
                .OnDelete( DeleteBehavior.Cascade );
        }
    }
}
