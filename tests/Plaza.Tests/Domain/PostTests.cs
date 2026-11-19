using Plaza.Feed.Contracts;
using Plaza.Feed.Domain;
using Plaza.SharedKernel;
using Plaza.Tests.Support;
using Xunit;

namespace Plaza.Domain.Tests;

public class PostTests
{
    private static readonly DateTimeOffset Now = ManualTimeProvider.DefaultStart;

    [Fact]
    public void Create_trims_the_body()
    {
        var post = Post.Create("p1", "g1", "u1", PostKind.Notice, "  Bins go out tonight.  ", Now);

        Assert.True(post.Succeeded);
        Assert.Equal("Bins go out tonight.", post.Value.Body);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("hi")]
    public void Create_rejects_a_body_that_is_too_short(string body)
    {
        var post = Post.Create("p1", "g1", "u1", PostKind.Notice, body, Now);

        Assert.False(post.Succeeded);
        Assert.Equal("body_too_short", post.Failure!.Code);
    }

    [Fact]
    public void Create_rejects_a_body_over_the_limit()
    {
        var post = Post.Create("p1", "g1", "u1", PostKind.Notice, new string('a', Post.MaxBodyLength + 1), Now);

        Assert.Equal(FailureKind.Validation, post.Failure!.Kind);
        Assert.Equal("body_too_long", post.Failure.Code);
    }

    [Fact]
    public void Create_rejects_an_unknown_kind()
    {
        var post = Post.Create("p1", "g1", "u1", (PostKind)42, "A valid body", Now);

        Assert.Equal("unknown_kind", post.Failure!.Code);
    }

    [Fact]
    public void Remove_records_who_and_when_and_cannot_happen_twice()
    {
        var post = Post.Create("p1", "g1", "u1", PostKind.Notice, "A valid body", Now).Value;

        var first = post.Remove("mod-1", Now.AddMinutes(5));
        var second = post.Remove("mod-2", Now.AddMinutes(6));

        Assert.True(first.Succeeded);
        Assert.True(post.IsRemoved);
        Assert.Equal("mod-1", post.RemovedBy);
        Assert.Equal(Now.AddMinutes(5), post.RemovedAt);
        Assert.Equal(FailureKind.Conflict, second.Failure!.Kind);
    }

    [Fact]
    public void Preview_is_one_line_and_at_most_140_characters()
    {
        var body = "Line one\nline two " + new string('x', 300);
        var post = Post.Create("p1", "g1", "u1", PostKind.Notice, body, Now).Value;

        var preview = post.Preview();

        Assert.Equal(140, preview.Length);
        Assert.DoesNotContain("\n", preview);
        Assert.StartsWith("Line one line two", preview);
    }
}
