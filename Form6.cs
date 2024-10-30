using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace wxreader
{
    public partial class Form6 : Form
    {
        private int count = 0;
        private string loadingText = "加载中"; // 默认的加载文本

        public Form6(string customText = null)
        {
            InitializeComponent();
            this.CenterToScreen();
            label1.Text = customText ?? loadingText; // 如果提供了自定义文本，则使用自定义文本

            GloableVars.NoCondition.ValueChanged += MonitoredVariable_ValueChanged;

            // 初始化Timer
            timer1.Interval = 500; // 设置间隔时间为500毫秒
            timer1.Tick += new EventHandler(LoadingTimer_Tick);
            timer1.Start();
        }

        private void MonitoredVariable_ValueChanged(object sender, EventArgs e)
        {
            this.Close(); // 关闭当前窗口
        }

        private void LoadingTimer_Tick(object sender, EventArgs e)
        {
            // 更新Label显示的文本
            if (count < 5)
            {
                label1.Text += ".";
            }
            else
            {
                label1.Text = loadingText; // 重置为加载文本
                count = 0;
            }
            label1.Refresh(); // 刷新UI
            count++;
        }

        private async void LoadingForm_Load(object sender, EventArgs e)
        {
            await ShowLoadingAnimation();
        }

        private async Task ShowLoadingAnimation()
        {
            while (true)
            {
                for (int i = 0; i < 5; i++)
                {
                    label1.Text += ".";
                    label1.Refresh(); // 刷新UI
                    await Task.Delay(1000); // 异步等待，允许UI响应
                }
                label1.Text = loadingText; // 重置为加载文本
            }
        }

        // 可以提供一个方法来更新文本内容
        public void UpdateLoadingText(string newText)
        {
            loadingText = newText;
            label1.Text = newText; // 更新当前显示文本
        }
    }
}
