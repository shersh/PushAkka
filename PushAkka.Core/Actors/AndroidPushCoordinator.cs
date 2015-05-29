using Akka.Actor;
using Akka.Monitoring;
using Akka.Routing;
using PushAkka.Core.Messages;

namespace PushAkka.Core.Actors
{
    public class AndroidPushCoordinator : BaseReceiveActor
    {
        private readonly IActorRef _whoWaitToReply;
        private IActorRef _gcmPushRouter;

        public AndroidPushCoordinator(IActorRef whoWaitToReply)
        {
            _whoWaitToReply = whoWaitToReply;
            Receive<GCMPushMessage>(push =>
            {
                _gcmPushRouter.Tell(push);
                Context.IncrementCounter("android_gcm_push_notification");
            });
        }

        protected override void Unhandled(object message)
        {
            Info("Unhandled: " + message);
            Context.IncrementUnhandledMessage();
            base.Unhandled(message);
        }

        protected override void PreStart()
        {
            _gcmPushRouter = Context
                .ActorOf(Props.Create<GCMPushActor>(_whoWaitToReply)
                    .WithRouter(FromConfig.Instance.WithFallback(new NoRouter())),
                    "gcm_push_actor");
            base.PreStart();
        }
    }
}