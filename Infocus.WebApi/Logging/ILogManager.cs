using System;

namespace Infocus.Common.Logging
{
    public interface ILogManager
    {
        ILogger GetLogger(Type type);
        void Initialize();
        void Initialize(LogSeverity defaultSeverity);
        void Initialize(LogSeverity defaultSeverity, String fileName);
    }
}
