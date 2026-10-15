using System.ComponentModel.DataAnnotations;
using RoadmapPlusPlus.Modules.Identity.Domain;

namespace RoadmapPlusPlus.Modules.Identity.Application.Auth
{
    /// <summary>
    /// Правила из ADR-0004: пароль от 8 символов без требований к цифрам и спецсимволам, 5 неудачных попыток входа
    /// дают блокировку на 5 минут. Поля проверяем сами, чтобы ошибки были по-русски и с именами полей формы.
    /// </summary>
    internal static class AuthRules
    {
        public const int MinPasswordLength = 8;

        public const int MaxFailedAccessAttempts = 5;

        public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes( 5 );

        private static readonly EmailAddressAttribute EmailFormat = new();

        public static Dictionary<string, string[]> Validate( RegisterRequest request )
        {
            Dictionary<string, string[]> fields = [];

            string email = request.Email?.Trim() ?? string.Empty;
            if ( email.Length == 0 )
            {
                fields[ "email" ] = [ "Укажите email." ];
            }
            else if ( email.Length > User.EmailMaxLength || !EmailFormat.IsValid( email ) )
            {
                fields[ "email" ] = [ "Это не похоже на email." ];
            }

            if ( string.IsNullOrEmpty( request.Password ) || request.Password.Length < MinPasswordLength )
            {
                fields[ "password" ] = [ $"Пароль должен быть не короче {MinPasswordLength} символов." ];
            }

            string displayName = request.DisplayName?.Trim() ?? string.Empty;
            if ( displayName.Length == 0 )
            {
                fields[ "displayName" ] = [ "Укажите, как вас называть." ];
            }
            else if ( displayName.Length > User.DisplayNameMaxLength )
            {
                fields[ "displayName" ] = [ $"Имя должно быть не длиннее {User.DisplayNameMaxLength} символов." ];
            }

            return fields;
        }
    }
}
