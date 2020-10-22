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
    }
}
