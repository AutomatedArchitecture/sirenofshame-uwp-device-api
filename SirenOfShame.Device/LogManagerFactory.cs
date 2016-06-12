namespace SirenOfShame.Device
{
    public class LogManagerFactory
    {
        public class DefaultLogManager
        {
            public static ILogger GetLogger<T>()
            {
                return new ConsoleLogger<T>();
            }
        }
    }

    public class ConsoleLogger<T> : ILogger
    {
        public void Debug(string s)
        {
            System.Diagnostics.Debug.WriteLine(typeof(T).Name + ": " + s);
        }
    }

    public interface ILogger
    {
        void Debug(string s);
    }
}
