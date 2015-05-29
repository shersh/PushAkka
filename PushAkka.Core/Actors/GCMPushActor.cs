using System;
using System.Net.Http;
using System.Text;
using Akka.Actor;
using Akka.Monitoring;
using Newtonsoft.Json.Linq;
using PushAkka.Core.Messages;

namespace PushAkka.Core.Actors
{
    public class GCMPushActor : BaseReceiveActor, IWithUnboundedStash
    {
        public const string GCMSendEndpoint = "https://gcm-http.googleapis.com/gcm/send";

        public IStash Stash { get; set; }

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
        private GCMPushMessage _currentMessage;

        public GCMPushActor(IActorRef whoWaitReply)
        {
            _whoWaitReply = whoWaitReply;
            Become(Ready);
        }

        /// <summary>
        /// Actor is ready for processing next message
        /// </summary>
        private void Ready()
        {
            Receive<GCMPushMessage>(gcm =>
            {
                Become(Processing);
                var request = CreateHttpRequest(gcm);
                _httpSender.Tell(request);
            });

            Receive<Exception>(failed =>
            {
                SendFail(failed);
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
            Warning("Sending Android notication has been failed.\nSee logs.");
            _whoWaitReply.Tell(new NotificationResult()
            {
                Id = _currentMessage.MessageId,
                Error = ex
            });
        }

        private void Processing()
        {
            Receive<GCMPushMessage>(gcm =>
            {
                Context.IncrementCounter("android_gcm_receive_when_busy");
                Stash.Stash();
            });
        }

        private string GetPayload(GCMPushMessage msg)
        {
            var json = new JObject();

            json["to"] = msg.Destination;

            if (!string.IsNullOrEmpty(msg.CollapseKey))
                json["collapse_key"] = msg.CollapseKey;

            if (msg.TimeToLive.HasValue)
                json["time_to_live"] = msg.TimeToLive.Value;

            if (msg.RegistrationIds != null && msg.RegistrationIds.Count > 0)
                json["registration_ids"] = new JArray(msg.RegistrationIds.ToArray());

            if (msg.DelayWhileIdle.HasValue)
                json["delay_while_idle"] = msg.DelayWhileIdle.Value;

            if (msg.DryRun.HasValue && msg.DryRun.Value)
                json["dry_run"] = true;

            if (!string.IsNullOrEmpty(msg.JsonData))
            {
                var jsonData = JObject.Parse(msg.JsonData);

                if (jsonData != null)
                    json["data"] = jsonData;
            }

            if (!string.IsNullOrWhiteSpace(msg.NotificationKey))
                json["notification_key"] = msg.NotificationKey;

            if (!string.IsNullOrWhiteSpace(msg.RestrictedPackageName))
                json["restricted_package_name"] = msg.RestrictedPackageName;

            return json.ToString();
        }

        /// <summary>
        /// Creates the HTTP request.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <returns></returns>
        private HttpRequestMessage CreateHttpRequest(GCMPushMessage req)
        {
            var payload = GetPayload(req);
            Info("Payload: {0}", payload);
            var request = new HttpRequestMessage(HttpMethod.Post, GCMSendEndpoint) { Content = new StringContent(payload, Encoding.UTF8, "application/json") };

            request.Headers.Add("Authorization", "key=" + req.AuthorizationToken);

            return request;
        }


        protected override void PreStart()
        {
            _httpSender = Context.ActorOf(Props.Create<HttpSenderActor>(), "push_sender");
            base.PreStart();
        }
    }
}