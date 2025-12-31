import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { NotificationMessage } from './signalr.service';

export interface UiNotification extends NotificationMessage {
  id: string;
  isRead: boolean;
  dismissed: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private readonly notifications = new BehaviorSubject<UiNotification[]>([]);
  private readonly maxNotifications = 50;
  private notificationCounter = 0;

  public readonly notifications$ = this.notifications.asObservable();

  constructor() {}

  /**
   * Add a new notification
   */
  public addNotification(notification: NotificationMessage): void {
    const uiNotification: UiNotification = {
      ...notification,
      id: `notification-${++this.notificationCounter}`,
      isRead: false,
      dismissed: false
    };

    const current = this.notifications.value;
    const updated = [uiNotification, ...current].slice(0, this.maxNotifications);
    this.notifications.next(updated);

    // Auto-dismiss non-persistent notifications after 5 seconds
    if (!notification.persistent) {
      setTimeout(() => {
        this.dismissNotification(uiNotification.id);
      }, 5000);
    }
  }

  /**
   * Mark notification as read
   */
  public markAsRead(id: string): void {
    const current = this.notifications.value;
    const updated = current.map(notification =>
      notification.id === id ? { ...notification, isRead: true } : notification
    );
    this.notifications.next(updated);
  }

  /**
   * Mark all notifications as read
   */
  public markAllAsRead(): void {
    const current = this.notifications.value;
    const updated = current.map(notification => ({ ...notification, isRead: true }));
    this.notifications.next(updated);
  }

  /**
   * Dismiss a notification
   */
  public dismissNotification(id: string): void {
    const current = this.notifications.value;
    const updated = current.filter(notification => notification.id !== id);
    this.notifications.next(updated);
  }

  /**
   * Clear all notifications
   */
  public clearAll(): void {
    this.notifications.next([]);
  }

  /**
   * Get unread notification count
   */
  public getUnreadCount(): Observable<number> {
    return new BehaviorSubject(
      this.notifications.value.filter(n => !n.isRead && !n.dismissed).length
    ).asObservable();
  }

  /**
   * Show a success notification
   */
  public showSuccess(title: string, message: string, persistent = false): void {
    this.addNotification({
      type: 'success',
      title,
      message,
      timestamp: new Date(),
      persistent
    });
  }

  /**
   * Show an error notification
   */
  public showError(title: string, message: string, persistent = true): void {
    this.addNotification({
      type: 'error',
      title,
      message,
      timestamp: new Date(),
      persistent
    });
  }

  /**
   * Show a warning notification
   */
  public showWarning(title: string, message: string, persistent = false): void {
    this.addNotification({
      type: 'warning',
      title,
      message,
      timestamp: new Date(),
      persistent
    });
  }

  /**
   * Show an info notification
   */
  public showInfo(title: string, message: string, persistent = false): void {
    this.addNotification({
      type: 'info',
      title,
      message,
      timestamp: new Date(),
      persistent
    });
  }
}