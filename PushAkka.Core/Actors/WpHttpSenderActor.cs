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
    public class HttpSenderActor : BaseReceiveActor
    {
        private HttpClient _client;

        public HttpSenderActor()
        {
            Context.IncrementActorCreated();
            Context.IncrementCounter("HttpSenderActor_created");

            Receive<HttpRequestMessage>(req =>
            {
                Info("Sending request to \"{0}\"", req.RequestUri);
                _client.SendAsync(req).PipeTo(Sender);
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

        protected override void PreStart()
        {
            _client = new HttpClient();
            base.PreStart();
        }
    }
}