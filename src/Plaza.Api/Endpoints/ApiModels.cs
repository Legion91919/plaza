using Plaza.Feed.Contracts;
using Plaza.Groups.Contracts;

namespace Plaza.Api.Endpoints;

internal sealed record PostNoticeRequest(string? Body, PostKind? Kind);

internal sealed record RemovePostRequest(string? Reason);

internal sealed record GroupMembershipDto(string GroupId, string Name, string Area, GroupRole Role);

internal sealed record MeDto(string UserId, string DisplayName, bool IsVerified, IReadOnlyList<GroupMembershipDto> Groups);

internal sealed record GroupDto(string GroupId, string Name, string Area, int MemberCount, GroupRole YourRole);
