using ESFramework.Extensions.ChatRendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Imaging;

namespace wxreader
{
    public class RenderDataProvider : IRenderDataProvider
    {
        
        public Dictionary<int, object> GetEmotions()
        {
            return null;
        }

        public string GetFilePathToSave(string fileName)
        {
            return GloableVars.filePath + "\\" + fileName;
        }

        public object GetImageOfAudioCall()
        {
            Image img = Image.FromFile("./Resources/audioCall.png");
            return img;
        }
        public static ImageSource Bitmap2ImageSource(System.Drawing.Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            ImageSource wpfBitmap = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            if (!DeleteObject(hBitmap))//记得要进行内存释放。否则会有内存不足的报错。
            {
                throw new System.ComponentModel.Win32Exception();
            }
            return wpfBitmap;
        }

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);
        public object GetImageOfFileType(string fileExtendName)
        {
            return CommonHelper.GetFileIconBitmap(fileExtendName);
        }

        public object GetImageOfSendFailed()
        {
            return null;
        }

        public object GetImageOfVideoCall()
        {
            Image img = Image.FromFile("./Resources/videoCall.png");
            return img;
        }

        public string GetSpeakerDisplayName(string speakerID)
        {
            string name = GloableVars.userlist.Find(x => x.username == speakerID).remark;
            if (name == null)
            {
                return GloableVars.userlist.Find(x => x.username == speakerID).nickname;
            }
            else
            {
                return name;
            }
        }

        public object GetUserHeadImage(string userID)
        {
            //在本地文件中查找用户头像
            string path = GloableVars.filePath + "\\headimage\\" + userID + ".jpg";
            if (File.Exists(path))
            {
                Image img = Image.FromFile(path);
                return img;
            }else
            {
                return Image.FromFile(GloableVars.filePath + "\\headimage\\default.jpg");
            }
        }

        public string GetUserName(string userID)
        {
            //查找用户列表
            string name =GloableVars.userlist.Find(x => x.username == userID).remark;
            if (name == null)
            {
                return GloableVars.userlist.Find(x => x.username == userID).nickname;
            }else
            {
                return name;
            }
            
        }
    }
}
