using System;

namespace PushAkka.Core.Messages
{
    public abstract class BaseWindowsPhonePushMessage
    {
        public string Uri { get; set; }

        public Guid MessageId { get; set; }

    }
}