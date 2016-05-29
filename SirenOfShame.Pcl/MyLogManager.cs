using MetroLog;
using MetroLog.Targets;

namespace SirenOfShame.Pcl
{
    public class MyLogManager
    {
        static MyLogManager()
        {
            LogManagerFactory.DefaultConfiguration.AddTarget(LogLevel.Trace, LogLevel.Fatal, new StreamingFileTarget());
        }
    }
}
