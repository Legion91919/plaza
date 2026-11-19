using Plaza.Feed.Domain;
using Plaza.Groups.Contracts;
using Plaza.SharedKernel;
using Xunit;

namespace Plaza.Domain.Tests;

public class FeedPolicyTests
{
    [Fact]
    public void Non_members_cannot_read()
    {
        var denied = FeedPolicy.CanRead(null);

        Assert.Equal(FailureKind.Forbidden, denied!.Kind);
        Assert.Equal("not_a_member", denied.Code);
    }

    [Fact]
    public void Members_and_moderators_can_read()
    {
        Assert.Null(FeedPolicy.CanRead(GroupRole.Member));
        Assert.Null(FeedPolicy.CanRead(GroupRole.Moderator));
    }

    [Fact]
    public void Only_verified_members_can_post()
    {
        Assert.Null(FeedPolicy.CanPost(GroupRole.Member, isVerified: true));
        Assert.Equal("not_verified", FeedPolicy.CanPost(GroupRole.Member, isVerified: false)!.Code);
        Assert.Equal("not_a_member", FeedPolicy.CanPost(null, isVerified: true)!.Code);
    }

    [Fact]
    public void Only_moderators_can_moderate()
    {
        Assert.Null(FeedPolicy.CanModerate(GroupRole.Moderator));
        Assert.Equal("not_a_moderator", FeedPolicy.CanModerate(GroupRole.Member)!.Code);
        Assert.Equal("not_a_moderator", FeedPolicy.CanModerate(null)!.Code);
    }

    [Theory]
    [InlineData(null, "reason_required")]
    [InlineData("", "reason_required")]
    [InlineData("spam", "reason_required")]
    public void Removal_needs_a_reason(string? reason, string code)
    {
        Assert.Equal(code, FeedPolicy.ValidateReason(reason)!.Code);
    }

    [Fact]
    public void A_clear_reason_is_accepted_and_an_essay_is_not()
    {
        Assert.Null(FeedPolicy.ValidateReason("Advertising, not a neighbourhood notice"));
        Assert.Equal("reason_too_long", FeedPolicy.ValidateReason(new string('r', 501))!.Code);
    }
}
