namespace wxreader
{
    public partial class Form4 : Form
    {
        int count = 0;
        public Form4()
        {
            InitializeComponent();
            this.CenterToScreen();
            label1.Text = "加载中";
            // 初始化Timer
            loadingTimer.Interval = 500; // 设置间隔时间为500毫秒
            loadingTimer.Tick += new EventHandler(LoadingTimer_Tick);
            loadingTimer.Start();
        }
        private void LoadingTimer_Tick(object sender, EventArgs e)
        {
            // 更新Label显示的文本
            if (count !=5)
            {
                label1.Text += ".";
            }
            else
            {
                label1.Text = "加载中";
                count = 0;
            }
            label1.Refresh(); // 刷新UI
            count++;
        }

        private async void Form4_Load(object sender, EventArgs e)
        {
            //label1.Text = "正在检测数据.";

            //await ShowLoadingAnimation();
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
            }

        }
    }
}
