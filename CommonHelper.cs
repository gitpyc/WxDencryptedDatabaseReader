using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace wxreader
{
    public static class CommonHelper
    {
        public static string commentEmotion = AppDomain.CurrentDomain.BaseDirectory + "Emotion\\";

        public static string[] ImageExtensions = new string[] { "bmp", "jpeg", "png", "jpg"};
        public static string[] ImageGifExtensions = new string[] { "gif" };

        /// <summary>
        /// 获取文件保存路径
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetFilePathToSave(string fileName)
        {
            return AppDomain.CurrentDomain.BaseDirectory + "Files\\" + fileName;
        }



        /// <summary>
        /// 打开文件、文件夹 或 浏览器打开Url
        /// </summary>
        /// <param name="url"></param>
        public static void OpenFileOrUrl(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(url);
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }

        }

        public static string ShowLastMsgTime(DateTime date)
        {
            string v = date.ToString("yyy-MM-dd");
            string v1 = DateTime.Now.ToString("yyy-MM-dd");
            if (v == v1)
            {
                string str = "上午";
                int v2 = int.Parse(date.ToString("HH"));
                if (v2 > 12)
                {
                    str = "下午";
                }
                string dateTime = date.ToString("h:mm");
                if (!dateTime.Contains(str))
                {
                    dateTime = str + dateTime;
                }
                return dateTime;
            }
            return date.ToString("yyyy-MM-dd");
        }

        /*public static Image GetUserHeadByID(string id)
        {
            int index = 0;
            String str = id.Last().ToString();
            Regex _numericregex = new Regex(@"^[-]?[0-9]+(\.[0-9]+)?$");
            bool isExist = _numericregex.IsMatch(str);
            if (isExist)
            {
                int last = int.Parse(str);
                if (last % 2 == 0)
                {
                    index = 1;
                }
                else
                {
                    index = 2;
                }
            }
            Image image = ChatRenderTestForm.ImageHeadList[index];
            if (image == null)
            {
                return null;
            }
            return image;
        }*/

        public static Image GetFileIconBitmap(string fileExtendName)
        {
            try
            {
                FileType fileType = GetFileType(fileExtendName.Replace(".", ""));
                string iconName = "unknown";
                switch (fileType)
                {
                    case FileType.Video:
                        iconName = "video";
                        break;
                    case FileType.Music:
                        iconName = "music";
                        break;
                    case FileType.Rar:
                        iconName = "rar";
                        break;
                    case FileType.PPT:
                        iconName = "ppt";
                        break;
                    case FileType.Word:
                        iconName = "word";
                        break;
                    case FileType.Excel:
                        iconName = "excel";
                        break;
                    case FileType.Txt:
                        iconName = "txt";
                        break;
                    case FileType.PDF:
                        iconName = "pdf";
                        break;
                    case FileType.Image:
                        iconName = "image";
                        break;
                    case FileType.Other:
                        iconName = "unknown";
                        break;
                    case FileType.None:
                        iconName = "folder";
                        break;
                }

                //return new BitmapImage(new Uri(iconName));
                Image image = ChatHistory.imageFileList[iconName];
                if (image == null)
                {
                    return null;
                }
                return image;
            }
            catch (Exception ee)
            {
                return null;
            }
        }

        public static Dictionary<string, Image> fileTypeDict = new Dictionary<string, Image>();
        public static Dictionary<string, Image> FileTypeDict
        {
            get
            {
                return fileTypeDict;
            }
        }
        

        public static FileType GetFileType(string fileExtendName)
        {
            try
            {
                switch (fileExtendName.ToLower())
                {
                    case "txt":
                        return FileType.Txt;
                    case "xls":
                    case "xlsx":
                        return FileType.Excel;
                    case "doc":
                    case "docx":
                        return FileType.Word;
                    case "ppt":
                    case "pptx":
                        return FileType.PPT;
                    case "pdf":
                        return FileType.PDF;
                    case "zip":
                    case "rar":
                        return FileType.Rar;
                    case "exe":
                        return FileType.Exe;
                    case "gif":
                    case "jpg":
                    case "jpeg":
                    case "png":
                    case "bmp":
                    case "ico":
                    case "svg":
                    case "tiff":
                        return FileType.Image;
                    case "mp3":
                    case "wma":
                    case "wav":
                    case "aif":
                    case "aiff":
                    case "au":
                    case "ram":
                    case "mid":
                    case "rmi":
                        return FileType.Music;
                    case "mp4":
                    case "avi":
                    case "mov":
                    case "rmvb":
                    case "rm":
                    case "flv":
                    case "3gp":
                    case "mpeg":
                    case "mpg":
                    case "dat":
                    case "mkv":
                        return FileType.Video;
                    default:
                        return FileType.Other;
                }
            }
            catch (Exception e)
            {
                return FileType.None;
            }
        }

        public enum FileType
        {
            None,
            Other,
            Txt,
            Excel,
            Word,
            PPT,
            PDF,
            Rar,
            Exe,
            Image,
            Music,
            Video,
        }
    }
}
