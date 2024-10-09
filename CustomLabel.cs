using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

public class CustomLabel : Label
{
    public Dictionary<string, Image> imageMap = new Dictionary<string, Image>(); // ���ռλ����ͼƬ��ӳ���ϵ
    private string labelText; // ���ڴ���ı�
    public Color BorderColor { get; set; }

    public string LabelText
    {
        get { return labelText; }
        set
        {
            labelText = value;
            Invalidate(); // �����ı����ػ�ؼ�
        }
    }

    public void AddImage(string placeholder, Image image)
    {
        imageMap[placeholder] = image; // ���ռλ����ͼƬ��ӳ��
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // ��ǰ���Ƶ�ˮƽƫ�����ʹ�ֱƫ����
        int xOffset = 0;
        int yOffset = 0;

        // ��ȡ�����
        int maxWidth = this.MaximumSize.Width > 0 ? this.MaximumSize.Width : int.MaxValue;

        // ����ı��е�ռλ��������
        bool hasPlaceholder = false;

        foreach (var placeholder in imageMap.Keys)
        {
            if (labelText.Contains(placeholder))
            {
                hasPlaceholder = true;
                break;
            }
        }

        // �����ռλ��������д�������ֱ�ӻ����ı�
        if (hasPlaceholder)
        {
            var regex = new Regex(@"\[.+?\]"); // ƥ�� "[ռλ��]"
            var matches = regex.Matches(labelText);

            int lastIndex = 0;

            foreach (Match match in matches)
            {
                string placeholder = match.Value; // ��ȡռλ��������������
                string key = placeholder; // ʹ�ñ��������ŵ�ռλ����Ϊkey

                // ��ȡռλ��֮ǰ���ı�����鲻Խ�磩
                string precedingText = labelText.Substring(lastIndex, match.Index - lastIndex);

                // ���ǰ�����ı���������ı�
                if (!string.IsNullOrEmpty(precedingText))
                {
                    // �����ı��Ŀ��
                    Size textSize = TextRenderer.MeasureText(precedingText, this.Font);
                    if (xOffset + textSize.Width > maxWidth)
                    {
                        // ����
                        yOffset += this.Font.Height;
                        xOffset = 0;
                    }

                    e.Graphics.DrawString(precedingText, this.Font, new SolidBrush(this.ForeColor), xOffset, yOffset);
                    xOffset += textSize.Width; // ����ƫ����
                }

                // ����ǰռλ����Ӧ��ͼƬ
                if (imageMap.ContainsKey(key))
                {
                    Image img = imageMap[key];

                    // ����ͼƬ��Ŀ��߶ȣ����������������
                    float ratio = (float)this.Font.Height / img.Height;
                    int imgWidth = (int)(img.Width * ratio);
                    Image resizedImage = new Bitmap(img, new Size(imgWidth, this.Font.Height));

                    if (xOffset + resizedImage.Width > maxWidth)
                    {
                        // ����
                        yOffset += this.Font.Height;
                        xOffset = 0;
                    }

                    // ����ͼƬ
                    e.Graphics.DrawImage(resizedImage, new Rectangle(xOffset, yOffset, resizedImage.Width, resizedImage.Height));
                    xOffset += resizedImage.Width; // ����ƫ����
                }

                lastIndex = match.Index + match.Length;
            }

            // �������һ���ı�������У�
            if (lastIndex < labelText.Length)
            {
                string remainingText = labelText.Substring(lastIndex);
                if (!string.IsNullOrEmpty(remainingText)) // �����ʣ���ı�������֮
                {
                    Size remainingTextSize = TextRenderer.MeasureText(remainingText, this.Font);
                    if (xOffset + remainingTextSize.Width > maxWidth)
                    {
                        // ����
                        yOffset += this.Font.Height;
                        xOffset = 0;
                    }

                    e.Graphics.DrawString(remainingText, this.Font, new SolidBrush(this.ForeColor), xOffset, yOffset);
                }
            }
        }
        else
        {
            // ���û��ռλ�������������ı�
            Size labelSize = TextRenderer.MeasureText(labelText, this.Font);
            if (labelSize.Width > maxWidth)
            {
                // ����
                yOffset += this.Font.Height;
                xOffset = 0;
            }

            e.Graphics.DrawString(labelText, this.Font, new SolidBrush(this.ForeColor), xOffset, yOffset);
        }

        // �����ܿ�Ⱥ��ܸ߶�
        int totalWidth = xOffset + this.Padding.Left + this.Padding.Right;
        int totalHeight = yOffset + this.Font.Height + this.Padding.Top + this.Padding.Bottom;

        if (totalWidth != this.Width || totalHeight != this.Height)
        {
            this.Size = new Size(totalWidth, totalHeight); // �����ؼ���С
            this.Invalidate(); // �ػ�ؼ�
        }
        // ���Ʊ߿�
        using (Pen pen = new Pen(BorderColor))
        {
            e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
        }
        //֪ͨ���ؼ����²���
        this.Parent.Invalidate();
    }


    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        //this.Invalidate(); // �ڳߴ�仯ʱ�ػ�
    }
}

