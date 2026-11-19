using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Plaza.Groups.Contracts;
using Plaza.Notifications.Contracts;

namespace Plaza.Notifications;

/// <summary>
/// Connection gateway: each connection joins one SignalR group per neighbourhood group the user belongs to.
/// Clients only listen; they never send business commands over the hub.
/// </summary>
[Authorize]
internal sealed class NotificationsHub(IGroupDirectory groups) : Hub
{
    private const string SubscriptionsKey = "plaza.subscriptions";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            Context.Abort();
            return;
        }

        var joined = new List<string>();
        foreach (var membership in groups.MembershipsOf(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, NotificationChannels.ForGroup(membership.GroupId));
            joined.Add(membership.GroupId);
        }

        Context.Items[SubscriptionsKey] = joined;

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// The neighbourhood groups this connection receives notifications for. SignalR finishes
    /// <see cref="OnConnectedAsync"/> before it handles any invocation, so a reply here also means "ready".
    /// </summary>
    public IReadOnlyList<string> Subscriptions() =>
        Context.Items.TryGetValue(SubscriptionsKey, out var value) && value is List<string> joined ? joined : [];
}
