using RoadmapPlusPlus.BuildingBlocks;

namespace RoadmapPlusPlus.Modules.Identity.Application.Auth
{
    internal static class AuthErrors
    {
        public static readonly Error EmailTaken = Error.Conflict(
            "identity.email_taken",
            "Этот email уже зарегистрирован." );

        public static readonly Error InvalidCredentials = Error.Validation(
            "identity.invalid_credentials",
            "Неверный email или пароль." );

        public static readonly Error LockedOut = Error.Forbidden(
            "identity.locked_out",
            "Слишком много неудачных попыток. Попробуйте войти через 5 минут." );

        public static Error InvalidRequest( IReadOnlyDictionary<string, string[]> fields )
        {
            return Error.Validation( "identity.invalid_request", "Проверьте поля формы.", fields );
        }
    }
}
