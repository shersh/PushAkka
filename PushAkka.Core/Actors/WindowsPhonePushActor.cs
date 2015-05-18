using System;
using System.Collections;
using System.Net;
using System.Text;
using System.Xml.Linq;
using Akka.Actor;
using Akka.Monitoring;
using PushAkka.Core.Messages;

namespace PushAkka.Core.Actors
{
    /// <summary>
    /// Actor for Windows Phone push channel
    /// </summary>
    public class WindowsPhonePushActor : BaseReceiveActor, IWithUnboundedStash
    {
        /// <summary>
        /// Push Channel actor that working with network and send request
        /// </summary>
        private IActorRef _httpSender;

        /// <summary>
        /// Actor that initiated push notification
        /// </summary>
        private IActorRef _sender;

        /// <summary>
        /// The current message
        /// </summary>
        private BaseWindowsPhonePushMessage _currentMessage;

        private int _retryCount;

        public WindowsPhonePushActor()
        {
            CountOfReties = 5;
            Become(Ready);
        }

        /// <summary>
        /// Gets or sets the count of reties.
        /// </summary>
        /// <value>
        /// The count of reties to resend when WebException occured.
        /// </value>
        public int CountOfReties { get; set; }

        public IStash Stash { get; set; }

        /// <summary>
        /// Actor is ready for processing next message
        /// </summary>
        private void Ready()
        {
            Receive<WindowsPhoneToast>(toast =>
            {
                Context.IncrementCounter("windows_phone_toast_notification");
                if (_sender.IsNobody())
                    _sender = Sender;
                _currentMessage = toast;
                Become(Processing);
                var request = PrepareRequest(toast.Uri);
                request.XNotificationClass = "2";
                request.XWindowsPhoneTarget = "toast";
                request.MessageId = toast.MessageId;

                var payload = GetPayload(toast);

                Info("Payload: {0}", payload);

                //request.Content = Encoding.UTF8.GetBytes(payload);
                request.Content = payload;

                _httpSender.Tell(request);
            });
        }


        /// <summary>
        /// Gets the XML payload for sending to push-channel.
        /// </summary>
        /// <param name="push"></param>
        /// <returns></returns>
        private string GetPayload(WindowsPhoneToast push)
        {
            XNamespace wp = "WPNotification";
            var notification = new XElement(wp + "Notification", new XAttribute(XNamespace.Xmlns + "wp", "WPNotification"));

            var toast = new XElement(wp + "Toast");

            if (!string.IsNullOrEmpty(push.Text1))
                toast.Add(new XElement(wp + "Text1", push.Text1));

            if (!string.IsNullOrEmpty(push.Text2))
                toast.Add(new XElement(wp + "Text2", push.Text2));

            if (!string.IsNullOrEmpty(push.NavigatePath) || (push.Parameters != null && push.Parameters.Count > 0))
            {
                var sb = new StringBuilder();

                if (!string.IsNullOrEmpty(push.NavigatePath))
                    sb.Append("/" + push.NavigatePath.TrimStart('/'));

                if (push.Parameters != null && push.Parameters.Count > 0)
                {
                    sb.Append("?");

                    foreach (string key in push.Parameters.Keys)
                        sb.Append(key + "=" + push.Parameters[key] + "&");
                }

                var paramValue = sb.ToString();

                if (!string.IsNullOrEmpty(paramValue) && paramValue.EndsWith("&"))
                    paramValue = paramValue.Substring(0, paramValue.Length - 1);

                if (!string.IsNullOrEmpty(paramValue))
                    toast.Add(new XElement(wp + "Param", paramValue));
            }

            notification.Add(toast);
            return notification.ToString();
        }


        /// <summary>
        /// Prepares the request.
        /// </summary>
        /// <param name="uri">The URI of channel specified by device</param>
        /// <returns></returns>
        private Request PrepareRequest(string uri)
        {
            var request = new Request { Uri = uri };
            return request;
        }

        /// <summary>
        /// Actor is processing current message and will send <see cref="Busy">Busy</see> message
        /// </summary>
        private void Processing()
        {
            Receive<BaseWindowsPhonePushMessage>(push =>
            {
                Context.IncrementCounter("windows_phone_receive_when_busy");
                Stash.Stash();

                //Sender.Tell(new NotificationQueued() { Id = push.MessageId });
                //Sender.Tell(new Busy());
            });

            Receive<WpHttpSenderActor.SendFailed>(failed =>
            {
                if (failed.Reason is WebException && _retryCount++ < CountOfReties)
                {
                    Become(Ready);
                    Self.Tell(_currentMessage);
                }
                else
                {
                    _sender.Tell(new NotificationResult()
                    {
                        Error = failed.Reason,
                        Id = _currentMessage.MessageId
                    });
                    Clear();
                    Become(Ready);
                    Stash.Unstash();
                }
            });

            Receive<WpHttpSenderActor.SendSuccess>(res =>
            {
                _sender.Tell(new NotificationResult()
                {
                    Id = _currentMessage.MessageId
                });
                Clear();
                Become(Ready);
                Stash.Unstash();
            });
        }

        private void Clear()
        {
            _retryCount = 0;
            _sender = null;
        }

        protected override void PreRestart(Exception reason, object message)
        {
            Stash.UnstashAll();
            base.PreRestart(reason, message);
        }

        protected override void PreStart()
        {
            Info("Started");
            _httpSender = Context.ActorOf(Props.Create<WpHttpSenderActor>(), "push_sender");
            base.PreStart();
        }
    }

  
}