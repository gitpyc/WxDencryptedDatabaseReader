using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

public class CustomLabel : Label
{
    public Dictionary<string, Image> imageMap = new Dictionary<string, Image>(); // 存放占位符与图片的映射关系
    private string labelText; // 用于存放文本
    public Color BorderColor { get; set; }

    public string LabelText
    {
        get { return labelText; }
        set
        {
            labelText = value;
            Invalidate(); // 更新文本后重绘控件
        }
    }

    public void AddImage(string placeholder, Image image)
    {
        imageMap[placeholder] = image; // 添加占位符与图片的映射
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // 当前绘制的水平偏移量和垂直偏移量
        int xOffset = 0;
        int yOffset = 0;

        // 获取最大宽度
        int maxWidth = this.MaximumSize.Width > 0 ? this.MaximumSize.Width : int.MaxValue;

        // 检查文本中的占位符并绘制
        bool hasPlaceholder = false;

        foreach (var placeholder in imageMap.Keys)
        {
            if (labelText.Contains(placeholder))
            {
                hasPlaceholder = true;
                break;
            }
        }

        // 如果有占位符，则进行处理，否则直接绘制文本
        if (hasPlaceholder)
        {
            var regex = new Regex(@"\[.+?\]"); // 匹配 "[占位符]"
            var matches = regex.Matches(labelText);

            int lastIndex = 0;

            foreach (Match match in matches)
            {
                string placeholder = match.Value; // 提取占位符，保留方括号
                string key = placeholder; // 使用保留方括号的占位符作为key

                // 获取占位符之前的文本（检查不越界）
                string precedingText = labelText.Substring(lastIndex, match.Index - lastIndex);

                // 如果前面有文本，则绘制文本
                if (!string.IsNullOrEmpty(precedingText))
                {
                    // 计算文本的宽度
                    Size textSize = TextRenderer.MeasureText(precedingText, this.Font);
                    if (xOffset + textSize.Width > maxWidth)
                    {
                        // 换行
                        yOffset += this.Font.Height;
                        xOffset = 0;
                    }

                    e.Graphics.DrawString(precedingText, this.Font, new SolidBrush(this.ForeColor), xOffset, yOffset);
                    xOffset += textSize.Width; // 更新偏移量
                }

                // 处理当前占位符对应的图片
                if (imageMap.ContainsKey(key))
                {
                    Image img = imageMap[key];

                    // 计算图片的目标高度，并按比例调整宽度
                    float ratio = (float)this.Font.Height / img.Height;
                    int imgWidth = (int)(img.Width * ratio);
                    Image resizedImage = new Bitmap(img, new Size(imgWidth, this.Font.Height));

                    if (xOffset + resizedImage.Width > maxWidth)
                    {
                        // 换行
                        yOffset += this.Font.Height;
                        xOffset = 0;
                    }

                    // 绘制图片
                    e.Graphics.DrawImage(resizedImage, new Rectangle(xOffset, yOffset, resizedImage.Width, resizedImage.Height));
                    xOffset += resizedImage.Width; // 更新偏移量
                }

                lastIndex = match.Index + match.Length;
            }

            // 绘制最后一段文本（如果有）
            if (lastIndex < labelText.Length)
            {
                string remainingText = labelText.Substring(lastIndex);
                if (!string.IsNullOrEmpty(remainingText)) // 如果有剩余文本，绘制之
                {
                    Size remainingTextSize = TextRenderer.MeasureText(remainingText, this.Font);
                    if (xOffset + remainingTextSize.Width > maxWidth)
                    {
                        // 换行
                        yOffset += this.Font.Height;
                        xOffset = 0;
                    }

                    e.Graphics.DrawString(remainingText, this.Font, new SolidBrush(this.ForeColor), xOffset, yOffset);
                }
            }
        }
        else
        {
            // 如果没有占位符，正常绘制文本
            Size labelSize = TextRenderer.MeasureText(labelText, this.Font);
            if (labelSize.Width > maxWidth)
            {
                // 换行
                yOffset += this.Font.Height;
                xOffset = 0;
            }

            e.Graphics.DrawString(labelText, this.Font, new SolidBrush(this.ForeColor), xOffset, yOffset);
        }

        // 计算总宽度和总高度
        int totalWidth = xOffset + this.Padding.Left + this.Padding.Right;
        int totalHeight = yOffset + this.Font.Height + this.Padding.Top + this.Padding.Bottom;

        if (totalWidth != this.Width || totalHeight != this.Height)
        {
            this.Size = new Size(totalWidth, totalHeight); // 调整控件大小
            this.Invalidate(); // 重绘控件
        }
        // 绘制边框
        using (Pen pen = new Pen(BorderColor))
        {
            e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
        }
        //通知父控件重新布局
        this.Parent.Invalidate();
    }


    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        //this.Invalidate(); // 在尺寸变化时重绘
    }
}

