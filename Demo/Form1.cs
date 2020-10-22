using PerformanceTools;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Demo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            PerformanceHandler.PCounters.ReciveData += PCounters_ReciveData;
            PerformanceHandler.PCounters.Start();
        }

        private void PCounters_ReciveData(List<CountersResult> datas)
        {
            richTextBox1.Clear();
            var buf = string.Empty;
            for (int i = 0; i < datas.Count; i++)
            {
                buf += $"计数器实例 {datas[i].InstanceName}，计数器名 {datas[i].CounterName}，计数类型 {datas[i].Type}，值 {datas[i].Value}，单位 {datas[i].Unit}\n";
            }
            richTextBox1.Text = buf;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var datas = PerformanceCounters.GetAllCategorys();
            datas?.ForEach(t => listBox1.Items.Add(t));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null)
                MessageBox.Show("先把第一个选上");
            else
            {
                var datas = PerformanceCounters.GetAllCountersWithCategory(listBox1.SelectedItem.ToString());
                datas?.ForEach(t => listBox2.Items.Add(t));
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null)
                MessageBox.Show("先把第一个选上");
            else
            {
                var datas = PerformanceCounters.GetAllInstanceWithCategory(listBox1.SelectedItem.ToString());
                datas?.ForEach(t => listBox3.Items.Add(t));
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null || listBox2.SelectedItem == null)
                MessageBox.Show("至少先把第一个和第二个选上");
            else
            {
                List<CounterConfig> pCounterInfos = new List<CounterConfig>
                {
                    new CounterConfig(listBox1.SelectedItem.ToString(),listBox2.SelectedItem.ToString(),CustomType.Unknown,null,listBox3.SelectedItem?.ToString())
                };
                PerformanceHandler.PCounters.Stop();
                PerformanceHandler.PCounters = new PerformanceCounters(pCounterInfos, 1000);
                PerformanceHandler.PCounters.ReciveData += PCounters_ReciveData;
                PerformanceHandler.PCounters.Start();
            }
        }
    }
}
