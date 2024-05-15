using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace MetricsToRedis
{
    internal class DotEnv
    {
        protected static Dictionary<string, string> env = new Dictionary<string, string>();

        public static string? getEnv(string key, string? def = null)
        {
            Load();
            var val = env.FirstOrDefault(pair=>pair.Key == key).Value;

            if( val == null )
            {
                return def;
            }

            return val;
        }
        public static Dictionary<string, string> Load()
        {
            if (env.Count > 0) return env;
            var filePath = EnvPath();

            if (!File.Exists(filePath))
                return env;

            foreach (var line in File.ReadAllLines(filePath))
            {
                var parts = line.Split('=');

                if (parts.Length != 2)
                    continue;

                env.Add(parts[0], parts[1]);
            }
            return env;

        }

        private static string EnvPath()
        {
            var root = Directory.GetCurrentDirectory();
            var dotenv = Path.Combine(root, ".env");

            return dotenv;
        }
    }
}
