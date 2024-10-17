using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using wxreader;

public class CustomMessagePanel : Panel
{
    private string _messageText;
    private Dictionary<string, Image> _emotionDict; // 存放表情的字典
    private Font _font;
    private const int EmojiSize = 16; // 表情图标的大小
    private const int DefaultPadding = 5; // 设置默认的文本边距
    private int parentCornerRadius;

    public string MessageText
    {
        get { return _messageText; }
        set { _messageText = value; Invalidate(); } // 更新文本，重绘
    }

    public Dictionary<string, Image> EmotionDict
    {
        get { return _emotionDict; }
        set { _emotionDict = value; Invalidate(); } // 更新表情字典，重绘
    }

    public CustomMessagePanel()
    {
        _emotionDict = new Dictionary<string, Image>();
        _font = new Font("Segoe UI Emoji", 9);
        this.AutoSize = true;
        this.BackColor = Color.White; // 默认背景颜色
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (IsOnlyEmoji(_messageText))
        {
            // 仅有表情，绘制表情
            DrawEmotion(e.Graphics);
        }
        else
        {
            // 绘制文本和表情
            DrawMessage(e.Graphics);
        }
    }

    private void DrawEmotion(Graphics graphics)
    {
        var parentCornerRadius = this.Parent is RounderCornerBubble parentControl ? parentControl.CornerRadius : 0; // 假设父控件有 CornerRadius 属性
        parentCornerRadius = Math.Max(parentCornerRadius, 0); // 确保 parentCornerRadius 至少为 0
        var currentX = Padding.Left + 5;
        var currentY = Padding.Top + 5;
        var lineHeight = TextRenderer.MeasureText("问", _font).Height; // 计算行高
        var maxWidth = 280 - this.Padding.Left - this.Padding.Right; // 计算最大宽度
        var totalwidth = 0; // 计算总宽度

        // 使用正则表达式找到表情占位符
        var regex = new Regex(@"\[.+?\]"); // 匹配 "[占位符]用\[.+?\]"
        var matches = regex.Matches(_messageText);

        int lastIndex = 0;

        foreach (Match match in matches)
        {
            // 绘制表情图标
            string key = match.Value; // 获取占位符
            if (_emotionDict.ContainsKey(key))
            {
                // 检查是否会超出最大宽度
                if (currentX + EmojiSize > maxWidth)
                {
                    currentX = Padding.Left + 10; // 重置X位置
                    currentY += lineHeight; // 换行
                }

                graphics.DrawImage(_emotionDict[key], new Rectangle(currentX + 3, currentY, EmojiSize, EmojiSize)); // 垂直居中显示
                currentX += EmojiSize + 3; // 更新当前X位置并增加一些边距
                totalwidth += EmojiSize + 3; // 更新总宽度
            }

            lastIndex = match.Index + match.Length; // 更新上一个匹配的结束位置 
        }

        this.Width = totalwidth + 10; // 更新面板宽度
        // 更新面板高度
        this.Height = currentY + lineHeight + this.Padding.Top + this.Padding.Bottom; // 考虑行高以更新面板高度
    }
        

    private void DrawMessage(Graphics graphics)
    {
        var parentCornerRadius = this.Parent is RounderCornerBubble parentControl ? parentControl.CornerRadius : 0; // 假设父控件有 CornerRadius 属性
        parentCornerRadius = Math.Max(parentCornerRadius, 0); // 确保 parentCornerRadius 至少为 0
        var currentX = Padding.Left + 8;
        var currentY = Padding.Top + 5;
        var lineHeight = TextRenderer.MeasureText("问", _font).Height; // 计算行高
        var maxWidth = 280 - this.Padding.Left - this.Padding.Right; // 计算最大宽度
        var totalwidth = 0; // 计算总宽度

        // 使用正则表达式找到表情占位符
        var regex = new Regex(@"\[.+?\]"); // 匹配 "[占位符]用\[.+?\]"
        var matches = regex.Matches(_messageText);

        int lastIndex = 0;

        // 绘制文本和表情
        foreach (Match match in matches)
        {
            // 绘制前面的文本
            if (match.Index > lastIndex)
            {
                string textBefore = _messageText.Substring(lastIndex, match.Index - lastIndex);
                // 处理文本换行
                DrawTextWithWrap(graphics, textBefore, ref currentX, ref currentY, lineHeight, maxWidth);
                totalwidth += Math.Max(totalwidth, CalculateTextWidth(graphics, textBefore));
            }

            // 绘制表情图标
            string key = match.Value; // 获取占位符
            if (_emotionDict.ContainsKey(key))
            {
                // 检查是否会超出最大宽度
                if (currentX + EmojiSize > maxWidth)
                {
                    currentX = Padding.Left + 10; // 重置X位置
                    currentY += lineHeight; // 换行
                }

                graphics.DrawImage(_emotionDict[key], new Rectangle(currentX + 3, currentY, EmojiSize, EmojiSize)); // 垂直居中显示
                currentX += EmojiSize + 3; // 更新当前X位置并增加一些边距
                totalwidth += EmojiSize + 3; // 更新总宽度
            }

            lastIndex = match.Index + match.Length; // 更新上一个匹配的结束位置
        }

        // 绘制最后的文本
        if (lastIndex < _messageText.Length)
        {
            string textAfter = _messageText.Substring(lastIndex);
            DrawTextWithWrap(graphics, textAfter, ref currentX, ref currentY, lineHeight, maxWidth);
            totalwidth += CalculateTextWidth(graphics, textAfter);
        }

        this.Width = totalwidth +15; // 更新面板宽度
        // 更新面板高度
        this.Height = currentY + lineHeight + this.Padding.Top + this.Padding.Bottom; // 考虑行高以更新面板高度
    }

