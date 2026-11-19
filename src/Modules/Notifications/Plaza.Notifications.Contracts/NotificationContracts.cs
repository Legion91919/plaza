namespace Plaza.Notifications.Contracts;

/// <summary>A live notification pushed to the online members of one neighbourhood group.</summary>
/// <param name="Type">One of <see cref="NotificationTypes"/>.</param>
/// <param name="Kind">The post kind, e.g. "notice" or "safetyAlert".</param>
/// <param name="Preview">At most 140 characters of the post body.</param>
public sealed record LiveNotification(
    string Type,
    string GroupId,
    string PostId,
    string AuthorId,
    string AuthorName,
    string Kind,
    string Preview,
    DateTimeOffset OccurredAt);

public static class NotificationTypes
{
    public const string PostCreated = "post.created";
    public const string PostRemoved = "post.removed";
}

/// <summary>Wire names shared by the server, tests and the web client.</summary>
public static class NotificationChannels
{
    public const string HubPath = "/hubs/notifications";

    /// <summary>The client-side method name the hub invokes.</summary>
    public const string ClientMethod = "notification";

    /// <summary>One SignalR group per neighbourhood group.</summary>
    public static string ForGroup(string groupId) => $"group:{groupId}";
}

/// <summary>Port other modules use to reach online members. Implemented by the Notifications module.</summary>
public interface ILiveNotifier
{
    Task NotifyGroupAsync(LiveNotification notification, CancellationToken cancellationToken);
}
