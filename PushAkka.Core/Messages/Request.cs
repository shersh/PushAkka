using System;

namespace PushAkka.Core.Messages
{
    public class Request
    {
        public string Uri { get; set; }
        public string Content { get; set; }
        public string ContentType { get; set; }
        public string Method { get; set; }
        public string XNotificationClass { get; set; }
        public string XWindowsPhoneTarget { get; set; }
        public Guid MessageId { get; set; }
    }
}