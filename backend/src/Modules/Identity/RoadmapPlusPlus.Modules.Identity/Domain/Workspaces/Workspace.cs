using RoadmapPlusPlus.BuildingBlocks;

namespace RoadmapPlusPlus.Modules.Identity.Domain.Workspaces
{
    /// <summary>
    /// Команда и её участники. Это агрегат: состав меняется только через его методы, и все правила из ADR-0003
    /// проверяются здесь, в одном месте. Любое изменение увеличивает <see cref="Version"/>, а база сохраняет команду,
    /// только если версия не поменялась с момента чтения: так два одновременных изменения состава не затрут друг друга.
    /// </summary>
    internal sealed class Workspace
    {
        public const int NameMaxLength = 100;

        private readonly List<Member> _members = [];

        private Workspace()
        {
            Name = string.Empty;
            Color = string.Empty;
        }

        public Guid Id { get; private set; }

        public string Name { get; private set; }

        public string Color { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public int Version { get; private set; }

        public IReadOnlyList<Member> Members => _members;

        public static Workspace Create( string name, string color, Guid creatorId, DateTimeOffset now )
        {
            Workspace workspace = new()
            {
                Id = Guid.CreateVersion7(),
                Name = name,
                Color = color,
                CreatedAt = now,
            };

            workspace._members.Add( new Member( workspace.Id, creatorId, isHead: true, WorkspacePalette.ForMember( 0 ), now ) );
            return workspace;
        }

        public Member? FindMember( Guid userId )
        {
            return _members.FirstOrDefault( m => m.UserId == userId );
        }

        public bool IsHead( Guid userId )
        {
            return FindMember( userId )?.IsHead == true;
        }

        public Result Update( Guid actorId, string name, string color )
        {
            if ( !IsHead( actorId ) )
            {
                return WorkspaceErrors.NotHead;
            }

            Name = name;
            Color = color;
            Version++;
            return Result.Success();
        }

        /// <summary>Добавляет участника без прав главы. Возвращает <see langword="false"/>, если он уже в команде.</summary>
        public bool AddMember( Guid userId, DateTimeOffset now )
        {
            if ( FindMember( userId ) is not null )
            {
                return false;
            }

            _members.Add( new Member( Id, userId, isHead: false, WorkspacePalette.ForMember( _members.Count ), now ) );
            Version++;
            return true;
        }

        /// <summary>
        /// Глава может исключить любого, а любой участник может выйти сам. Последнего главу убрать нельзя, даже если он выходит сам.
        /// </summary>
        public Result RemoveMember( Guid actorId, Guid userId )
        {
            Member? target = FindMember( userId );
            if ( target is null )
            {
                return WorkspaceErrors.MemberNotFound;
            }

            if ( actorId != userId && !IsHead( actorId ) )
            {
                return WorkspaceErrors.NotHead;
            }

            if ( target.IsHead && HeadCount == 1 )
            {
                return WorkspaceErrors.LastHead;
            }

            _members.Remove( target );
            Version++;
            return Result.Success();
        }

        public Result SetHead( Guid actorId, Guid userId, bool isHead )
        {
            if ( !IsHead( actorId ) )
            {
                return WorkspaceErrors.NotHead;
            }

            Member? target = FindMember( userId );
            if ( target is null )
            {
                return WorkspaceErrors.MemberNotFound;
            }

            if ( !isHead && target.IsHead && HeadCount == 1 )
            {
                return WorkspaceErrors.LastHead;
            }

            target.SetHead( isHead );
            Version++;
            return Result.Success();
        }

        private int HeadCount => _members.Count( m => m.IsHead );
    }
}
