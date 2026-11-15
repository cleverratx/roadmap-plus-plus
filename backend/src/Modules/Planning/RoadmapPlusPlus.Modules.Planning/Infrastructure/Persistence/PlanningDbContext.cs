using Microsoft.EntityFrameworkCore;
using RoadmapPlusPlus.BuildingBlocks.Persistence;
using RoadmapPlusPlus.Modules.Planning.Domain.Access;
using RoadmapPlusPlus.Modules.Planning.Domain.Items;
using RoadmapPlusPlus.Modules.Planning.Domain.Links;

namespace RoadmapPlusPlus.Modules.Planning.Infrastructure.Persistence
{
    internal sealed class PlanningDbContext( DbContextOptions<PlanningDbContext> options ) : ModuleDbContext( options )
    {
        public DbSet<Item> Items => Set<Item>();

        public DbSet<Link> Links => Set<Link>();

        public DbSet<ItemRole> ItemRoles => Set<ItemRole>();
    }
}
