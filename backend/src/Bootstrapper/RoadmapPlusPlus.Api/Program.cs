using RoadmapPlusPlus.Api.Middleware;
using RoadmapPlusPlus.Api.OpenApi;
using RoadmapPlusPlus.BuildingBlocks;
using RoadmapPlusPlus.BuildingBlocks.Persistence;
using RoadmapPlusPlus.Modules.Assistant;
using RoadmapPlusPlus.Modules.Identity;
using RoadmapPlusPlus.Modules.Interchange;
using RoadmapPlusPlus.Modules.Knowledge;
using RoadmapPlusPlus.Modules.Planning;

IModule[] modules =
[
    new IdentityModule(),
    new PlanningModule(),
    new KnowledgeModule(),
    new InterchangeModule(),
    new AssistantModule(),
];

WebApplicationBuilder builder = WebApplication.CreateBuilder( args );

if ( BuildTimeOpenApi.IsGeneratingDocument )
{
    builder.Logging.ClearProviders();
}

builder.Services.AddErrorHandling();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddPostgres( builder.Configuration );

foreach ( IModule module in modules )
{
    module.AddServices( builder.Services, builder.Configuration );
}

WebApplication app = builder.Build();

if ( !BuildTimeOpenApi.IsGeneratingDocument )
{
    await app.Services.MigrateModuleDatabasesAsync();
}

app.UseErrorHandling();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

RouteGroupBuilder api = app.MapGroup( "/api" ).RequireAuthorization();
api.MapOpenApi( "/openapi/{documentName}.json" ).AllowAnonymous();
api.MapHealthChecks( "/health" ).AllowAnonymous();

foreach ( IModule module in modules )
{
    module.MapEndpoints( api );
}

await app.RunAsync();
