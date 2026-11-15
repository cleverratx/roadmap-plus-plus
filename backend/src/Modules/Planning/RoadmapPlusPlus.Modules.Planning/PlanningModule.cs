using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadmapPlusPlus.BuildingBlocks;
using RoadmapPlusPlus.BuildingBlocks.Persistence;
using RoadmapPlusPlus.Modules.Planning.Application.Access;
using RoadmapPlusPlus.Modules.Planning.Application.Items;
using RoadmapPlusPlus.Modules.Planning.Application.Links;
using RoadmapPlusPlus.Modules.Planning.Endpoints;
using RoadmapPlusPlus.Modules.Planning.Infrastructure.Persistence;

namespace RoadmapPlusPlus.Modules.Planning
{
    public sealed class PlanningModule : IModule
    {
        public void AddServices( IServiceCollection services, IConfiguration configuration )
        {
            services.AddModuleDbContext<PlanningDbContext>();
            services.TryAddSingleton( TimeProvider.System );
            services.AddScoped<ItemVisibility>();
            services.AddScoped<ItemGrants>();
            services.AddScoped<ItemService>();
            services.AddScoped<ItemAccessService>();
            services.AddScoped<LinkService>();
        }

        public void MapEndpoints( IEndpointRouteBuilder endpoints )
        {
            endpoints.MapItemEndpoints();
            endpoints.MapLinkEndpoints();
        }
    }
}
