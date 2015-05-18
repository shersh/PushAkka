using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using PushAkka.Core.Messages;
using Serilog;

namespace PushAkka.JustPushLib
{
    public class JustPush
    {
        private readonly IActorRef _receiver;

        public JustPush()
        {
            var logger = new LoggerConfiguration()
             .WriteTo
                //.File("E:\\akka.log")
             .ColoredConsole()
             .MinimumLevel.Information()
             .CreateLogger();

            Serilog.Log.Logger = logger;

            var system = ActorSystem.Create("AkkaPush");
            _receiver = system.ActorOf(Props.Create<PushResultReceiver>(), "sender");
        }

        public Task<NotificationResult> Send(BasePushMessage message)
        {
            return _receiver.Ask<NotificationResult>(message);
        }
    }
}
