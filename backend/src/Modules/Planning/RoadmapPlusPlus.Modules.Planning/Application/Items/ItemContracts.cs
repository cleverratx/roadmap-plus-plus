using RoadmapPlusPlus.Modules.Planning.Domain.Items;

namespace RoadmapPlusPlus.Modules.Planning.Application.Items
{
    internal sealed record CreateItemRequest(
        Guid? WorkspaceId,
        Guid? ParentId,
        ItemKind Kind,
        string Title,
        DateOnly? StartDate,
        DateOnly? EndDate,
        string? DescriptionMd );

    internal sealed record UpdateItemRequest( string Title, DateOnly? StartDate, DateOnly? EndDate, string? DescriptionMd );

    internal sealed record ItemResponse(
        Guid Id,
        Guid WorkspaceId,
        Guid? ParentId,
        ItemKind Kind,
        string Title,
        DateOnly? StartDate,
        DateOnly? EndDate,
        int Position )
    {
        public static ItemResponse From( Item item )
        {
            return new ItemResponse( item.Id, item.WorkspaceId, item.ParentId, item.Kind, item.Title, item.StartDate, item.EndDate, item.Position );
        }
    }

    /// <summary>Узел для панели: сам узел с описанием, путь от корня до родителя и прямые дети.</summary>
    internal sealed record ItemDetails( ItemResponse Item, string DescriptionMd, IReadOnlyList<ItemResponse> Path, IReadOnlyList<ItemResponse> Children );
}
