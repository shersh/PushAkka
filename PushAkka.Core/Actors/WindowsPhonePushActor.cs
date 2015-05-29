using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        /// Actor that initiated push notification
        /// </summary>
        private readonly IActorRef _whoWaitReply;

        /// <summary>
        /// Push Channel actor that working with network and send request
        /// </summary>
        private IActorRef _httpSender;

        /// <summary>
        /// The current message
        /// </summary>
        private BaseWindowsPhonePushMessage _currentMessage;

        public WindowsPhonePushActor(IActorRef whoWaitReply)
        {
            _whoWaitReply = whoWaitReply;
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
                _currentMessage = toast;
                Become(Processing);
                var payload = GetPayload(toast);
                var request = CreateHttpRequest(new Request() { XNotificationClass = "2", XWindowsPhoneTarget = "toast", MessageId = toast.MessageId, Content = payload, Uri = toast.Uri });
                Info("Payload: {0}", payload);
                _httpSender.Tell(request);
            });

            Receive<WindowsPhoneTile>(tile =>
            {
                Context.IncrementCounter("windows_phone_tile_notification");
                _currentMessage = tile;
                Become(Processing);
                var payload = GetPayload(tile);
                var request = CreateHttpRequest(new Request() { XNotificationClass = "1", XWindowsPhoneTarget = "token", MessageId = tile.MessageId, Content = payload, Uri = tile.Uri });
                Info("Payload: {0}", payload);
                _httpSender.Tell(request);
            });

            Receive<WindowsPhoneRaw>(raw =>
            {
                Context.IncrementCounter("windows_phone_raw_notification");
                _currentMessage = raw;
                Become(Processing);
                var payload = GetPayload(raw);
                var request = CreateHttpRequest(new Request() { XNotificationClass = "3", XWindowsPhoneTarget = "", MessageId = raw.MessageId, Content = payload, Uri = raw.Uri });
                Info("Payload: {0}", payload);
                _httpSender.Tell(request);
            });
        }


        /// <summary>
        /// Gets the payload for raw push notification.
        /// </summary>
        /// <param name="push">The push.</param>
        /// <returns></returns>
        private string GetPayload(WindowsPhoneRaw push)
        {
            // Create the raw message.
            string rawMessage = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<root>" +
                "<Value1>" + push.Value1 + "<Value1>" +
                "<Value2>" + push.Value2 + "<Value2>" +
            "</root>";
            return rawMessage;
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
        /// Gets the payload for tile push notification.
        /// </summary>
        /// <param name="push">The push.</param>
        /// <returns></returns>
        private string GetPayload(WindowsPhoneTile push)
        {
            string tileMessage = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                  "<wp:Notification xmlns:wp=\"WPNotification\">" +
                      "<wp:Tile>" +
                        "<wp:BackgroundImage>" + push.BackgroundImage + "</wp:BackgroundImage>" +
                        "<wp:Count>" + push.Count + "</wp:Count>" +
                        "<wp:Title>" + push.Title + "</wp:Title>" +
                        "<wp:BackBackgroundImage>" + push.BackBackgroundImage + "</wp:BackBackgroundImage>" +
                        "<wp:BackTitle>" + push.BackTitle + "</wp:BackTitle>" +
                        "<wp:BackContent>" + push.BackContent + "</wp:BackContent>" +
                     "</wp:Tile> " +
                  "</wp:Notification>";

            return tileMessage;
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
            });

            Receive<Exception>(failed =>
            {
                SendFail(failed);
                Become(Ready);
                Stash.Unstash();
            });

            Receive<HttpResponseMessage>(responce =>
            {
                var result = ParseResponce(responce);
                if (result.NotificationStatus != WpNotificationStatus.Received)
                {
                    SendFail(new WindowsPhonePushChannelException(result.NotificationStatus));
                }
                else
                {
                    _whoWaitReply.Tell(new NotificationResult()
                    {
                        Id = _currentMessage.MessageId
                    });
                }

                Become(Ready);
                Stash.Unstash();
            });
        }

        /// <summary>
        /// Reply to waiting actor that sending has been failed.
        /// </summary>
        /// <param name="ex">The exception occured during sending</param>
        private void SendFail(Exception ex)
        {
            Warning("Sending Windows Phone notication has been failed.\nSee logs.");
            _whoWaitReply.Tell(new NotificationResult()
            {
                Id = _currentMessage.MessageId,
                Error = ex
            });
        }

        /// <summary>
        /// Parses the responce.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns></returns>
        private WpPushResult ParseResponce(HttpResponseMessage response)
        {
            var wpStatus = "";
            var wpChannelStatus = "";
            var wpDeviceConnectionStatus = "";
            var messageId = "";


            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("Responce: {1}, {2}\nFrom: {0}\n", response.RequestMessage.RequestUri, response.StatusCode, response.ReasonPhrase);
            foreach (var key in response.Headers)
            {
                builder.AppendFormat("    {0}: {1}", key.Key, string.Join(";", key.Value));
                builder.AppendLine();
                switch (key.Key)
                {
                    case "X-NotificationStatus":
                        wpStatus = key.Value.FirstOrDefault();
                        break;
                    case "X-SubscriptionStatus":
                        wpChannelStatus = key.Value.FirstOrDefault();
                        break;
                    case "X-DeviceConnectionStatus":
                        wpDeviceConnectionStatus = key.Value.FirstOrDefault();
                        break;
                    case "X-MessageID":
                        messageId = key.Value.FirstOrDefault();
                        break;
                }
            }
            Info(builder.ToString());

            var res = new WpPushResult();

            WpNotificationStatus notStatus;
            Enum.TryParse(wpStatus, true, out notStatus);
            res.NotificationStatus = notStatus;

            if (!string.IsNullOrEmpty(messageId))
                res.MessageId = Guid.Parse(messageId);

            return res;
        }

        protected override void PreRestart(Exception reason, object message)
        {
            Stash.UnstashAll();
            base.PreRestart(reason, message);
        }

        protected override void PreStart()
        {
            _httpSender = Context.ActorOf(Props.Create<HttpSenderActor>(), "push_sender");
            base.PreStart();
        }

        /// <summary>
        /// Creates the HTTP request.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <returns></returns>
        private HttpRequestMessage CreateHttpRequest(Request req)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, req.Uri) { Content = new StringContent(req.Content, Encoding.UTF8, "text/xml") };

            request.Headers.Add("X-NotificationClass", req.XNotificationClass);
            request.Headers.Add("X-WindowsPhone-Target", req.XWindowsPhoneTarget);
            request.Headers.Add("X-MessageID", req.MessageId.ToString());

            return request;
        }
    }

    internal class WpPushResult
    {
        public WpNotificationStatus NotificationStatus { get; set; }
        public Guid MessageId { get; set; }
    }
}