using RoadmapPlusPlus.BuildingBlocks;
using RoadmapPlusPlus.Modules.Planning.Domain.Items;

namespace RoadmapPlusPlus.Modules.Planning.Domain.Links
{
    /// <summary>
    /// Проверенный адрес ссылки и вид, определённый по нему. Принимаются только http и https: адрес попадает в <c>href</c>,
    /// и <c>javascript:</c> или <c>data:</c> там недопустимы. Адрес без схемы, вроде <c>t.me/skb/42</c>, дополняется до https.
    /// </summary>
    internal sealed record LinkTarget( string Url, LinkKind Kind )
    {
        public const int UrlMaxLength = 2048;

        private static readonly string[] RepoHosts = [ "github.com", "gitlab.com", "bitbucket.org" ];

        public static Result<LinkTarget> Parse( string? url )
        {
            string trimmed = url?.Trim() ?? string.Empty;
            if ( trimmed.Length == 0 )
            {
                return Invalid( "Вставьте адрес ссылки." );
            }

            if ( trimmed.Length > UrlMaxLength )
            {
                return Invalid( $"Адрес должен быть не длиннее {UrlMaxLength} символов." );
            }

            Uri? uri = ToHttpUri( trimmed ) ?? ( trimmed.Contains( "://", StringComparison.Ordinal ) ? null : ToHttpUri( $"https://{trimmed}" ) );
            if ( uri is null || !uri.Host.Contains( '.', StringComparison.Ordinal ) )
            {
                return Invalid( "Это не похоже на адрес сайта. Нужна ссылка, которая начинается с http:// или https://." );
            }

            return new LinkTarget( uri.AbsoluteUri, Detect( uri ) );
        }

        public static LinkKind Detect( Uri uri )
        {
            string host = uri.Host.ToLowerInvariant();
            string[] segments = uri.AbsolutePath.Split( '/', StringSplitOptions.RemoveEmptyEntries );
            string first = segments.Length > 0 ? segments[ 0 ] : string.Empty;

            if ( IsHost( host, "docs.google.com" ) )
            {
                return first switch
                {
                    "spreadsheets" => LinkKind.GoogleSheet,
                    "document" => LinkKind.GoogleDoc,
                    "presentation" => LinkKind.GoogleSlides,
                    _ => LinkKind.GoogleDrive,
                };
            }

            if ( IsHost( host, "drive.google.com" ) )
            {
                return LinkKind.GoogleDrive;
            }

            if ( IsHost( host, "t.me" ) || IsHost( host, "telegram.me" ) )
            {
                return IsTelegramPost( segments ) ? LinkKind.TgPost : LinkKind.Telegram;
            }

            if ( RepoHosts.Any( repoHost => IsHost( host, repoHost ) ) )
            {
                return LinkKind.Repo;
            }

            if ( IsHost( host, "figma.com" ) )
            {
                return LinkKind.Figma;
            }

            if ( IsHost( host, "notion.so" ) || IsHost( host, "notion.site" ) || IsHost( host, "notion.com" ) )
            {
                return LinkKind.Notion;
            }

            return IsHost( host, "miro.com" ) ? LinkKind.Miro : LinkKind.Other;
        }

        /// <summary>Пост это <c>t.me/канал/42</c>, <c>t.me/s/канал/42</c> или <c>t.me/c/1234567/42</c> для закрытых каналов.</summary>
        private static bool IsTelegramPost( string[] segments )
        {
            int skip = segments.Length > 0 && segments[ 0 ] is "s" or "c" ? 1 : 0;
            return segments.Length == skip + 2 && segments[ ^1 ].All( char.IsAsciiDigit );
        }

        private static bool IsHost( string host, string domain )
        {
            return host == domain || host.EndsWith( $".{domain}", StringComparison.Ordinal );
        }

        private static Uri? ToHttpUri( string value )
        {
            return Uri.TryCreate( value, UriKind.Absolute, out Uri? uri ) && ( uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp )
                ? uri
                : null;
        }

        private static Error Invalid( string message )
        {
            return PlanningErrors.InvalidRequest( new Dictionary<string, string[]> { [ "url" ] = [ message ] } );
        }
    }
}
