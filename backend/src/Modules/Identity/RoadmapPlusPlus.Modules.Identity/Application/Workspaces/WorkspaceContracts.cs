namespace RoadmapPlusPlus.Modules.Identity.Application.Workspaces
{
    internal sealed record CreateWorkspaceRequest( string Name, string? Color );

    internal sealed record UpdateWorkspaceRequest( string Name, string Color );

    internal sealed record SetHeadRequest( bool IsHead );

    internal sealed record WorkspaceSummary( Guid Id, string Name, string Color, bool IsHead, int MemberCount );

    internal sealed record WorkspaceDetails( Guid Id, string Name, string Color, bool IsHead, IReadOnlyList<MemberResponse> Members );

    internal sealed record MemberResponse( Guid UserId, string DisplayName, string Email, string Color, bool IsHead, DateTimeOffset JoinedAt );

    internal sealed record InvitationResponse( string Token, DateTimeOffset ExpiresAt );

    internal sealed record InvitationState( InvitationResponse? Active );

    internal sealed record InvitationPreview( Guid WorkspaceId, string WorkspaceName, string Color, int MemberCount, bool AlreadyMember );
}
