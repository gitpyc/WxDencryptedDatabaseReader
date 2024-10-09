using System;
using System.Collections.Generic;
using System.Drawing;
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

        // ���Ʊ߿�
        using (Pen pen = new Pen(BorderColor))
        {
            e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
        }

        // ��ǰ���Ƶ�ˮƽƫ����
        int xOffset = 0;

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
            // ����ÿ��ռλ�����л���
            foreach (var placeholder in imageMap.Keys)
            {
                string[] parts = labelText.Split(new string[] { placeholder }, StringSplitOptions.None);
                for (int i = 0; i < parts.Length; i++)
                {
                    // ������ͨ�ı�
                    e.Graphics.DrawString(parts[i], this.Font, new SolidBrush(this.ForeColor), xOffset, 0);
                    xOffset += TextRenderer.MeasureText(parts[i], this.Font).Width; // ����ƫ����

                    // ����������һ�Σ�����ռλ����Ӧ��ͼƬ
                    if (i < parts.Length - 1)
                    {
                        Image img = imageMap[placeholder];

                        // ����ͼƬ��Ŀ��߶ȣ����������������
                        float ratio = (float)this.Font.Height / img.Height;
                        int imgWidth = (int)(img.Width * ratio);
                        Image resizedImage = new Bitmap(img, new Size(imgWidth, this.Font.Height));

                        // ����ͼƬ
                        e.Graphics.DrawImage(resizedImage, new Rectangle(xOffset, 0, resizedImage.Width, resizedImage.Height));
                        xOffset += resizedImage.Width; // ����ƫ����������һ���ı�
                    }
                }

                // ���������ı������¿�ʼ������һ��ռλ��
                labelText = string.Join("", parts);
            }
        }
        else
        {
            // ���û��ռλ�������������ı�
            e.Graphics.DrawString(labelText, this.Font, new SolidBrush(this.ForeColor), xOffset, 0);
        }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        this.Invalidate(); // �ڳߴ�仯ʱ�ػ�
    }
}

