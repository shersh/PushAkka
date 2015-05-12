using System;
using System.Net;
using System.Text;
using Akka.Actor;
using Akka.Monitoring;
using PushAkka.Core.Messages;

namespace PushAkka.Core.Actors
{
    public class WpHttpSenderActor : BaseReceiveActor
    {
        internal class SendFailed
        {
            public Exception Reason { get; set; }
            public Guid Id { get; set; }
        }

        internal class SendSuccess
        {
            public Guid Id { get; set; }
        }

        public WpHttpSenderActor()
        {
            Context.IncrementActorCreated();
            Context.IncrementCounter("WpHttpSenderActor_created");

            Receive<Request>(req =>
            {
                Info("Sending request to \"{0}\"", req.Uri);
                var request = CreateRequest(req);
                WpPushResult result = null;

                try
                {
                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(req.Content, 0, req.Content.Length);
                    }

                    var response = request.GetResponse();
                    result = ParseResponce(response as HttpWebResponse);

                    if (result.NotificationStatus != WpNotificationStatus.Received)
                    {
                        Warning("Sending Windows Phone notication has been failed.\nStatus: {0}\nSee logs.", result.NotificationStatus);
                        Sender.Tell(new SendFailed { Id = req.MessageId, Reason = new WindowsPhonePushChannelException(result.NotificationStatus) });
                    }
                    else
                    {
                        Sender.Tell(new SendSuccess() { Id = req.MessageId });
                    }
                }
                catch (Exception ex)
                {
                    Error(ex, "Error during Windows Phone push-notification sending");
                    Sender.Tell(new SendFailed { Reason = ex });
                }

                Sender.Tell(result);
            });
        }


        /// <summary>
        /// FOR TEST PURPOSE ONLY. Throws WebException. 
        /// </summary>
        /// <exception cref="WebException"></exception>
        private void Throw()
        {
            throw new WebException();
        }


        /// <summary>
        /// Parses the HttpWebResponse responce and returns WpPushResult.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private WpPushResult ParseResponce(HttpWebResponse response)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("Responce: {1}, {2}\nFrom: {0}\n", response.ResponseUri, response.StatusCode, response.StatusDescription);
            foreach (var key in response.Headers.AllKeys)
            {
                var value = response.Headers.GetValues(key);
                builder.AppendFormat("    {0}: ", key);
                if (value != null)
                {
                    var @join = string.Join("; ", value);
                    builder.Append(string.Format("{0}", @join));
                }
                builder.AppendLine();
            }
            Info(builder.ToString());

            var res = new WpPushResult();

            var wpStatus = response.Headers["X-NotificationStatus"];
            var wpChannelStatus = response.Headers["X-SubscriptionStatus"];
            var wpDeviceConnectionStatus = response.Headers["X-DeviceConnectionStatus"];
            var messageId = response.Headers["X-MessageID"];

            WpNotificationStatus notStatus;
            Enum.TryParse(wpStatus, true, out notStatus);
            res.NotificationStatus = notStatus;

            return res;
        }


        /// <summary>
        /// Generates web request to push-channel
        /// </summary>
        /// <param name="req">The req.</param>
        /// <returns></returns>
        private WebRequest CreateRequest(Request req)
        {
            var request = WebRequest.Create(req.Uri);

            request.ContentType = "text/xml;charset=\"utf-8\"";
            request.Method = "POST";

            request.ContentType = req.ContentType;
            request.ContentLength = req.Content != null ? req.Content.Length : 0;

            request.Headers.Add("X-NotificationClass", req.XNotificationClass);
            request.Headers.Add("X-WindowsPhone-Target", req.XWindowsPhoneTarget);
            request.Headers.Add("X-MessageID", req.MessageId.ToString());

            return request;
        }

        public class WpPushResult
        {
            public WpNotificationStatus NotificationStatus { get; set; }
            public long Id { get; set; }
        }
    }
}