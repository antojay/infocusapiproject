using System;
using System.Diagnostics;
using log4net;
namespace Infocus.Common.Logging
{
    public class Log4NetLogger : ILogger
    {
        protected ILog Logger
        {
            get;
            set;
        }
        protected internal Log4NetLogger(Type t)
        {
            Logger = LogManager.GetLogger(t);
        }


        public void LogCritical(Object message)
        {
            Logger.Fatal(message);
        }

        public void LogCritical(Object msg, Exception e)
        {
            Logger.Fatal(msg, e);
        }

        public void LogError(Object message)
        {
            Logger.Error(message);
        }

        public void LogError(Object msg, Exception ex)
        {
            Logger.Error(msg, ex);
        }

        public void LogWarning(Object message)
        {
            if(Logger.IsWarnEnabled)
            {
                Logger.Warn(message);
            }
        }

        public void LogWarning(Object message, Exception e)
        {
            if(Logger.IsWarnEnabled)
            {
                Logger.Warn(message, e);
            }
        }

        public void LogInfo(Object message)
        {
            if(Logger.IsInfoEnabled)
            {
                Logger.Info(message);
            }
        }

        public void LogInfo(Object msg, Exception e)
        {
            if(Logger.IsInfoEnabled)
            {
                Logger.Info(msg, e);
            }
        }

        public void LogDebug(Object message)
        {
            if(Logger.IsDebugEnabled)
            {
                Logger.Debug(message);
            }
        }

        public void LogTraceEntering(String methodName)
        {
            if(Logger.IsDebugEnabled)
            {
                LogDebug("Entering method: " + methodName);
            }
        }
        public void LogTraceLeaving(String methodName)
        {
            if(Logger.IsDebugEnabled)
            {
                LogDebug("Entering method: " + methodName);
            }
        }

        private static TraceEventType GetTraceEventType(LogSeverity severity)
        {
            switch(severity)
            {
                case LogSeverity.Critical:
                    return TraceEventType.Critical;
                case LogSeverity.Warning:
                    return TraceEventType.Warning;
                case LogSeverity.Error:
                    return TraceEventType.Error;
                case LogSeverity.Information:
                    return TraceEventType.Information;
                case LogSeverity.Debug:
                    return TraceEventType.Verbose;
            }
            return TraceEventType.Information;
        }
    }
}
