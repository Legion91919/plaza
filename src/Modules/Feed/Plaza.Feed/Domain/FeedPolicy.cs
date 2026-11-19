using Plaza.Groups.Contracts;
using Plaza.SharedKernel;

namespace Plaza.Feed.Domain;

/// <summary>Who may do what in a group feed. Pure rules, no I/O.</summary>
internal static class FeedPolicy
{
    public const int MinReasonLength = 5;
    public const int MaxReasonLength = 500;

    public static Failure? CanRead(GroupRole? role) =>
        role is null ? Failure.Forbidden("not_a_member", "Only members of this group can read its feed.") : null;

    public static Failure? CanPost(GroupRole? role, bool isVerified)
    {
        if (role is null)
        {
            return Failure.Forbidden("not_a_member", "Only members of this group can post here.");
        }

        return isVerified
            ? null
            : Failure.Forbidden("not_verified", "Verify your address before posting to your neighbourhood.");
    }

    public static Failure? CanModerate(GroupRole? role) =>
        role == GroupRole.Moderator ? null : Failure.Forbidden("not_a_moderator", "Only moderators of this group can do this.");

    public static Failure? ValidateReason(string? reason)
    {
        var text = reason?.Trim() ?? string.Empty;
        if (text.Length < MinReasonLength)
        {
            return Failure.Validation("reason_required", $"Give a reason of at least {MinReasonLength} characters.");
        }

        return text.Length > MaxReasonLength
            ? Failure.Validation("reason_too_long", $"Keep the reason under {MaxReasonLength} characters.")
            : null;
    }
}
