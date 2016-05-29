using MetroLog;
using MetroLog.Targets;

namespace SirenOfShame.Device
{
    internal class MyLogManager
    {
        static MyLogManager()
        {
            LogManagerFactory.DefaultConfiguration.AddTarget(LogLevel.Trace, LogLevel.Fatal, new StreamingFileTarget());
        }
    }
}
