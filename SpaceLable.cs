using System;
using System.Drawing;
using System.Windows.Forms;

public class SpacedLabel : Label
{
    public float LineSpacing { get; set; } = 5.0f; // 行间距

    protected override void OnPaint(PaintEventArgs e)
    {
        if (string.IsNullOrEmpty(Text))
            return;

        using (StringFormat format = new StringFormat())
        {
            format.LineAlignment = StringAlignment.Near;
            format.Alignment = StringAlignment.Near;

            // 考虑 Padding
            float y = Padding.Top;
            float totalHeight = Padding.Top; // 初始化总高度

            foreach (string line in Text.Split(new[] { '\n' }, StringSplitOptions.None))
            {
                float lineHeight = Font.GetHeight(e.Graphics); // 获取当前行的高度
                // 确保行的总高度不超出控件的高度
                if (y + lineHeight > Height - Padding.Bottom)
                    break; // 如果超出则停止绘制

                e.Graphics.DrawString(line, Font, new SolidBrush(ForeColor), new RectangleF(Padding.Left, y, Width - Padding.Horizontal, lineHeight), format);
                y += lineHeight + LineSpacing; // 更新下一行的 Y 坐标
                totalHeight += lineHeight + LineSpacing; // 更新总高度
            }

            // 更新当前控件的高度
            if (totalHeight > Height)
            {
                this.Height = (int)totalHeight + Padding.Vertical; // 更新控件高度
            }
        }
    }
}
