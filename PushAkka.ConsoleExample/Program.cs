using System;
using Akka.Actor;
using CommandLine;
using CommandLine.Text;
using PushAkka.Core.Actors;
using PushAkka.Core.Messages;
using Serilog;

namespace PushAkka.ConsoleTest
{
    class PushResultReceiver : BaseReceiveActor
    {
        public PushResultReceiver()
        {
            Receive<NotificationResult>(res =>
            {

            });
        }

        protected override void Unhandled(object message)
        {
            base.Unhandled(message);
        }
    }

    class Program
    {
        private static IActorRef _pushManager;

        static void Main(string[] args)
        {
            var logger = new LoggerConfiguration()
                .WriteTo
                //.File("E:\\akka.log")
                .ColoredConsole()
                .MinimumLevel.Information()
                .CreateLogger();

            Serilog.Log.Logger = logger;

            var system = ActorSystem.Create("AkkaPush");
            var receiver = system.ActorOf<PushResultReceiver>();
            _pushManager = system.ActorOf(Props.Create<PushManager>(receiver), "push_manager");

            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                Send(options);
                Console.ReadLine();
            }
            else
            {
                for (int i = 0; i < 1; i++)
                {
                    Send(new Options()
                    {
                        Token =
                            "http://s.notify.live.net/u/1/hk2/H2QAAABm3CnPfua9CJakzRQLwEDbsgH5pV5Fi-nLvjnlWLpejghb38gERasesGmn6u1uOF96Dt7wrxdAO0XwbueeePkRu2KWea-kx2jSlSAPZjcsRNOUb2TbyGsVPJBjDrBr7OU/d2luZG93c3Bob25lZGVmYXVsdA/siwVetYiJkGJhCC3_FYwsA/Etcl2CDCpC8Yv08grtwONdeem94",
                        Text = i.ToString(),
                        Type = DeviceType.WinPhone
                    });
                }
                Console.ReadLine();
            }
        }

        private static void Send(Options options)
        {
            switch (options.Type)
            {
                case DeviceType.WinPhone:
                    _pushManager.Tell(new WindowsPhoneToast()
                    {
                        MessageId = Guid.NewGuid(),
                        Text1 = options.Text,
                        Uri = options.Token
                    });
                    break;
                case DeviceType.Windows:
                    break;
                case DeviceType.Android:
                    break;
                case DeviceType.Apple:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    /// <summary>
    /// Command line options
    /// </summary>
    class Options
    {
        [Option('d', "dev_type", Required = true,
          HelpText = "Type of device. Available types [WinPhone, Windows, Android, Apple]")]
        public DeviceType Type { get; set; }

        [Option('t', "token", Required = true,
          HelpText = "Device token url")]
        public string Token { get; set; }

        [Option("text", Required = true,
          HelpText = "Device token url")]
        public string Text { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

    /// <summary>
    /// Type of device specified in command line
    /// </summary>
    internal enum DeviceType
    {
        WinPhone,
        Windows,
        Android,
        Apple
    }
}
