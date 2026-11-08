using RoadmapPlusPlus.Modules.Planning.Domain.Links;

namespace RoadmapPlusPlus.Modules.Planning.Application.Links
{
    /// <summary>Вид ссылки клиент не присылает: сервер определяет его по адресу сам.</summary>
    internal sealed record SaveLinkRequest( string Url, string? Title );

    internal sealed record LinkResponse( Guid Id, string Url, string Title, LinkKind Kind, int Position )
    {
        public static LinkResponse From( Link link )
        {
            return new LinkResponse( link.Id, link.Url, link.Title, link.Kind, link.Position );
        }
    }
}
