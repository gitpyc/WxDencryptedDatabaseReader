using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

public class VideoPlayRounderPanel : Panel
{
    // 圆角半径
    public int CornerRadius { get; set; } = 20;

    // 边框颜色
    public Color BorderColor { get; set; } = Color.Black;

    // 填充颜色
    public Color FillColor { get; set; } = Color.White;

    // 行间距
    public float LineSpacing { get; set; } = 5f;

    // 要显示的文本
    public string DisplayText { get; set; } = string.Empty;

    protected override void OnPaint(PaintEventArgs e)
    {
        // 如果 CornerRadius 为 0 则绘制普通矩形
        if (CornerRadius <= 0)
        {
            using (Brush brush = new SolidBrush(FillColor))
            {
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            }

            using (Pen pen = new Pen(BorderColor))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            }
        }
        else
        {
            // 创建圆角矩形路径
            using (GraphicsPath path = new GraphicsPath())
            {
                path.StartFigure();
                path.AddArc(0, 0, CornerRadius, CornerRadius, 180, 90); // 左上角
                path.AddArc(Width - CornerRadius, 0, CornerRadius, CornerRadius, 270, 90); // 右上角
                path.AddArc(Width - CornerRadius, Height - CornerRadius, CornerRadius, CornerRadius, 0, 90); // 右下角
                path.AddArc(0, Height - CornerRadius, CornerRadius, CornerRadius, 90, 90); // 左下角
                path.CloseFigure();

                // 设置面板的区域
                this.Region = new Region(path);

                // 填充背景颜色
                using (Brush brush = new SolidBrush(FillColor))
                {
                    e.Graphics.FillPath(brush, path);
                }

                // 设置边框颜色
                using (Pen pen = new Pen(BorderColor))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }

        // 绘制文本
        DrawText(e.Graphics);
    }

    private void DrawText(Graphics graphics)
    {
        // 设置文本格式
        StringFormat format = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Near
        };

        // 分行绘制文本
        string[] lines = DisplayText.Split(new[] { '\n' }, StringSplitOptions.None);
        float y = this.Padding.Top; // 从顶部开始绘制文本

        foreach (var line in lines)
        {
            graphics.DrawString(line, this.Font, new SolidBrush(this.ForeColor), new PointF(this.Padding.Left, y), format);
            y += this.Font.GetHeight(graphics) + LineSpacing; // 增加行间距
        }
    }
}
