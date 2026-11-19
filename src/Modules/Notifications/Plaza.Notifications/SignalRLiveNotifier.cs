using Microsoft.AspNetCore.SignalR;
using Plaza.Notifications.Contracts;

namespace Plaza.Notifications;

internal sealed class SignalRLiveNotifier(IHubContext<NotificationsHub> hub) : ILiveNotifier
{
    public Task NotifyGroupAsync(LiveNotification notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        return hub.Clients
            .Group(NotificationChannels.ForGroup(notification.GroupId))
            .SendAsync(NotificationChannels.ClientMethod, notification, cancellationToken);
    }
}
