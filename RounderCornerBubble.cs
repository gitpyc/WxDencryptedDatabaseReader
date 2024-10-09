using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wxreader
{
    public class RounderCornerBubble : Panel
    {
        public int CornerRadius { get; set; } = 20; // 圆角半径
        public Color BorderColor { get; set; } = Color.Black; // 边框颜色

        protected override void OnPaint(PaintEventArgs e)
        {
            // 创建圆角矩形路径
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            path.StartFigure();
            path.AddArc(0, 0, CornerRadius, CornerRadius, 180, 90); // 左上角
            path.AddArc(Width - CornerRadius, 0, CornerRadius, CornerRadius, 270, 90); // 右上角
            path.AddArc(Width - CornerRadius, Height - CornerRadius, CornerRadius, CornerRadius, 0, 90); // 右下角
            path.AddArc(0, Height - CornerRadius, CornerRadius, CornerRadius, 90, 90); // 左下角
            path.CloseFigure();

            // 设置面板的区域
            this.Region = new Region(path);
            base.OnPaint(e);

            // 填充背景颜色
            using (Brush brush = new SolidBrush(this.BackColor))
            {
                e.Graphics.FillPath(brush, path);
            }

            // 设置边框颜色
            using (Pen pen = new Pen(this.BorderColor)) // 使用 BorderColor 属性
            {
                e.Graphics.DrawPath(pen, path);
            }
        }
    }
}
