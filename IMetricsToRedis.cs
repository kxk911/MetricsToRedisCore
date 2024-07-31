using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetricsToRedis
{
    internal interface IMetricsToRedis
    {
        public MetricsToRedis name(string name);
        public MetricsToRedis tags(List<KeyValuePair<string, string>> tags);
        public MetricsToRedis tag(string tag, object value);
        public MetricsToRedis instance(string instance);
        public void Set(object data, System.TimeSpan? elapsedTime = null);
        public void Increment(int step = 1);
        public void Decriment(int step = 1);

    }
}
