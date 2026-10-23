using System.Text.RegularExpressions;

namespace RoadmapPlusPlus.Modules.Identity.Domain.Workspaces
{
    /// <summary>
    /// Цвета команд и участников. Команде цвет выбирает человек (подойдёт любой <c>#RRGGBB</c>), а участнику он
    /// достаётся по очереди из палитры, чтобы соседи в списке различались.
    /// </summary>
    internal static partial class WorkspacePalette
    {
        public static readonly IReadOnlyList<string> Colors =
        [
            "#3b5bdb",
            "#2b8a3e",
            "#e8590c",
            "#c2255c",
            "#7048e8",
            "#1098ad",
            "#e67700",
            "#5c940d",
        ];

        public static string ForMember( int index )
        {
            return Colors[ index % Colors.Count ];
        }

        public static string ForWorkspace( int index )
        {
            return Colors[ index % Colors.Count ];
        }

        public static bool IsValid( string? color )
        {
            return color is not null && HexColor().IsMatch( color );
        }

        [GeneratedRegex( "^#[0-9a-fA-F]{6}$" )]
        private static partial Regex HexColor();
    }
}
