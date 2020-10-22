using PerformanceTools;
using System.Collections.Generic;

namespace Demo
{
    public static class PerformanceHandler
    {
        public static readonly PerformanceCounters pCounters;
        public static string StrData = string.Empty;
        static PerformanceHandler()
        {

            List<CounterConfig> pCounterInfos = new List<CounterConfig>
            {
                new CounterConfig("Network Interface","Bytes Received/sec",CustomType.Download,NetFunc),
                new CounterConfig("Network Interface","Bytes Sent/sec",CustomType.Upload,NetFunc),
            };
            pCounters = new PerformanceCounters(pCounterInfos, 1000);
        }

        private static void PCounters_ReciveData(List<CountersResult> datas)
        {
            var buf = string.Empty;
            for (int i = 0; i < datas.Count; i++)
            {
                buf += $"计数器实例 {datas[i].InstanceName}，计数器名 {datas[i].CounterName}，计数类型 {datas[i].Type}，值 {datas[i].Value}，单位 {datas[i].Unit}";
            }
            StrData = buf;
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

        public static void Start() { }
    }
}
