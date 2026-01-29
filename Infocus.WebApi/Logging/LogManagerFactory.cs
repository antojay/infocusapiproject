using System;
using System.Configuration;
using System.IO;
using Infocus.Common;
namespace Infocus.Common.Logging
{
    public static class LogManagerFactory
    {
        private static ILogManager logManager = new Log4NetLogManager();

        static LogManagerFactory()
        {
            //ClientConfiguration config = ClientConfiguration.Instance;
            LogSeverity logSeverity = LogSeverity.Information;
            try
            {
                String debugString = ConfigurationManager.AppSettings["LogLevel"];
                if(!String.IsNullOrWhiteSpace(debugString))
                {
                    if(debugString.Equals("debug", StringComparison.CurrentCultureIgnoreCase))
                    {
                        logSeverity = LogSeverity.Debug;
                    }
                    else if(debugString.StartsWith("warn", StringComparison.CurrentCultureIgnoreCase))
                    {
                        logSeverity = LogSeverity.Warning;
                    }
                    else if(debugString.Equals("error", StringComparison.CurrentCultureIgnoreCase) ||
                        debugString.Equals("critical", StringComparison.CurrentCultureIgnoreCase))
                    {
                        logSeverity = LogSeverity.Error;
                    }
                }
            }
            catch(Exception)
            {
            }
            logManager.Initialize(logSeverity, Path.Combine(SystemUtility.GetWorkingDirectory(), "log/log.txt"));
        }


        public static ILogManager GetLogFactory()
        {
            return logManager;
        }
    }
}
