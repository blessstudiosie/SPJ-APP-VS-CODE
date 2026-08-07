using System;

namespace SPJ_APP.Service
{
    public class NotificationEventArgs : EventArgs
    {
        public string Title { get; }
        public string Message { get; }
        public Type TargetPageType { get; }

        public NotificationEventArgs(string title, string message, Type targetPageType)
        {
            Title = title;
            Message = message;
            TargetPageType = targetPageType;
        }
    }

    public static class NotificationService
    {
        public static event EventHandler<NotificationEventArgs>? OnNotificationReceived;

        public static void Notify(string title, string message, Type targetPageType)
        {
            OnNotificationReceived?.Invoke(null, new NotificationEventArgs(title, message, targetPageType));
        }
    }
}
