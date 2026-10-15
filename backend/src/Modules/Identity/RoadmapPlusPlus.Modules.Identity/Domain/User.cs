using Microsoft.AspNetCore.Identity;

namespace RoadmapPlusPlus.Modules.Identity.Domain
{
    /// <summary>
    /// Пользователь сервиса. Логин это email: он же лежит в <see cref="IdentityUser{TKey}.UserName"/>.
    /// </summary>
    internal sealed class User : IdentityUser<Guid>
    {
        public const int EmailMaxLength = 256;

        public const int DisplayNameMaxLength = 100;

        public User( string email, string displayName )
        {
            Id = Guid.CreateVersion7();
            UserName = email;
            Email = email;
            DisplayName = displayName;
            CreatedAt = DateTimeOffset.UtcNow;
        }

        private User()
        {
            DisplayName = string.Empty;
        }

        public string DisplayName { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }
    }
}
