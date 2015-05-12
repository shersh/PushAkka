using System;
using PushAkka.Core.Messages;

namespace PushAkka.Core.Actors
{
    public class WindowsPhonePushChannelException : Exception
    {
        public WpNotificationStatus Status { get; set; }

        public WindowsPhonePushChannelException(WpNotificationStatus status)
        {
            Status = status;
        }
    }
}