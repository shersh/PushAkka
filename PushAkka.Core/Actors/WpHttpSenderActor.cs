using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Akka.Actor;
using Akka.Monitoring;
using PushAkka.Core.Messages;

namespace PushAkka.Core.Actors
{
    public class WpHttpSenderActor : BaseReceiveActor
    {
        private IActorRef _parent = NoSender.Instance;
        private Request _request;

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

            Receive<Status.Failure>(fail =>
            {
                SendFail(fail.Cause);
            });

            Receive<Exception>(ex =>
            {
                SendFail(ex);
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
                    _parent.Tell(new SendSuccess() { Id = result.MessageId });
                }
            });

            Receive<Request>(req =>
            {
                Info("Sending request to \"{0}\"", req.Uri);
                _parent = Sender;
                _request = req;
                HttpClient client = new HttpClient();
                var request = CreateHttpRequest(req);
                client.SendAsync(request).PipeTo(Self);
            });
        }

        private void SendFail(Exception ex)
        {
            Warning("Sending Windows Phone notication has been failed.\nSee logs.");
            _parent.Tell(new SendFailed { Id = _request.MessageId, Reason = ex });
        }

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


        /// <summary>
        /// FOR TEST PURPOSE ONLY. Throws WebException. 
        /// </summary>
        /// <exception cref="WebException"></exception>
        private void Throw()
        {
            throw new WebException();
        }


        private HttpRequestMessage CreateHttpRequest(Request req)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, req.Uri) { Content = new StringContent(req.Content, Encoding.UTF8, "text/xml") };

            request.Headers.Add("X-NotificationClass", req.XNotificationClass);
            request.Headers.Add("X-WindowsPhone-Target", req.XWindowsPhoneTarget);
            request.Headers.Add("X-MessageID", req.MessageId.ToString());

            return request;
        }

        public class WpPushResult
        {
            public WpNotificationStatus NotificationStatus { get; set; }
            public long Id { get; set; }
            public Guid MessageId { get; set; }
        }
    }
}