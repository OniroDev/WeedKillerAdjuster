using BepInEx.Logging;

namespace OniroDev
{
    public static class WeedKillerAdjusterLogger
    {
        internal static ManualLogSource logSource;

        public static void Initialize(string pluginGUID)
        {
            logSource = Logger.CreateLogSource(pluginGUID);
        }

        public static void Log(object message)
        {
            logSource.LogInfo(message);
        }

        public static void LogError(object message)
        {
            logSource.LogError(message);
        }

        public static void LogWarning(object message)
        {
            logSource.LogWarning(message);
        }

    }
}
