using System;
using System.Collections.Generic;
using System.Drawing;
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

        // 绘制边框
        using (Pen pen = new Pen(BorderColor))
        {
            e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
        }

        // 当前绘制的水平偏移量
        int xOffset = 0;

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
            // 遍历每个占位符进行绘制
            foreach (var placeholder in imageMap.Keys)
            {
                string[] parts = labelText.Split(new string[] { placeholder }, StringSplitOptions.None);
                for (int i = 0; i < parts.Length; i++)
                {
                    // 绘制普通文本
                    e.Graphics.DrawString(parts[i], this.Font, new SolidBrush(this.ForeColor), xOffset, 0);
                    xOffset += TextRenderer.MeasureText(parts[i], this.Font).Width; // 更新偏移量

                    // 如果不是最后一段，绘制占位符对应的图片
                    if (i < parts.Length - 1)
                    {
                        Image img = imageMap[placeholder];

                        // 计算图片的目标高度，并按比例调整宽度
                        float ratio = (float)this.Font.Height / img.Height;
                        int imgWidth = (int)(img.Width * ratio);
                        Image resizedImage = new Bitmap(img, new Size(imgWidth, this.Font.Height));

                        // 绘制图片
                        e.Graphics.DrawImage(resizedImage, new Rectangle(xOffset, 0, resizedImage.Width, resizedImage.Height));
                        xOffset += resizedImage.Width; // 更新偏移量用于下一个文本
                    }
                }

                // 更新完整文本，重新开始处理下一个占位符
                labelText = string.Join("", parts);
            }
        }
        else
        {
            // 如果没有占位符，正常绘制文本
            e.Graphics.DrawString(labelText, this.Font, new SolidBrush(this.ForeColor), xOffset, 0);
        }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        this.Invalidate(); // 在尺寸变化时重绘
    }
}

