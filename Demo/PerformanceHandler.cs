using PerformanceTools;
using System.Collections.Generic;

namespace Demo
{
    public static class PerformanceHandler
    {
        public static readonly PerformanceCounters PCounters;

        static PerformanceHandler()
        {
            //初始化计数器
            List<CounterConfig> pCounterInfos = new List<CounterConfig>
            {
                new CounterConfig("Network Interface","Bytes Received/sec",CustomType.Download,NetFunc),
                new CounterConfig("Network Interface","Bytes Sent/sec",CustomType.Upload,NetFunc),
            };
            PCounters = new PerformanceCounters(pCounterInfos, 1000);
        }

        /// <summary>
        /// 处理数值转换和单位
        /// </summary>
        /// <param name="count"></param>
        /// <param name="currCount"></param>
        /// <param name="unit"></param>
        private static void NetFunc(long count, out float currCount, out string unit)
        {
            if ((currCount = count / 1024F) < 1024)
            {
                unit = "KB/s";
                return;
            }
            else
            {
                currCount /= 1024F;
                unit = "MB/s";
                return;
            }
        }
    }
}
