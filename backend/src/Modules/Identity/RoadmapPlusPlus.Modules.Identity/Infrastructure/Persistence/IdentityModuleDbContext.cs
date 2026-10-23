using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RoadmapPlusPlus.BuildingBlocks.Persistence;
using RoadmapPlusPlus.Modules.Identity.Domain;
using RoadmapPlusPlus.Modules.Identity.Domain.Workspaces;

namespace RoadmapPlusPlus.Modules.Identity.Infrastructure.Persistence
{
    /// <summary>
    /// Таблицы Identity, команды и ключи Data Protection в схеме <c>identity</c>. Не наследуется от <c>IdentityDbContext</c>,
    /// потому что контекст модуля обязан наследовать <see cref="ModuleDbContext"/>; модель Identity настроена в <c>Configurations/</c>,
    /// а <c>UserOnlyStore</c> работает с любым контекстом, где есть эти таблицы.
    /// </summary>
    internal sealed class IdentityModuleDbContext( DbContextOptions<IdentityModuleDbContext> options )
        : ModuleDbContext( options ), IDataProtectionKeyContext
    {
        public DbSet<User> Users => Set<User>();

        public DbSet<Workspace> Workspaces => Set<Workspace>();

        public DbSet<Member> Members => Set<Member>();

        public DbSet<Invitation> Invitations => Set<Invitation>();

        public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    }
}
