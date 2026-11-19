import { useEffect, useState } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import type { LiveNotification } from '../api/types';

export type LiveStatus = 'connecting' | 'live' | 'offline';

export const HUB_PATH = '/hubs/notifications';

/**
 * Connects to the notifications hub as the demo user. The server puts the connection in one SignalR group
 * per neighbourhood group the user belongs to. WebSockets can't send custom headers, so the demo identity
 * travels as the `demoUser` query parameter.
 */
export function useLiveNotifications(userId: string | null, onNotification: (n: LiveNotification) => void): LiveStatus {
  const [status, setStatus] = useState<LiveStatus>('connecting');

  useEffect(() => {
    if (!userId) return;
    let stopped = false;
    const connection = new HubConnectionBuilder()
      .withUrl(`${HUB_PATH}?demoUser=${encodeURIComponent(userId)}`)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('notification', (n: LiveNotification) => onNotification(n));
    connection.onreconnecting(() => setStatus('connecting'));
    connection.onreconnected(() => setStatus('live'));
    connection.onclose(() => !stopped && setStatus('offline'));

    setStatus('connecting');
    connection
      .start()
      .then(() => connection.invoke<string[]>('Subscriptions'))
      .then(() => !stopped && setStatus('live'))
      .catch(() => !stopped && setStatus('offline'));

    return () => {
      stopped = true;
      void connection.stop();
    };
  }, [userId]);

  return status;
}
