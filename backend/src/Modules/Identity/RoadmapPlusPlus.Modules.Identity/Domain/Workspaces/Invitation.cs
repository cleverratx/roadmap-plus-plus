using System.Buffers.Text;
using System.Security.Cryptography;

namespace RoadmapPlusPlus.Modules.Identity.Domain.Workspaces
{
    /// <summary>
    /// Ссылка-приглашение в команду. Многоразовая: по ней может вступить сколько угодно людей, пока она не истекла
    /// и её не отозвали. У команды одна активная ссылка, новая отзывает старую.
    /// </summary>
    internal sealed class Invitation
    {
        public static readonly TimeSpan Lifetime = TimeSpan.FromDays( 7 );

        private Invitation()
        {
            Token = string.Empty;
        }

        public Guid Id { get; private set; }

        public Guid WorkspaceId { get; private set; }

        /// <summary>Случайные 192 бита в base64url: угадать перебором невозможно.</summary>
        public string Token { get; private set; }

        public Guid CreatedByUserId { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public DateTimeOffset ExpiresAt { get; private set; }

        public DateTimeOffset? RevokedAt { get; private set; }

        public static Invitation Create( Guid workspaceId, Guid createdByUserId, DateTimeOffset now )
        {
            return new Invitation
            {
                Id = Guid.CreateVersion7(),
                WorkspaceId = workspaceId,
                Token = Base64Url.EncodeToString( RandomNumberGenerator.GetBytes( 24 ) ),
                CreatedByUserId = createdByUserId,
                CreatedAt = now,
                ExpiresAt = now + Lifetime,
            };
        }

        public bool IsActive( DateTimeOffset now )
        {
            return RevokedAt is null && now < ExpiresAt;
        }

        public void Revoke( DateTimeOffset now )
        {
            RevokedAt ??= now;
        }
    }
}
