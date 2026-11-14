namespace RoadmapPlusPlus.Modules.Identity.Application.Workspaces
{
    internal sealed record CreateWorkspaceRequest( string Name, string? Color );

    internal sealed record UpdateWorkspaceRequest( string Name, string Color );

    internal sealed record SetHeadRequest( bool IsHead );

    internal sealed record SetRolesEnabledRequest( bool Enabled );

    /// <summary>Роль без цвета получает следующий цвет из палитры, чтобы соседние роли различались.</summary>
    internal sealed record SaveRoleRequest( string Name, string? Color );

    internal sealed record SetMemberRolesRequest( IReadOnlyList<Guid> RoleIds );

    internal sealed record WorkspaceSummary( Guid Id, string Name, string Color, bool IsHead, int MemberCount );

    /// <summary>
    /// Команда для её страницы. Роли и то, у кого какие роли, видят все участники, как в Discord,
    /// а менять их может только глава.
    /// </summary>
    internal sealed record WorkspaceDetails(
        Guid Id,
        string Name,
        string Color,
        bool IsHead,
        bool RolesEnabled,
        IReadOnlyList<RoleResponse> Roles,
        IReadOnlyList<MemberResponse> Members );

    internal sealed record RoleResponse( Guid Id, string Name, string Color, int MemberCount );

    internal sealed record MemberResponse(
        Guid UserId,
        string DisplayName,
        string Email,
        string Color,
        bool IsHead,
        DateTimeOffset JoinedAt,
        IReadOnlyList<Guid> RoleIds );

    internal sealed record InvitationResponse( string Token, DateTimeOffset ExpiresAt );

    internal sealed record InvitationState( InvitationResponse? Active );

    internal sealed record InvitationPreview( Guid WorkspaceId, string WorkspaceName, string Color, int MemberCount, bool AlreadyMember );
}
