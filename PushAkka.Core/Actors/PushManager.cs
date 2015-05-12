using Akka.Actor;
using PushAkka.Core.Messages;

namespace PushAkka.Core.Actors
{
    public class PushManager : BaseReceiveActor
    {
        private IActorRef _wpCoordinator;

        public PushManager()
        {
            Receive<BaseWindowsPhonePushMessage>(push => _wpCoordinator.Forward(push));
        }

        protected override void Unhandled(object message)
        {
            base.Unhandled(message);
        }

        protected override void PreStart()
        {
            _wpCoordinator = Context.ActorOf<WinphonePushCoordinator>("WinPhoneCoordinator");

            base.PreStart();
        }
    }
}
