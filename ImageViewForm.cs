using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace wxreader
{
    public class ImageViewForm : Form
    {
        private PictureBox pictureBox;
        private float zoomFactor = 1.0f; // 用于存储缩放因子
        private Point dragOffset;
        private bool dragging;

        public ImageViewForm(Image image)
        {
            this.ClientSize = new Size(image.Width, image.Height);
            pictureBox = new PictureBox
            {
                Image = image,
                SizeMode = PictureBoxSizeMode.Zoom, // 确保使用Zoom模式
                Dock = DockStyle.Fill
            };
            this.Controls.Add(pictureBox);
            this.Text = "图片查看器";

            // 添加鼠标滚轮事件
            pictureBox.MouseWheel += new MouseEventHandler(OnMouseWheel);
            pictureBox.MouseDown += PictureBox_MouseDown;
            pictureBox.MouseMove += PictureBox_MouseMove;
            pictureBox.MouseUp += PictureBox_MouseUp;
        }

        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            zoomFactor += e.Delta / 1000.0f;
            if (zoomFactor < 0.1f) zoomFactor = 0.1f;
            pictureBox.Width = (int)(pictureBox.Image.Width * zoomFactor);
            pictureBox.Height = (int)(pictureBox.Image.Height * zoomFactor);

            // 更新ClientSize
            UpdatePictureBoxSize();
        }
        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            dragging = true;
            dragOffset = new Point(e.X, e.Y);
        }

        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                Point point = pictureBox.PointToScreen(new Point(e.X, e.Y));
                pictureBox.Location = new Point(point.X - dragOffset.X, point.Y - dragOffset.Y);
            }
        }

        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            dragging = false;
        }

        private void UpdatePictureBoxSize()
        {
            // 根据缩放因子更新PictureBox的大小
            pictureBox.Width = (int)(pictureBox.Image.Width * zoomFactor);
            pictureBox.Height = (int)(pictureBox.Image.Height * zoomFactor);
            pictureBox.Invalidate(); // 强制重绘PictureBox
        }
    }


}
