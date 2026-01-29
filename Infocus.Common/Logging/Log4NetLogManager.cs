using System;
using System.Diagnostics;
using log4net;
using log4net.Core;
using log4net.Repository.Hierarchy;
using log4net.Appender;
namespace Infocus.Common.Logging
{
    public sealed class Log4NetLogManager : ILogManager
    {
        private const String DefaultLogPath = "log/log.xml";
        public void Initialize()
        {
            Initialize(LogSeverity.Information, DefaultLogPath);
        }
        public void Initialize(LogSeverity defaultSeverity)
        {
            Initialize(defaultSeverity, DefaultLogPath);
        }
        public void Initialize(LogSeverity defaultSeverity, String fileName)
        {
            Hierarchy repository = (Hierarchy)LogManager.GetRepository();
            Logger root = repository.Root;
            root.Level = GetLog4NetLevel(defaultSeverity);
            root.AddAppender(GetConsoleAppender(defaultSeverity));
            root.AddAppender(GetFileAppender(defaultSeverity, fileName, 15000, true, "RootAppender"));

            //Logger l = (Logger)repository.GetLogger("NHibernate");
            //l.Additivity = false;
            //l.AddAppender(GetFileAppender(FusionFrameworkProperties.Instance.LogLevel,
            //    FusionFrameworkProperties.Instance.ObjectAccessLogFileName, FusionFrameworkProperties.Instance.LogFileSize, true, "NHibernateFileLog"));
            //l.Level = GetLog4NetLevel(FusionFrameworkProperties.Instance.LogLevel);

            root.Repository.Configured = true;
        }

        private static ConsoleAppender GetConsoleAppender(LogSeverity logLevel)
        {
            ConsoleAppender lAppender = new ConsoleAppender
            {
                Name = "Console",
                Layout = new log4net.Layout.PatternLayout("%date{dd-MM-yyyy HH:mm:ss,fff} %5level [%2thread]  <%logger{1}>: %message %n"),
                Threshold = GetLog4NetLevel(logLevel)
            };
            lAppender.ActivateOptions();
            return lAppender;
        }

        private static FileAppender GetFileAppender(LogSeverity logLevel, String fileName, Int32 fileSize, Boolean staticLog, String loggerName)
        {
            RollingFileAppender lAppender = new RollingFileAppender
            {
                Name = loggerName,
                AppendToFile = true,
                MaximumFileSize = String.Format("{0}KB", fileSize),
                LockingModel = new FileAppender.MinimalLock(),
                MaxSizeRollBackups = 20,
                File = fileName,
                Layout = new log4net.Layout.PatternLayout("%date{dd-MM-yyyy HH:mm:ss,fff} %5level [%2thread]  <%logger{1}>: %message %n"),
                Threshold = GetLog4NetLevel(logLevel)
            };
            if(staticLog)
            {
                lAppender.StaticLogFileName = staticLog;
            }
            lAppender.ActivateOptions();

            return lAppender;
        }

        private static log4net.Core.Level GetLog4NetLevel(LogSeverity level)
        {
            switch(level)
            {
                case LogSeverity.Debug:
                    return log4net.Core.Level.Debug;
                case LogSeverity.Error:
                    return log4net.Core.Level.Error;
                case LogSeverity.Critical:
                    return log4net.Core.Level.Fatal;
                case LogSeverity.Information:
                    return log4net.Core.Level.Info;
                case LogSeverity.Warning:
                    return log4net.Core.Level.Warn;
                default:
                    return log4net.Core.Level.Error;
            }

        }

        public ILogger GetLogger(Type type)
        {
            return new Log4NetLogger(type);
        }
    }
}
