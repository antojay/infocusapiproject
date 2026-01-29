using System;

namespace Infocus.Common.Logging
{
    public interface ILogger
    {
        void LogCritical(Object message);
        void LogCritical(Object message, Exception e);
        void LogError(Object message);
        void LogError(Object message, Exception e);
        void LogWarning(Object message);
        void LogWarning(Object message, Exception e);
        void LogInfo(Object message);
        void LogInfo(Object message, Exception e);
        void LogDebug(Object message);
        void LogTraceEntering(String methodName);
        void LogTraceLeaving(String methodName);
    }
}
