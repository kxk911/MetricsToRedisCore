using StackExchange.Redis;

namespace MetricsToRedis
{
    public class MetricsToRedis
    {
        private static IDatabase? _db;
        private static IServer? _server;
        private string? _name;
        private List<KeyValuePair<string, string>> _tags = new List<KeyValuePair<string, string>>();
        private string? metric_instance;

        /// <summary>
        /// Constructor with custom connection to Redis
        /// </summary>
        /// <param name="connection">Connection string to Redis</param>
        public MetricsToRedis(string connection)
        {
            if (_db == null)
            {
                var configuration = ConfigurationOptions.Parse(connection);

                var redis = ConnectionMultiplexer.Connect(configuration);
                _db = redis.GetDatabase();
                _server = redis.GetServer(connection);
            }
        }

        /// <summary>
        /// Default constructor
        /// geting connection string from .env file or default (localhost connection)
        /// </summary>
        public MetricsToRedis()
        {
            if (_db == null)
            {
                var AddressandPort = DotEnv.getEnv("REDIS_ADDRES_PORT", "localhost:6379");

                if (AddressandPort == null) throw new Exception("Incorect connection string");

                var configuration = ConfigurationOptions.Parse(AddressandPort);

                var redis = ConnectionMultiplexer.Connect(configuration);
                _db = redis.GetDatabase();
                _server = redis.GetServer(AddressandPort);
            }
        }

        /// <summary>
        /// Setting name of key
        /// </summary>
        /// <param name="name">Key</param>
        /// <returns></returns>
        public MetricsToRedis name(string name)
        {

            var inst = new MetricsToRedis();

            inst._name = name;

            return inst;
        }

        /// <summary>
        /// Set tags list
        /// </summary>
        /// <param name="tags">List of tags</param>
        /// <returns></returns>
        public MetricsToRedis tags(List<KeyValuePair<string, string>> tags)
        {
            _tags = tags;
            return this;
        }

        /// <summary>
        /// Set tag
        /// </summary>
        /// <param name="tag">Tag name</param>
        /// <param name="value">Tag value</param>
        /// <returns></returns>
        public MetricsToRedis tag(string tag, object value)
        {
            if (value == null) return this;

            _tags.Add(new KeyValuePair<string, string>(tag, value.ToString()));

            return this;
        }

        /// <summary>
        /// Setting instance name, in metric out shows like `__instance__`
        /// </summary>
        /// <param name="instance">Instance name</param>
        /// <returns></returns>
        public MetricsToRedis instance(string instance)
        {
            metric_instance = instance;
            return this;
        }

        /// <summary>
        /// Set metrin value
        ///     If set elapsedTime, adding `__init_ts__` with timestamp (in milliseconds) of action
        /// </summary>
        /// <param name="data">Data for metric</param>
        /// <param name="elapsedTime">Elapsed time for metric</param>
        /// <exception cref="System.Exception"></exception>
        public void Set(object data, System.TimeSpan? elapsedTime = null)
        {
            if (_db == null) throw new Exception("Cannot connect to Redis");

            if (metric_instance == null) throw new System.Exception("Instance not set");

            if (elapsedTime != null)
                _tags.Add(new KeyValuePair<string, string>("__init_ts__", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()));

            var metricKey = GenerateKey();

            var dataString = data.ToString();

            if (elapsedTime == null)
            {
                _db.StringSet(metricKey, dataString);
            }
            else
            {
                _db.StringSet(metricKey, dataString, elapsedTime);
            }

        }

        public void Increment(int step = 1)
        {
            if (_db == null) throw new Exception("Cannot connect to Redis");

            var metricKey = GenerateKey();
            _db.StringIncrement(metricKey, step);
        }

        public void Decriment(int step = 1)
        {
            if (_db == null) throw new Exception("Cannot connect to Redis");

            var metricKey = GenerateKey();
            _db.StringDecrement(metricKey, step);
        }

        private string? GenerateKey()
        {
            var tags = GenerateTags();
            var metricKey = _name;
            if (tags != null)
            {
                metricKey += "{" + tags + "}";
            }

            return metricKey;
        }

        private string? GenerateTags()
        {
            string result = string.Empty;


            _tags.Add(new KeyValuePair<string, string>("__instance__", metric_instance));

            if (_tags.Count == 0)
            {
                return null;
            }
            foreach (var tag in _tags)
            {
                if (result.Length > 0) result += ", ";

                result += tag.Key + "=" + "'" + tag.Value + "'";
            }

            return result;
        }

        public static string Metric()
        {
            if (_db == null) throw new Exception("Cannot connect to Redis");
            if (_server == null) throw new Exception("Cannot connect to Redis");

            string result = "";

            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();

            foreach (var key in _server.Keys(pattern: "*"))
            {
                list.Add(new KeyValuePair<string, string>(key, _db.StringGet(key).ToString()));
            }

            list.Sort((pair1, pair2) => pair1.Key.CompareTo(pair2.Key));

            foreach (var item in list)
            {
                result += item.Key + " " + item.Value + '\n';
            }

            result = result.Replace('\'', '"');

            return result;
        }
    }
}

