using System.Drawing.Drawing2D;

namespace wxreader
{
    public class VoicePlayRounderPanel : Panel
    {
        public int CornerRadius { get; set; } = 20; // 圆角半径
        public Color BorderColor { get; set; } = Color.Black; // 边框颜色
        private System.Windows.Forms.Timer animationTimer;
        private float animationProgress = 0f; // 动画进度
        public Color fillColor = Color.LightGreen; // 填充颜色
        private bool isSender; // 填充方向标识

        public VoicePlayRounderPanel()
        {
            this.DoubleBuffered = true; // 启用双缓冲

            // 初始化定时器
            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 50; // 每100毫秒更新一次
            animationTimer.Tick += OnAnimationTick;
        }

        /// <summary>
        /// 更新动画进度
        /// </summary>
        /// <param name="progress"></param>
        public void UpdateAnimationProgress(float progress, bool isSender)
        {
            this.isSender = isSender; // 设置填充方向
            animationProgress = progress; // 更新动画进度
            Invalidate(); // 触发重绘
        }

        /// <summary>
        /// 启动动画
        /// </summary>
        public void StartAnimation()
        {
            animationProgress = 0f; // 重置进度
            animationTimer.Start(); // 启动定时器
        }

        private void OnAnimationTick(object sender, EventArgs e)
        {
            animationProgress += 0.05f; // 动画进度增加

            if (animationProgress >= 1f)
            {
                animationProgress = 1f; // 限制在1之内
                animationTimer.Stop(); // 停止动画
            }

            Invalidate(); // 触发重新绘制
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // 调用基类的 OnPaint
            base.OnPaint(e);

            // 创建圆角矩形路径
            GraphicsPath path = new GraphicsPath();
            path.StartFigure();
            path.AddArc(0, 0, CornerRadius, CornerRadius, 180, 90); // 左上角
            path.AddArc(Width - CornerRadius, 0, CornerRadius, CornerRadius, 270, 90); // 右上角
            path.AddArc(Width - CornerRadius, Height - CornerRadius, CornerRadius, CornerRadius, 0, 90); // 右下角
            path.AddArc(0, Height - CornerRadius, CornerRadius, CornerRadius, 90, 90); // 左下角
            path.CloseFigure();

            // 设置区域以适应圆角
            this.Region = new Region(path);

            // 计算填充宽度
            float fillWidth = this.Width * animationProgress;

            // 根据 isSender 决定填充位置
            using (SolidBrush brush = new SolidBrush(fillColor))
            {
                if (isSender) // 自己发送，从左到右填充
                {
                    //e.Graphics.FillRectangle(brush, 0, 0, fillWidth, this.Height);
                    e.Graphics.FillRectangle(brush, this.Width - fillWidth, 0, fillWidth, this.Height);
                }
                else // 对方发送，从右到左填充
                {
                    //e.Graphics.FillRectangle(brush, this.Width - fillWidth, 0, fillWidth, this.Height);
                    e.Graphics.FillRectangle(brush, 0, 0, fillWidth, this.Height);
                }
            }

            // 绘制边框
            using (Pen pen = new Pen(BorderColor))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }
    }

}
