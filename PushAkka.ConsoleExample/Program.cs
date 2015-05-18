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
                        Token =
                            "http://s.notify.live.net/u/1/hk2/H2QAAABm3CnPfua9CJakzRQLwEDbsgH5pV5Fi-nLvjnlWLpejghb38gERasesGmn6u1uOF96Dt7wrxdAO0XwbueeePkRu2KWea-kx2jSlSAPZjcsRNOUb2TbyGsVPJBjDrBr7OU/d2luZG93c3Bob25lZGVmYXVsdA/siwVetYiJkGJhCC3_FYwsA/Etcl2CDCpC8Yv08grtwONdeem94",
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
                    var result = await push.Send(new WindowsPhoneToast()
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
