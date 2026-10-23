using RoadmapPlusPlus.BuildingBlocks;

namespace RoadmapPlusPlus.Modules.Identity.Domain.Workspaces
{
    internal static class WorkspaceErrors
    {
        /// <summary>Команды нет или пользователь в ней не состоит. Один ответ на оба случая, чтобы не выдавать, что команда существует.</summary>
        public static readonly Error NotFound = Error.NotFound(
            "identity.workspace_not_found",
            "Команда не найдена." );

        public static readonly Error NotHead = Error.Forbidden(
            "identity.not_head",
            "Это может сделать только глава команды." );

        public static readonly Error MemberNotFound = Error.NotFound(
            "identity.member_not_found",
            "Такого участника в команде нет." );

        public static readonly Error LastHead = Error.Conflict(
            "identity.last_head",
            "В команде должен остаться хотя бы один глава. Сначала назначьте главой кого-то ещё." );

        public static readonly Error InvitationNotFound = Error.NotFound(
            "identity.invitation_not_found",
            "Приглашение не найдено или больше не действует." );

        public static readonly Error ConcurrentChange = Error.Conflict(
            "identity.concurrent_change",
            "Команду только что изменил кто-то ещё. Обновите страницу и попробуйте снова." );

        public static Error InvalidRequest( IReadOnlyDictionary<string, string[]> fields )
        {
            return Error.Validation( "identity.invalid_request", "Проверьте поля формы.", fields );
        }
    }
}
