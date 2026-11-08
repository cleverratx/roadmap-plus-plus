using RoadmapPlusPlus.BuildingBlocks;
using RoadmapPlusPlus.Modules.Planning.Domain.Items;

namespace RoadmapPlusPlus.Modules.Planning.Domain.Links
{
    /// <summary>
    /// Внешняя ссылка у узла: Google Таблица с задачами, пост организаторов в Telegram, репозиторий, макет в Figma.
    /// Принадлежит узлу и удаляется вместе с ним. Название необязательное: без него панель подпишет ссылку по виду и адресу.
    /// </summary>
    internal sealed class Link
    {
        public const int TitleMaxLength = 200;

        /// <summary>Сколько ссылок можно повесить на один узел. С запасом для реального события и защита от мусора.</summary>
        public const int MaxPerItem = 50;

        private Link()
        {
            Url = string.Empty;
            Title = string.Empty;
        }

        public Guid Id { get; private set; }

        public Guid ItemId { get; private set; }

        public string Url { get; private set; }

        public LinkKind Kind { get; private set; }

        public string Title { get; private set; }

        public int Position { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public static Result<Link> Create( Guid itemId, string? url, string? title, int position, DateTimeOffset now )
        {
            Link link = new()
            {
                Id = Guid.CreateVersion7(),
                ItemId = itemId,
                Position = position,
                CreatedAt = now,
            };

            Result applied = link.Apply( url, title );
            return applied.IsSuccess ? link : applied.Error;
        }

        public Result Update( string? url, string? title )
        {
            return Apply( url, title );
        }

        private Result Apply( string? url, string? title )
        {
            Result<LinkTarget> target = LinkTarget.Parse( url );
            string trimmedTitle = title?.Trim() ?? string.Empty;
            Dictionary<string, string[]> fields = target.IsSuccess ? [] : new( target.Error.Fields );

            if ( trimmedTitle.Length > TitleMaxLength )
            {
                fields[ "title" ] = [ $"Название должно быть не длиннее {TitleMaxLength} символов." ];
            }

            if ( fields.Count > 0 )
            {
                return PlanningErrors.InvalidRequest( fields );
            }

            Url = target.Value.Url;
            Kind = target.Value.Kind;
            Title = trimmedTitle;
            return Result.Success();
        }
    }
}
