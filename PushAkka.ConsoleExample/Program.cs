using System;
using System.Threading.Tasks;
using Akka.Actor;
using CommandLine;
using CommandLine.Text;
using PushAkka.Core.Actors;
using PushAkka.Core.Messages;
using PushAkka.JustPushLib;
using Serilog;

namespace PushAkka.ConsoleTest
{
    class Program
    {
        private static IActorRef _pushManager;

        static void Main(string[] args)
        {
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                Send(options);
                Console.ReadLine();
            }
            // For debug only
#if DEBUG
            else
            {
                for (int i = 0; i < 1; i++)
                {
                    Send(new Options()
                    {
                        Token = "",
                        Text = i.ToString(),
                        Type = DeviceType.WinPhone
                    });
                }
                Console.ReadLine();
            }
#endif
        }

        private static async void Send(Options options)
        {
            JustPush push = new JustPush();

            switch (options.Type)
            {
                case DeviceType.WinPhone:
                    var result = await push.Send(new WindowsPhoneTile()
                    {
                        MessageId = Guid.NewGuid(),
                        BackgroundImage = "for_test.jpg",
                        Count = 11,
                        BackContent = "Hello from PushAkka",
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
