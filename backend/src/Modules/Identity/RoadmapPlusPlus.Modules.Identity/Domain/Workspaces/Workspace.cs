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

        /// <summary>Сколько ролей может быть в одной команде. С запасом для любой реальной команды.</summary>
        public const int MaxRoles = 50;

        private readonly List<Member> _members = [];

        private readonly List<Role> _roles = [];

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

        /// <summary>
        /// Флажок "Включить роли" из ADR-0003. Пока он выключен, роли можно заводить и раздавать, но на видимость
        /// они не влияют. Выключение ничего не удаляет, при повторном включении всё возвращается как было.
        /// </summary>
        public bool RolesEnabled { get; private set; }

        public IReadOnlyList<Member> Members => _members;

        public IReadOnlyList<Role> Roles => _roles;

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

        public Result SetRolesEnabled( Guid actorId, bool enabled )
        {
            if ( !IsHead( actorId ) )
            {
                return WorkspaceErrors.NotHead;
            }

            if ( RolesEnabled != enabled )
            {
                RolesEnabled = enabled;
                Version++;
            }

            return Result.Success();
        }

        public Result<Role> AddRole( Guid actorId, string name, string color, DateTimeOffset now )
        {
            if ( !IsHead( actorId ) )
            {
                return WorkspaceErrors.NotHead;
            }

            if ( _roles.Count >= MaxRoles )
            {
                return WorkspaceErrors.TooManyRoles( MaxRoles );
            }

            if ( IsRoleNameTaken( name, exceptId: null ) )
            {
                return WorkspaceErrors.RoleNameTaken;
            }

            Role role = new( Guid.CreateVersion7(), Id, name, color, now );
            _roles.Add( role );
            Version++;
            return role;
        }

        public Result UpdateRole( Guid actorId, Guid roleId, string name, string color )
        {
            if ( !IsHead( actorId ) )
            {
                return WorkspaceErrors.NotHead;
            }

            Role? role = FindRole( roleId );
            if ( role is null )
            {
                return WorkspaceErrors.RoleNotFound;
            }

            if ( IsRoleNameTaken( name, exceptId: roleId ) )
            {
                return WorkspaceErrors.RoleNameTaken;
            }

            role.Rename( name, color );
            Version++;
            return Result.Success();
        }

        /// <summary>Удаляет роль и снимает её со всех участников. Привязки роли к узлам в Planning просто перестанут совпадать.</summary>
        public Result RemoveRole( Guid actorId, Guid roleId )
        {
            if ( !IsHead( actorId ) )
            {
                return WorkspaceErrors.NotHead;
            }

            Role? role = FindRole( roleId );
            if ( role is null )
            {
                return WorkspaceErrors.RoleNotFound;
            }

            foreach ( Member member in _members )
            {
                member.TakeRole( roleId );
            }

            _roles.Remove( role );
            Version++;
            return Result.Success();
        }

        /// <summary>Выдаёт участнику ровно этот набор ролей. Раздаёт роли только глава, и только роли этой команды.</summary>
        public Result SetMemberRoles( Guid actorId, Guid userId, IReadOnlyCollection<Guid> roleIds )
        {
            if ( !IsHead( actorId ) )
            {
                return WorkspaceErrors.NotHead;
            }

            Member? member = FindMember( userId );
            if ( member is null )
            {
                return WorkspaceErrors.MemberNotFound;
            }

            HashSet<Guid> wanted = [ .. roleIds ];
            if ( wanted.Any( id => FindRole( id ) is null ) )
            {
                return WorkspaceErrors.RoleNotFound;
            }

            member.SetRoles( wanted );
            Version++;
            return Result.Success();
        }

        public Role? FindRole( Guid roleId )
        {
            return _roles.FirstOrDefault( r => r.Id == roleId );
        }

        private bool IsRoleNameTaken( string name, Guid? exceptId )
        {
            return _roles.Any( r => r.Id != exceptId && string.Equals( r.Name, name, StringComparison.CurrentCultureIgnoreCase ) );
        }

        private int HeadCount => _members.Count( m => m.IsHead );
    }
}
