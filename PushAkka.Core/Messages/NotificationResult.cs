using System;

namespace PushAkka.Core.Messages
{
    public class NotificationResult
    {
        public Exception Error { get; set; }
        public bool IsSuccess { get { return Error == null; } }
        public Guid Id { get; set; }
    }
}