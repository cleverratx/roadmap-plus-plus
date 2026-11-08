namespace RoadmapPlusPlus.Modules.Planning.Domain.Links
{
    /// <summary>
    /// Куда ведёт ссылка. Вид не выбирают руками, его определяет <see cref="LinkTarget"/> по адресу,
    /// чтобы панель могла подписать ссылку и показать нужную иконку.
    /// </summary>
    internal enum LinkKind
    {
        Other,
        GoogleSheet,
        GoogleDoc,
        GoogleSlides,
        GoogleDrive,
        TgPost,
        Telegram,
        Repo,
        Figma,
        Notion,
        Miro,
    }
}
