using System;
using Akka.Actor;
using Akka.Event;

namespace PushAkka.Core.Actors
{
    public class BaseReceiveActor : ReceiveActor
    {
        protected ILoggingAdapter _log;

        public BaseReceiveActor()
        {
            _log = Context.GetLogger();
        }

        protected void Debug(string format, params object[] objects)
        {
            _log.Debug(format, objects);
        }

        protected void Info(string format, params object[] objects)
        {
            _log.Info(format, objects);
        }

        protected void Warning(string format, params object[] objects)
        {
            _log.Warning(format, objects);
        }

        protected void Error(string format, params object[] objects)
        {
            _log.Error(format, objects);
        }

        protected void Error(Exception ex, string format, params object[] objects)
        {
            _log.Error(ex, format, objects);
        }
    }
}