using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace PushAkka.Core.Messages
{
    public abstract class BaseWindowsPhonePushMessage : BasePushMessage
    {
        public string Uri { get; set; }
    }

    public class GCMPushMessage : BasePushMessage
    {
        public string Destination { get; set; }

        public string AuthorizationToken { get; set; }

        public bool? DryRun
        {
            get;
            set;
        }

        public int? TimeToLive
        {
            get;
            set;
        }

        public bool? DelayWhileIdle
        {
            get;
            set;
        }

        public string JsonData
        {
            get;
            set;
        }

        public string CollapseKey
        {
            get;
            set;
        }

        public List<string> RegistrationIds
        {
            get;
            set;
        }

        public string NotificationKey { get; set; }

        public string RestrictedPackageName { get; set; }
    }

    public abstract class BasePushMessage
    {
        public Guid MessageId { get; set; }
    }
}