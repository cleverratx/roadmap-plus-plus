using RoadmapPlusPlus.Modules.Planning.Application.Links;
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
        string? DescriptionMd,
        IReadOnlyList<Guid>? RoleIds = null );

    /// <summary>Каким ролям открыт узел. Набор заменяется целиком, менять его может только глава команды.</summary>
    internal sealed record SetItemRolesRequest( IReadOnlyList<Guid> RoleIds );

    /// <summary>Сколько событий команды не открыто ни одной роли: при включённых ролях их увидят только главы.</summary>
    internal sealed record AccessSummary( int UnsharedEvents, IReadOnlyList<ItemResponse> Examples );

    internal sealed record UpdateItemRequest( string Title, DateOnly? StartDate, DateOnly? EndDate, string? DescriptionMd );

    /// <summary>
    /// Куда перенести узел. <c>ParentId = null</c> значит верхний уровень его команды. <c>Position</c> это место среди
    /// новых соседей без самого узла, с нуля; без него или за пределами списка узел встаёт в конец или в начало.
    /// Чтобы просто поменять порядок, передают того же родителя и новое место.
    /// </summary>
    internal sealed record MoveItemRequest( Guid? ParentId, int? Position );

    /// <summary>Узел, в который можно перенести, и названия его предков от корня, чтобы показать, где он лежит.</summary>
    internal sealed record MoveTarget( ItemResponse Item, IReadOnlyList<string> Path );

    internal sealed record MoveTargets( bool CanBeRoot, Guid? CurrentParentId, IReadOnlyList<MoveTarget> Parents );

    internal sealed record ItemResponse(
        Guid Id,
        Guid WorkspaceId,
        Guid? ParentId,
        ItemKind Kind,
        string Title,
        DateOnly? StartDate,
        DateOnly? EndDate,
        int Position,
        bool IsFrame )
    {
        public static ItemResponse From( Item item, bool isFrame = false )
        {
            return new ItemResponse( item.Id, item.WorkspaceId, item.ParentId, item.Kind, item.Title, item.StartDate, item.EndDate, item.Position, isFrame );
        }
    }

    /// <summary>
    /// Узел для панели: сам узел с описанием, путь от корня до родителя, прямые дети и ссылки. <c>RoleIds</c> это роли,
    /// которым открыт сам узел, а <c>InheritedRoleIds</c> роли, которым открыт кто-то из предков, и доступ от них тоже действует.
    /// У рамки (<c>Item.IsFrame</c>) нет ни описания, ни ссылок, ни ролей, а среди детей только то, что пользователю видно.
    /// </summary>
    internal sealed record ItemDetails(
        ItemResponse Item,
        string DescriptionMd,
        IReadOnlyList<ItemResponse> Path,
        IReadOnlyList<ItemResponse> Children,
        IReadOnlyList<LinkResponse> Links,
        IReadOnlyList<Guid> RoleIds,
        IReadOnlyList<Guid> InheritedRoleIds );
}
