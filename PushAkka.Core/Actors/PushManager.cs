using Akka.Actor;
using PushAkka.Core.Messages;

namespace PushAkka.Core.Actors
{
    public class PushManager : BaseReceiveActor
    {
        private readonly IActorRef _whoWaitToReply;
        private IActorRef _wpCoordinator;

        public PushManager(IActorRef whoWaitToReply)
        {
            _whoWaitToReply = whoWaitToReply;
            Receive<BaseWindowsPhonePushMessage>(push => _wpCoordinator.Forward(push));
        }

        protected override void Unhandled(object message)
        {
            base.Unhandled(message);
        }

        protected override void PreStart()
        {
            _wpCoordinator = Context.ActorOf(Props.Create<WinphonePushCoordinator>(_whoWaitToReply), "WinPhoneCoordinator");

            base.PreStart();
        }
    }
}
