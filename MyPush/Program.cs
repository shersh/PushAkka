using System;
using Akka.Actor;
using CommandLine;
using CommandLine.Text;
using PushAkka.Core.Actors;
using PushAkka.Core.Messages;

namespace PushAkka.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var system = ActorSystem.Create("AkkaPush", "akka { logLevel=DEBUG }");
            var pushManager = system.ActorOf<PushManager>("push_manager");

            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                switch (options.Type)
                {
                    case DeviceType.WinPhone:
                        pushManager.Tell(new WindowsPhoneToast()
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

            Console.ReadLine();
        }
    }

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


    internal enum DeviceType
    {
        WinPhone,
        Windows,
        Android,
        Apple
    }
}
