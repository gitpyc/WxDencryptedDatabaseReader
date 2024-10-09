using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace wxreader
{
    public class CustomRichTextBox : RichTextBox
    {
        public Dictionary<string, Image> ImageMap { get; private set; } // 存放占位符与图片的映射关系
        private string richTextContent;

        public string RichTextContent
        {
            get { return richTextContent; }
            set
            {
                richTextContent = value;
                UpdateContent(); // 更新内容后更新RichTextBox
            }
        }

        public CustomRichTextBox()
        {
            ImageMap = new Dictionary<string, Image>();
            this.BorderStyle = BorderStyle.FixedSingle; // 设置边框样式
            this.ReadOnly = true; // 设置为只读，防止用户修改内容
            this.BackColor = Color.White;
            this.SelectionChanged += (s, e) => { this.Select(0, 0); }; // 取消当前选择
        }

        public void AddImage(string placeholder, Image image)
        {
            ImageMap[placeholder] = image; // 添加占位符与图片的映射
        }

        private void UpdateContent()
        {
            this.Clear(); // 清空当前内容
            string pattern = @"\[.+?\]"; // 匹配"[占位符]"
            int currentIndex = 0;

            // 使用正则表达式提取所有占位符
            MatchCollection matches = Regex.Matches(RichTextContent, pattern);

            foreach (Match match in matches)
            {
                // 插入占位符前的文本
                if (currentIndex < match.Index)
                {
                    this.AppendText(RichTextContent.Substring(currentIndex, match.Index - currentIndex));
                }

                // 插入占位符对应的图片
                string placeholder = match.Value;
                if (ImageMap.ContainsKey(placeholder))
                {
                    // 获取相应的图像
                    Image img = ImageMap[placeholder];
                    // 在RichTextBox中插入图像
                    this.LoadFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wxemoji", placeholder.Substring(1, placeholder.Length - 2) + ".png"), RichTextBoxStreamType.RichText);
                }

                currentIndex = match.Index + match.Length; // 更新索引
            }

            // 插入剩余文本
            if (currentIndex < RichTextContent.Length)
            {
                this.AppendText(RichTextContent.Substring(currentIndex));
            }

            // 自动换行
            this.SelectionStart = this.Text.Length; // 将光标放到文本末尾
            this.ScrollToCaret(); // 滚动到光标位置
        }

        // 可以重写的方法，处理高度变化，以便让父容器调整大小
        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            this.Height = this.GetPreferredSize(new Size(this.Width, int.MaxValue)).Height; // 根据内容调整高度
        }
    }

}