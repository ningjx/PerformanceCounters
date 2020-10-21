using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace PerformanceTools
{
    /// <summary>
    /// 性能计数器封装
    /// </summary>
    public class PerformanceCounters
    {
        private readonly Timer Timer = new Timer();
        private readonly List<CounterSets> CountersSets = new List<CounterSets>();
        private readonly Dictionary<string, CountersResult> CounterResults = new Dictionary<string, CountersResult>();
        private object Lock = new object();
        private int Interval;
        private float Ratio;

        /// <summary>
        /// 初始化计数器
        /// </summary>
        /// <param name="pCounterInfos">需要使用的计数器的列表</param>
        /// <param name="miSec">刷新间隔时间（ms）</param>
        public PerformanceCounters(List<CounterConfig> pCounterInfos, int miSec = 50)
        {
            foreach (var item in pCounterInfos)
            {
                CountersSets.Add(new CounterSets(item));
            }
            Interval = miSec;
            Ratio = 1000F / miSec;
            Timer.Interval = miSec;
            Timer.AutoReset = true;
            Timer.Elapsed += GetData;
            Timer.Enabled = true;
        }

        /// <summary>
        /// 开始计数
        /// </summary>
        public void Start()
        {
            Timer.Start();
        }

        /// <summary>
        /// 暂停计数
        /// </summary>
        public void Stop()
        {
            Timer.Stop();
        }

        /// <summary>
        /// 重新设置刷新间隔时间
        /// </summary>
        /// <param name="miSec"></param>
        public void SetTimerInterval(int miSec)
        {
            if (miSec > 0)
            {
                this.Interval = miSec;
                Timer.Stop();
                Timer.Interval = miSec;
                Timer.Start();
            }
        }

        /// <summary>
        /// 获取本机所有计数器类型
        /// </summary>
        public static List<string> GetAllCategorys()
        {
            List<PerformanceCounterCategory> pcc = PerformanceCounterCategory.GetCategories().ToList();
            List<string> result = pcc.Select(t => t.CategoryName).Distinct().ToList();
            return result;
        }

        /// <summary>
        /// 获取指定类型下的所有计数器
        /// </summary>
        /// <param name="CategoryName">计数器类型</param>
        /// <returns></returns>
        public static List<string> GetAllCountersWithCategory(string CategoryName)
        {
            List<string> result = new List<string>();
            PerformanceCounterCategory Category = new PerformanceCounterCategory(CategoryName);
            var instanceNames = Category.GetInstanceNames();
            List<PerformanceCounter> counters = new List<PerformanceCounter>();
            for (int i = 0; i < instanceNames.Length; i++)
            {
                List<PerformanceCounter> thisCounters = new List<PerformanceCounter>();
                try
                {
                    thisCounters = Category.GetCounters(instanceNames[i]).ToList();
                }
                catch { }
                counters.AddRange(thisCounters);
            }
            result = counters.Select(t => t.CounterName).Distinct().ToList();
            return result;
        }

        /// <summary>
        /// 获取指定类型下的所有可被计数的实例
        /// </summary>
        /// <param name="CategoryName">计数器类型</param>
        /// <returns></returns>
        public static List<string> GetAllInstanceWithCategory(string CategoryName)
        {
            PerformanceCounterCategory Category = new PerformanceCounterCategory(CategoryName);
            return Category.GetInstanceNames().Distinct().ToList();
        }

        /// <summary>
        /// 获取本机所有计数器以及它们关联的实例名和包含的计数器名称
        /// 警告!!!:很慢，慎用。(i7-8565U用时1分40秒)，建议使用异步写入到文件查看。
        /// 异步写入文件时，返回值为空
        /// </summary>
        /// <param name="fileName">提供文件名时可以写入到文件</param>
        /// <param name="isSync">当文件名不存在时，异步不生效</param>
        /// <returns></returns>
        public static Dictionary<string, CategoryInfo> GetAllCategorysInfo(string fileName = null, bool isSync = true)
        {
            Dictionary<string, CategoryInfo> GetAllCategorysInfoFunc()
            {
                Dictionary<string, CategoryInfo> result = new Dictionary<string, CategoryInfo>();
                List<string> categorys = GetAllCategorys();
                foreach (string category in categorys)
                {
                    try
                    {
                        CategoryInfo info = new CategoryInfo
                        {
                            InstanceNames = GetAllInstanceWithCategory(category),
                            CounterNames = GetAllCountersWithCategory(category)
                        };
                        result.ExtensionAdd(category, info);
                    }
                    catch { }
                }
                return result;
            }

            if (string.IsNullOrEmpty(fileName))
            {
                return GetAllCategorysInfoFunc();
            }
            else if (isSync)
            {
                Dictionary<string, CategoryInfo> datas = GetAllCategorysInfoFunc();
                using (FileStream fileStream = new FileStream(fileName, FileMode.OpenOrCreate))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(datas));
                    fileStream.Write(bytes, 0, bytes.Length);
                };
                return datas;
            }
            else
            {
                Task.Run(() =>
                {
                    Dictionary<string, CategoryInfo> datas = GetAllCategorysInfoFunc();
                    using (FileStream fileStream = new FileStream(fileName, FileMode.OpenOrCreate))
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(datas));
                        fileStream.Write(bytes, 0, bytes.Length);
                    };
                });
                return new Dictionary<string, CategoryInfo>();
            }
        }

        private void GetData(object sender, ElapsedEventArgs e)
        {
            lock (Lock)
            {
                foreach (var counterSet in CountersSets)
                {
                    foreach (var counter in counterSet.CounterDatas)
                    {
                        counter.Value = counter.Counter.NextSample().RawValue;
                        counter.Count = (long)((counter.Value - counter.OldValue) * Ratio);
                        counter.OldValue = counter.Value;
                        CounterResults.ExtensionAdd(counter.InstanceName + counter.CounterName, new CountersResult(counter));
                    }
                }
                ReciveData?.Invoke(CounterResults.Values.ToList());
            }
        }

        public delegate void RefreshHandler(List<CountersResult> datas);

        /// <summary>
        /// 每次刷新后的事件
        /// </summary>
        public event RefreshHandler ReciveData;
    }

    internal static class Extensions
    {
        public static void ExtensionAdd<K, V>(this Dictionary<K, V> dic, K key, V value)
        {
            if (dic.ContainsKey(key))
            {
                dic[key] = value;
            }
            else
            {
                dic.Add(key, value);
            }
        }
    }

    public class CounterConfig
    {
        /// <summary>
        /// 计数器类型
        /// </summary>
        public string CategoryName;

        /// <summary>
        /// 计数器名称
        /// </summary>
        public string CounterName;

        /// <summary>
        /// 实例名称（被计数的设备名）
        /// </summary>
        public string InstanceName;

        /// <summary>
        /// 数据类型
        /// </summary>
        public CustomType Type;

        /// <summary>
        /// 处理数据方法
        /// </summary>
        public DealDataHandler Func;

        public CounterConfig(string categoryName, string counterName, CustomType type, DealDataHandler func = null, string instanceName = null)
        {
            CategoryName = categoryName;
            CounterName = counterName;
            Type = type;
            Func = func;
            InstanceName = instanceName;
        }
    }

    public delegate void DealDataHandler(long count, out float currCount, out string currUnit);

    public class CounterSets
    {
        /// <summary>
        /// 该计数器类型类型下所有被计数设备的计数器列表
        /// </summary>
        public List<CounterData> CounterDatas = new List<CounterData>();

        /// <summary>
        /// 计数器类型
        /// </summary>
        public string CategoryName;

        /// <summary>
        /// 计数器名称
        /// </summary>
        public string CounterName;

        /// <summary>
        /// 实例名称（被计数的设备名）
        /// </summary>
        public string InstanceName;

        /// <summary>
        /// 数据类型
        /// </summary>
        public CustomType Type;

        /// <summary>
        /// 处理数据方法
        /// </summary>
        public DealDataHandler Func;

        public CounterSets(CounterConfig info)
        {
            Type = info.Type;
            Func = info.Func;
            CategoryName = info.CategoryName;
            CounterName = info.CounterName;
            InstanceName = info.InstanceName;
            if (info.InstanceName == null)
            {
                PerformanceCounterCategory category = new PerformanceCounterCategory(CategoryName);
                foreach (string name in category.GetInstanceNames())
                {
                    CounterDatas.Add(new CounterData(new PerformanceCounter(CategoryName, CounterName, name), Type, Func));
                }
            }
            else
                CounterDatas.Add(new CounterData(new PerformanceCounter(CategoryName, CounterName, InstanceName), Type, Func));
        }
    }

    public class CounterData
    {
        /// <summary>
        /// 当前计数
        /// </summary>
        public long Value;

        /// <summary>
        /// 上一次计数
        /// </summary>
        public long OldValue;

        /// <summary>
        /// 区间计数
        /// </summary>
        public long Count;

        /// <summary>
        /// 计数器类型
        /// </summary>
        public string CategoryName;

        /// <summary>
        /// 计数器名称
        /// </summary>
        public string CounterName;

        /// <summary>
        /// 实例名称（被计数的设备名）
        /// </summary>
        public string InstanceName;

        /// <summary>
        /// 计数器实例
        /// </summary>
        public PerformanceCounter Counter;

        /// <summary>
        /// 数据类型
        /// </summary>
        public CustomType Type;

        /// <summary>
        /// 处理数据方法
        /// </summary>
        public DealDataHandler Func;

        public CounterData(PerformanceCounter counter, CustomType type, DealDataHandler func)
        {
            Counter = counter;
            InstanceName = counter.InstanceName;
            CounterName = counter.CounterName;
            CategoryName = counter.CategoryName;
            Type = type;
            Func = func;
        }
    }

    public class CountersResult
    {
        /// <summary>
        /// 计数器类型
        /// </summary>
        public string CategoryName;

        /// <summary>
        /// 计数器名称
        /// </summary>
        public string CounterName;

        /// <summary>
        /// 实例名称（被计数的设备名）
        /// </summary>
        public string InstanceName;

        /// <summary>
        /// 当前计数
        /// </summary>
        public long CurrentCount;

        /// <summary>
        /// 上一次计数
        /// </summary>
        public long OldCount;

        /// <summary>
        /// 区间计数
        /// </summary>
        public long Count;

        /// <summary>
        /// 数据类型
        /// </summary>
        public CustomType Type;

        /// <summary>
        /// 数据单位
        /// </summary>
        public string Unit;

        /// <summary>
        /// 处理Count的方法
        /// </summary>
        public DealDataHandler Func;

        /// <summary>
        /// 处理后的结果
        /// </summary>
        public float Value;

        public CountersResult(CounterData data)
        {
            CurrentCount = data.Value;
            OldCount = data.OldValue;
            CategoryName = data.CategoryName;
            CounterName = data.CounterName;
            InstanceName = data.InstanceName;
            Count = data.Count;
            Value = Count;
            Type = data.Type;
            Func = data.Func;
            Func?.Invoke(Count, out Value, out Unit);
            if (string.IsNullOrEmpty(Unit))
                Unit = data.Counter.CounterName.Split(' ')[0];
        }
    }

    public enum CustomType
    {
        Voltage, // V
        Clock, // MHz
        Temperature, // °C
        Load, // %
        Fan, // RPM
        Flow, // L/h
        Control, // %
        Level, // %
        Factor, // 1
        Power, // W
        Data, // GB = 2^30 Bytes    
        SmallData, // MB = 2^20 Bytes
        Throughput, // MB/s = 2^20 Bytes/s
        Download,
        Upload,
        Unknown = 99
    }

    public class CategoryInfo
    {
        public List<string> InstanceNames = new List<string>();
        public List<string> CounterNames = new List<string>();
    }
}
