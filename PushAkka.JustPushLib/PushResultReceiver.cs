using Akka.Actor;
using PushAkka.Core.Actors;
using PushAkka.Core.Messages;

namespace PushAkka.JustPushLib
{
    class PushResultReceiver : BaseReceiveActor
    {
        private IActorRef _pushManager;
        private IActorRef _sender;

        public PushResultReceiver()
        {
            Receive<NotificationResult>(res =>
            {
                _sender.Tell(res);
            });

            Receive<BasePushMessage>(m =>
            {
                _sender = Sender;
                _pushManager.Tell(m);
            });
        }

        protected override void Unhandled(object message)
        {
            base.Unhandled(message);
        }

        protected override void PreStart()
        {
            _pushManager = Context.ActorOf(Props.Create<PushManager>(Self), "push_manager");
            base.PreStart();
        }
    }
}