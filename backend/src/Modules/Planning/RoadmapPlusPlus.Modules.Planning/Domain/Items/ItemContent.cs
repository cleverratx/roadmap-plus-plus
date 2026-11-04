using RoadmapPlusPlus.BuildingBlocks;

namespace RoadmapPlusPlus.Modules.Planning.Domain.Items
{
    /// <summary>
    /// То, что человек вводит в узел. Даты календарные и включительные: событие с 28 по 29 сентября идёт два дня.
    /// Времени суток пока нет, оно появится вместе с расписанием по часам на этапе 3.
    /// </summary>
    internal sealed record ItemContent( string Title, DateOnly? StartDate, DateOnly? EndDate, string DescriptionMd )
    {
        public const int TitleMaxLength = 200;

        public const int DescriptionMaxLength = 20_000;

        public static Result<ItemContent> Create( string? title, DateOnly? startDate, DateOnly? endDate, string? descriptionMd )
        {
            Dictionary<string, string[]> fields = [];
            string trimmedTitle = title?.Trim() ?? string.Empty;
            string description = descriptionMd ?? string.Empty;

            if ( trimmedTitle.Length == 0 )
            {
                fields[ "title" ] = [ "Назовите узел." ];
            }
            else if ( trimmedTitle.Length > TitleMaxLength )
            {
                fields[ "title" ] = [ $"Название должно быть не длиннее {TitleMaxLength} символов." ];
            }

            if ( startDate.HasValue != endDate.HasValue )
            {
                fields[ startDate.HasValue ? "endDate" : "startDate" ] = [ "Укажите обе даты или ни одной." ];
            }
            else if ( startDate > endDate )
            {
                fields[ "endDate" ] = [ "Конец не может быть раньше начала." ];
            }

            if ( description.Length > DescriptionMaxLength )
            {
                fields[ "descriptionMd" ] = [ $"Описание должно быть не длиннее {DescriptionMaxLength} символов." ];
            }

            return fields.Count > 0
                ? PlanningErrors.InvalidRequest( fields )
                : new ItemContent( trimmedTitle, startDate, endDate, description );
        }
    }
}
