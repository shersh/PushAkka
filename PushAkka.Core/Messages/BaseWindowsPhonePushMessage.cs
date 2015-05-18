using System;

namespace PushAkka.Core.Messages
{
    public abstract class BaseWindowsPhonePushMessage : BasePushMessage
    {
        public string Uri { get; set; }
    }

    public abstract class BasePushMessage
    {
        public Guid MessageId { get; set; }
    }
}