    private int CalculateTextWidth(Graphics graphics, string text)
    {
        int width = 0;
        for (int i = 0; i < text.Length; i++)
        {
            width += (int)_font.Size + 3; // 更新宽度
        }
        return width; // 返回计算出的宽度
    }

    private void DrawTextWithWrap(Graphics graphics, string text, ref int currentX, ref int currentY, int lineHeight, int maxWidth)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        const int charSpacing = 3; // 定义固定的字符间距，可以根据需要调整


        string emotionstring = "";
        for (int i = 0; i < text.Length; i++)
        {
            //var charSize = TextRenderer.MeasureText(text[i].ToString(), _font);
            string temp = text.Substring(i, 1);
            var charSize = graphics.MeasureString(temp, _font);

            // 如果当前X位置加上字符宽度超过最大宽度，换行
            if (currentX + charSize.Width > maxWidth)
            {
                currentX = Padding.Left + parentCornerRadius + 10; // 重置X位置
                currentY += lineHeight; // 换行
            }
            char c = text[i];
            // 检测是否为中文字符及中文标点符号
            if (ContainsChinese(temp)) // Unicode范围内的中文字符
            {
                // 中文字符，直接绘制
                graphics.DrawString(temp, _font, Brushes.Black, new Point(currentX, currentY));
            }
            else
            {
                emotionstring += temp;
                if (emotionstring.Length > 1)
                {
                    graphics.DrawString(emotionstring, _font, Brushes.Red, new Point(currentX, currentY)); // 使用红色作为示例 
                    emotionstring = ""; // 清空缓存
                }
                else
                {
                    continue;
                }
            }
            currentX += (int)_font.Size + charSpacing; // 更新当前X位置，加上字符间距
        }
    }

    /*private void DrawTextWithWrap(Graphics graphics, string text, ref int currentX, ref int currentY, int lineHeight, int maxWidth)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        const int charSpacing = 3; // 定义固定的字符间距，可以根据需要调整

        for (int i = 0; i < text.Length; i++)
        {
            char currentChar = text[i];
            string currentStr = currentChar.ToString();

            // 如果是Unicode表情，假设它的形状为\u1234这样的字符，我们可以用Regex来判断
            if (currentChar >= 0x10000) // 假设Unicode表情的代码点大于0x10000（需要根据具体情况调整）
            {
                // 对于Unicode表情，绘制时使用两倍的宽度
                var charSize = graphics.MeasureString(currentStr, _font);

                // 如果当前X位置加上字符宽度超过最大宽度，换行
                if (currentX + charSize.Width > maxWidth)
                {
                    currentX = Padding.Left + parentCornerRadius + 10; // 重置X位置
                    currentY += lineHeight; // 换行
                }

                // 绘制Unicode表情
                graphics.DrawString(currentStr, _font, Brushes.Black, new Point(currentX, currentY)); // 使用红色作为示例

                // 更新当前X位置，加上字符宽度和字符间距（这里加两倍的字符宽度）
                currentX += (int)charSize.Width + charSpacing; // 使用实际字符宽度
            }
            else
            {
                var charSize = graphics.MeasureString(currentStr, _font);

                // 如果当前X位置加上字符宽度超过最大宽度，换行
                if (currentX + charSize.Width > maxWidth)
                {
                    currentX = Padding.Left + parentCornerRadius + 10; // 重置X位置
                    currentY += lineHeight; // 换行
                }

                // 绘制普通字符
                graphics.DrawString(currentStr, _font, Brushes.Black, new Point(currentX, currentY));

                // 更新当前X位置，加上字符宽度和字符间距
                currentX += (int)_font.Size + charSpacing; // 使用实际字符宽度
            }
        }
    }*/

    private bool ContainsChinese(string input)
    {
        // 正则表达式匹配中文字符和中文标点符号
        return Regex.IsMatch(input, @"[\u4e00-\u9fa5]|[\u3000-\u303F]|[\uFF00-\uFFEF]");
    }


    private bool IsOnlyEmoji(string text)
    {
        int length = 0;
        var regex = new Regex(@"\[.+?\]"); // 匹配 "[占位符]用\[.+?\]"
        var matches = regex.Matches(text);
        foreach (Match match in matches)
        {
            length += match.Value.Length;
        }
        if (length == text.Length)
        {
            return true;
        }
        return false;
    }

}
